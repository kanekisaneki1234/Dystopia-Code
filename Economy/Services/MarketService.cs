using System;
using System.Collections.Generic;
using UnityEngine;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Data;
using Dystopia.Networking;

namespace Dystopia.Economy.Services
{
    public class MarketService
    {
        private readonly CloudScriptService  _cloudSvc;
        private readonly WalletService       _wallet;
        private readonly CollectionService   _collection;
        private readonly CardDatabase        _database;

        public MarketService(CloudScriptService cloudSvc, WalletService wallet,
            CollectionService collection, CardDatabase database)
        {
            _cloudSvc   = cloudSvc;
            _wallet     = wallet;
            _collection = collection;
            _database   = database;
        }

        // ── Browse ────────────────────────────────────────────────────────────

        public void GetMarketListings(
            Action<List<MarketListing>> onSuccess, Action<string> onFailed = null)
            => _cloudSvc.GetMarketListings(onSuccess, onFailed);

        public void GetMyListings(
            Action<List<MarketListing>> onSuccess, Action<string> onFailed = null)
            => _cloudSvc.GetMyListings(onSuccess, onFailed);

        // ── List ──────────────────────────────────────────────────────────────

        public void ListCard(CardInstance card, int price, string listingType,
            Action<ListCardResult> onSuccess, Action<string> onFailed = null)
        {
            _cloudSvc.ListCard(
                card.PlayFabItemInstanceId, price, listingType, "Player",
                result =>
                {
                    if (listingType == "original")
                    {
                        _collection.RemoveCard(card);
                    }
                    else
                    {
                        card.duplicateCount = Math.Max(0, card.duplicateCount - 1);
                    }
                    card.MarketCooldownUntil = result.cooldownEndsAt;
                    onSuccess?.Invoke(result);
                },
                onFailed);
        }

        // ── Delist ────────────────────────────────────────────────────────────

        public void DelistCard(MarketListing listing,
            Action<DelistCardResult> onSuccess, Action<string> onFailed = null)
        {
            _cloudSvc.DelistCard(listing.listingId, result =>
            {
                if (listing.listingType == "original")
                {
                    var cardData = _database.GetByCardId(result.cardId);
                    if (cardData != null)
                    {
                        var ci = _collection.AddCard(cardData);
                        ci.PlayFabItemInstanceId = result.newItemInstanceId;
                        if (result.savedCardData != null)
                        {
                            ci.RestoreState(
                                result.savedCardData.level,
                                result.savedCardData.tier,
                                (ResonanceLevel)result.savedCardData.resonance,
                                result.savedCardData.duplicateCount,
                                result.newItemInstanceId);
                        }
                        ci.MarketCooldownUntil = result.cooldownUntil;
                    }
                }
                else
                {
                    var existing = _collection.GetOwnedCardByInstanceId(result.originalItemInstanceId);
                    if (existing != null)
                        existing.duplicateCount++;
                }
                onSuccess?.Invoke(result);
            }, onFailed);
        }

        // ── Buy ───────────────────────────────────────────────────────────────

        public void BuyCard(MarketListing listing,
            Action<BuyCardResult> onSuccess, Action<string> onFailed = null)
        {
            _cloudSvc.BuyCard(listing.listingId, result =>
            {
                var cardData = _database.GetByCardId(result.cardId);

                if (result.isNewCard && cardData != null)
                {
                    var ci = _collection.AddCard(cardData);
                    ci.PlayFabItemInstanceId = result.newItemInstanceId;

                    if (listing.listingType == "original" && result.cardData != null)
                    {
                        ci.RestoreState(
                            result.cardData.level,
                            result.cardData.tier,
                            (ResonanceLevel)result.cardData.resonance,
                            result.cardData.duplicateCount,
                            result.newItemInstanceId);

                        _cloudSvc.UpdateCardState(
                            result.newItemInstanceId,
                            result.cardData.level,
                            result.cardData.tier,
                            result.cardData.resonance,
                            result.cardData.duplicateCount,
                            () => Debug.Log("[MarketService] BuyCard: UpdateCardState succeeded."),
                            err => Debug.LogWarning($"[MarketService] BuyCard: UpdateCardState failed: {err}"));
                    }
                }
                else
                {
                    var existing = _collection.GetOwnedCard(result.cardId);
                    if (existing != null) existing.AddDuplicate();
                }

                onSuccess?.Invoke(result);
            }, onFailed);
        }
    }
}
