using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Data;
using Dystopia.Networking;

namespace Dystopia.Economy.Services
{
    public class CollectionService
    {
        private readonly CardCollection _collection;

        private MonoBehaviour         _runner;
        private PlayFabAuthService    _auth;
        private CloudScriptService    _cloudSvc;
        private CardDatabase          _database;
        private PlayFabOperationQueue _queue;

        private bool IsPlayFabConnected => _queue != null && _auth != null && _auth.IsLoggedIn;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<CardInstance> OnCardAdded;
        public event Action<CardInstance> OnCardRemoved;

        // ── Constructor ───────────────────────────────────────────────────
        public CollectionService(CardCollection collection)
        {
            _collection = collection;
        }

        // ── PlayFab wiring (mirrors WalletService.ConnectPlayFab) ─────────
        public void ConnectPlayFab(PlayFabAuthService auth, MonoBehaviour runner,
            CloudScriptService cloudSvc, CardDatabase database, PlayFabOperationQueue queue)
        {
            _auth     = auth;
            _runner   = runner;
            _cloudSvc = cloudSvc;
            _database = database;
            _queue    = queue;

            if (auth.IsLoggedIn) FetchCollection();
            else auth.OnLoginSuccess += FetchCollection;
        }

        // ── Read-only accessors ───────────────────────────────────────────
        public IReadOnlyList<CardInstance> OwnedCards => _collection.OwnedCards;

        public int CardCount => _collection.OwnedCards.Count;

        // ── Fetch collection from PlayFab on login ────────────────────────
        private void FetchCollection()
        {
            _queue.Enqueue(done =>
                PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                    result => { ApplyInventory(result); done(); },
                    err    =>
                    {
                        Debug.LogError($"[CollectionService] Inventory fetch failed: {err.GenerateErrorReport()}");
                        done();
                    }));
        }

        private void ApplyInventory(GetUserInventoryResult result)
        {
            _collection.OwnedCards.Clear();

            if (result.Inventory == null) return;

            foreach (var item in result.Inventory)
            {
                var cardData = _database.GetByCardId(item.ItemId);
                if (cardData == null) continue;

                var cd        = item.CustomData ?? new Dictionary<string, string>();
                int level     = cd.TryGetValue("level",          out var lv)  ? int.Parse(lv)   : 1;
                int tier      = cd.TryGetValue("tier",           out var tr)  ? int.Parse(tr)   : 1;
                var resonance = cd.TryGetValue("resonance", out var res)
                    ? (ResonanceLevel)int.Parse(res)
                    : ResonanceLevel.None;
                int dupes     = cd.TryGetValue("duplicateCount", out var dc)  ? int.Parse(dc)   : 0;

                var instance = new CardInstance(cardData);
                instance.RestoreState(level, tier, resonance, dupes, item.ItemInstanceId);
                if (cd.TryGetValue("marketCooldownUntil", out var mcu) && mcu != null &&
                    long.TryParse(mcu, out var cooldownMs))
                    instance.MarketCooldownUntil = DateTimeOffset.FromUnixTimeMilliseconds(cooldownMs)
                        .UtcDateTime.ToString("O");
                _collection.OwnedCards.Add(instance);
                OnCardAdded?.Invoke(instance);
            }

            Debug.Log($"[CollectionService] Loaded {_collection.OwnedCards.Count} cards from PlayFab.");
        }

        // ── Save card progression state to PlayFab ────────────────────────
        // Uses UpdateCardState Cloud Script because client SDK has no write access
        // to inventory item custom data — that's a Server API only.
        public void SaveCardState(CardInstance card, Action onSuccess = null, Action onFailed = null)
        {
            if (_cloudSvc == null || string.IsNullOrEmpty(card.PlayFabItemInstanceId)) return;

            _cloudSvc.UpdateCardState(
                card.PlayFabItemInstanceId,
                card.CurrentLevel,
                card.CurrentTier,
                (int)card.resonanceLevel,
                card.duplicateCount,
                () => { Debug.Log($"[CollectionService] Saved: {card.data.cardName}"); onSuccess?.Invoke(); },
                err => { Debug.LogError($"[CollectionService] Save failed: {err}"); onFailed?.Invoke(); }
            );
        }

