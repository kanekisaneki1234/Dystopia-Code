using UnityEngine;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Data;
using Dystopia.Economy.Services;
using Dystopia.Progression;

namespace Dystopia.UI
{
    public class EconomyTestBootstrapper : MonoBehaviour
    {
        [Header("Card Asset")]
        public CardData testCard;

        [Header("UI")]
        public EconomyTestUI ui;

        [Header("Starting Resources")]
        public int startingGold      = 50000;
        public int startingFragments = 5000;
        public int startingMaterials = 20;
        public int startingDuplicates = 30;

        // ── Services and Managers (accessible by UI) ──────────────────
        public PlayerProfile     Profile    { get; private set; }
        public WalletService     Wallet     { get; private set; }
        public CollectionService Collection { get; private set; }
        public LevelManager      LevelMgr   { get; private set; }
        public TierManager       TierMgr    { get; private set; }
        public ResonanceManager  ResMgr     { get; private set; }
        public CardInstance      ActiveCard { get; private set; }

        private void Start()
        {
            // ── Create profile ───────────────────────────────────────
            Profile = new PlayerProfile();

            // ── Wire services ────────────────────────────────────────
            Wallet     = new WalletService(Profile.Wallet);
            Collection = new CollectionService(Profile.Collection);
            LevelMgr   = new LevelManager(Wallet);
            TierMgr    = new TierManager(Wallet);
            ResMgr     = new ResonanceManager(Wallet, Collection);

            // ── Give starting resources ──────────────────────────────
            Wallet.AddGold(startingGold);
            Wallet.AddFragments(startingFragments);
            Wallet.AddMaterial(testCard.cardClass, startingMaterials);

            // ── Add card to collection ───────────────────────────────
            ActiveCard = Collection.AddCard(testCard);

            // ── Add duplicates for resonance testing ─────────────────
            for (int i = 0; i < startingDuplicates; i++)
                ActiveCard.AddDuplicate();

            // ── Subscribe to events for logging ──────────────────────
            Wallet.OnCurrencyChanged += (name, amount) =>
                Debug.Log($"[Wallet] {name} → {amount}");

            LevelMgr.OnLevelUp += (card, level) =>
                Debug.Log($"[Level] {card.data.cardName} → Level {level}");

            TierMgr.OnTierUpgrade += (card, tier) =>
                Debug.Log($"[Tier] {card.data.cardName} → Tier {tier}");

            ResMgr.OnResonanceUp += (card, res) =>
                Debug.Log($"[Resonance] {card.data.cardName} → {res}");

            // ── Initial UI refresh ───────────────────────────────────
            ui.Initialise(this);
        }
    }
}