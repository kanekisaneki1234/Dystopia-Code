using UnityEngine;
using Dystopia.Cards;
using Dystopia.Economy.Services;
using Dystopia.Networking;
using Dystopia.Progression;

namespace Dystopia.UI
{
    public class EconomyTestBootstrapper : MonoBehaviour
    {
        [Header("Card Asset")]
        public CardData     testCard;
        public CardDatabase cardDatabase;

        [Header("UI")]
        public EconomyTestUI ui;

        [Header("Test Card Setup")]
        public int startingDuplicates = 30;

        // ── Services and Managers (accessible by UI) ──────────────────
        public WalletService      Wallet     { get; private set; }
        public CollectionService  Collection { get; private set; }
        public LevelManager       LevelMgr   { get; private set; }
        public TierManager        TierMgr    { get; private set; }
        public ResonanceManager   ResMgr     { get; private set; }
        public CloudScriptService CloudSvc   { get; private set; }
        public CardInstance       ActiveCard { get; private set; }

        private void Start()
        {
            var net = NetworkBootstrapper.Instance;
            if (net == null)
            {
                Debug.LogError("[EconomyTestBootstrapper] NetworkBootstrapper.Instance is null. Start from TitleScene.");
                return;
            }

            Wallet     = net.Wallet;
            Collection = net.Collection;
            CloudSvc   = net.CloudSvc;
            LevelMgr   = net.LevelMgr;
            TierMgr    = net.TierMgr;
            ResMgr     = net.ResMgr;

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

            LevelMgr.OnLevelUp    += (card, _) => Collection.SaveCardState(card);
            TierMgr.OnTierUpgrade += (card, _) => Collection.SaveCardState(card);
            ResMgr.OnResonanceUp  += (card, _) => Collection.SaveCardState(card);

            // ── Initial UI refresh ───────────────────────────────────
            ui.Initialise(this);
        }
    }
}