        // ── Server-authoritative card acquisition ─────────────────────────
        // New card  → GrantCard Cloud Script → itemInstanceId set on CardInstance
        // Duplicate → AddDuplicate Cloud Script (optimistic local update first)
        public void ClaimCard(CardData cardData, Action<CardInstance> onComplete)
        {
            var existing = GetOwnedCard(cardData);

            if (existing != null)
            {
                existing.AddDuplicate();
                OnCardAdded?.Invoke(existing);

                if (IsPlayFabConnected && !string.IsNullOrEmpty(existing.PlayFabItemInstanceId))
                {
                    _cloudSvc.AddDuplicate(existing.PlayFabItemInstanceId,
                        ()  => onComplete?.Invoke(existing),
                        err => { Debug.LogError($"[CollectionService] AddDuplicate failed: {err}"); onComplete?.Invoke(existing); });
                }
                else
                {
                    onComplete?.Invoke(existing);
                }
            }
            else
            {
                if (!IsPlayFabConnected)
                {
                    var local = AddCard(cardData);
                    onComplete?.Invoke(local);
                    return;
                }

                _cloudSvc.GrantCard(cardData.cardId,
                    itemInstanceId =>
                    {
                        var newCard = AddCard(cardData);
                        newCard.PlayFabItemInstanceId = itemInstanceId;
                        onComplete?.Invoke(newCard);
                    },
                    err =>
                    {
                        Debug.LogError($"[CollectionService] GrantCard failed: {err}");
                        onComplete?.Invoke(null);  // advance ClaimNext even on failure
                    });
            }
        }

        // ── Add (local; used by ClaimCard and test bootstrappers) ─────────
        public CardInstance AddCard(CardData cardData)
        {
            var instance = new CardInstance(cardData);

            if (cardData.rank == CardRank.S) instance.SetResonance(ResonanceLevel.R1);

            _collection.OwnedCards.Add(instance);
            OnCardAdded?.Invoke(instance);
            return instance;
        }

        // ── Remove ────────────────────────────────────────────────────────
        public bool RemoveCard(CardInstance card)
        {
            if (!_collection.OwnedCards.Contains(card)) return false;

            _collection.OwnedCards.Remove(card);
            OnCardRemoved?.Invoke(card);
            return true;
        }

        // ── Queries ───────────────────────────────────────────────────────
        public List<CardInstance> GetCardsByClass(CardClass cardClass)
            => _collection.OwnedCards.Where(c => c.data.cardClass == cardClass).ToList();

        public List<CardInstance> GetCardsByType(CardType cardType)
            => _collection.OwnedCards.Where(c => c.data.cardType == cardType).ToList();

        public List<CardInstance> GetCardsByName(string cardName)
            => _collection.OwnedCards.Where(c => c.data.cardName == cardName).ToList();

        // ── Duplicate counting ────────────────────────────────────────────
        public int CountDuplicates(CardData cardData)
            => _collection.OwnedCards.Count(c => c.data.cardId == cardData.cardId);

        // ── Ownership checks ──────────────────────────────────────────────
        public bool OwnsCard(CardInstance card)
            => _collection.OwnedCards.Contains(card);

        public bool OwnsCardOfType(CardData cardData)
            => _collection.OwnedCards.Any(c => c.data.cardId == cardData.cardId);

        public CardInstance GetOwnedCard(string cardId)
            => _collection.OwnedCards.FirstOrDefault(c => c.data.cardId == cardId);

        public CardInstance GetOwnedCard(CardData cardData)
            => GetOwnedCard(cardData.cardId);

        public CardInstance GetOwnedCardByInstanceId(string itemInstanceId)
            => _collection.OwnedCards.FirstOrDefault(c => c.PlayFabItemInstanceId == itemInstanceId);
    }
}
