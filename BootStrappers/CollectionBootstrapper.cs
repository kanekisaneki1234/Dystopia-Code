using System;
using System.Collections.Generic;
using UnityEngine;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Data;
using Dystopia.Economy.Services;
using Dystopia.Networking;
using Dystopia.Progression;

namespace Dystopia.UI
{
    public class CollectionBootstrapper : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CollectionUI    collectionUI;
        [SerializeField] private CardDetailSheet detailSheet;
        [SerializeField] private DeckView        deckView;

        // ── Services (accessible by UI scripts) ───────────────────────────────
        public PlayerProfile      Profile    { get; private set; }
        public WalletService      Wallet     { get; private set; }
        public CollectionService  Collection { get; private set; }
        public CloudScriptService CloudSvc   { get; private set; }
        public LevelManager       LevelMgr   { get; private set; }
        public TierManager        TierMgr    { get; private set; }
        public ResonanceManager   ResMgr     { get; private set; }
        public DeckService        DeckSvc    { get; private set; }

        // ── Expose UI controllers so they can cross-reference ─────────────────
        public CollectionUI    CollectionUI { get; private set; }
        public CardDetailSheet DetailSheet  { get; private set; }
        public DeckView        DeckView     { get; private set; }

        // ── Stored delegates for clean unsubscription ─────────────────────────
        private Action<CardInstance, int>            _onLevelUp;
        private Action<CardInstance, int>            _onTierUpgrade;
        private Action<CardInstance, ResonanceLevel> _onResonanceUp;
        private Action                               _onDeckChanged;

        private void Start()
        {
            var net = NetworkBootstrapper.Instance;
            if (net == null)
            {
                Debug.LogError("[CollectionBootstrapper] NetworkBootstrapper.Instance is null. Start from TitleScene.");
                return;
            }

            Profile    = net.Profile;
            Wallet     = net.Wallet;
            Collection = net.Collection;
            CloudSvc   = net.CloudSvc;
            LevelMgr   = net.LevelMgr;
            TierMgr    = net.TierMgr;
            ResMgr     = net.ResMgr;
            DeckSvc    = net.DeckSvc;

            // ── Save first, spend after ───────────────────────────────────────
            _onLevelUp = (card, _) =>
            {
                int gold = LevelMgr.LastGoldCost, frags = LevelMgr.LastFragCost;
                Collection.SaveCardState(card,
                    onSuccess: () => CloudSvc?.SpendMultipleCurrencies(
                        new List<CurrencyCost>
                        {
                            new() { currencyCode = "GD", amount = gold },
                            new() { currencyCode = "FR", amount = frags }
                        }),
                    onFailed: () => { card.LevelDown(); RefreshCardUI(card); });
            };

            _onTierUpgrade = (card, _) =>
            {
                int gold      = TierMgr.LastGoldCost;
                int frags     = TierMgr.LastFragCost;
                int mats      = TierMgr.LastMatsCost;
                CardClass cls = TierMgr.LastCardClass;
                Collection.SaveCardState(card,
                    onSuccess: () => CloudSvc?.SpendMultipleCurrencies(
                        new List<CurrencyCost>
                        {
                            new() { currencyCode = "GD", amount = gold },
                            new() { currencyCode = "FR", amount = frags }
                        },
                        new MaterialCost { className = cls.ToString().ToLower(), amount = mats }),
                    onFailed: () => { card.TierDown(); RefreshCardUI(card); });
            };

            _onResonanceUp = (card, _) =>
            {
                int gold            = ResMgr.LastGoldCost;
                int frags           = ResMgr.LastFragCost;
                int dupes           = ResMgr.LastDupesConsumed;
                ResonanceLevel prev = ResMgr.LastPreviousResonance;
                Collection.SaveCardState(card,
                    onSuccess: () => CloudSvc?.SpendMultipleCurrencies(
                        new List<CurrencyCost>
                        {
                            new() { currencyCode = "GD", amount = gold },
                            new() { currencyCode = "FR", amount = frags }
                        }),
                    onFailed: () => { card.UndoResonance(prev, dupes); RefreshCardUI(card); });
            };

            _onDeckChanged = () => DeckSvc.SaveDecks(
                null,
                err => Debug.LogError($"[CollectionBootstrapper] SaveDecks failed: {err}"));

            LevelMgr.OnLevelUp    += _onLevelUp;
            TierMgr.OnTierUpgrade += _onTierUpgrade;
            ResMgr.OnResonanceUp  += _onResonanceUp;
            DeckSvc.OnDeckChanged += _onDeckChanged;

            // ── Cache UI references ───────────────────────────────────────────
            CollectionUI = collectionUI;
            DetailSheet  = detailSheet;
            DeckView     = deckView;

            // ── Initialise UI ─────────────────────────────────────────────────
            collectionUI?.Initialise(this);
            detailSheet?.Initialise(this);
            deckView?.Initialise(this);
        }

        private void OnDestroy()
        {
            if (LevelMgr != null) LevelMgr.OnLevelUp    -= _onLevelUp;
            if (TierMgr  != null) TierMgr.OnTierUpgrade -= _onTierUpgrade;
            if (ResMgr   != null) ResMgr.OnResonanceUp  -= _onResonanceUp;
            if (DeckSvc  != null) DeckSvc.OnDeckChanged -= _onDeckChanged;
        }

        private void RefreshCardUI(CardInstance card)
        {
            collectionUI?.RefreshCard(card);
            detailSheet?.RefreshIfOpen(card);
        }
    }
}
