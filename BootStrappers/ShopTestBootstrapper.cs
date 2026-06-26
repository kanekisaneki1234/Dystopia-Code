using UnityEngine;
using Dystopia.Cards;
using Dystopia.Economy.Data;
using Dystopia.Economy.Services;
using Dystopia.Networking;

namespace Dystopia.UI
{
    public class ShopTestBootstrapper : MonoBehaviour
    {
        [Header("Card Pool")]
        public CardData[]   allCards;
        public CardDatabase cardDatabase;

        [Header("Pack Assets")]
        public PackData[] packs;

        [Header("UI")]
        public ShopTestUI ui;

        // ── Services (accessible by UI) ──────────────────────────────
        public WalletService      Wallet     { get; private set; }
        public CollectionService  Collection { get; private set; }
        public CloudScriptService CloudSvc   { get; private set; }
        public PackService        PackSvc    { get; private set; }

        private void Start()
        {
            var net = NetworkBootstrapper.Instance;
            if (net == null)
            {
                Debug.LogError("[ShopTestBootstrapper] NetworkBootstrapper.Instance is null. Start from TitleScene.");
                return;
            }

            Wallet     = net.Wallet;
            Collection = net.Collection;
            CloudSvc   = net.CloudSvc;

            // PackService is shop-specific — takes allCards from this scene's serialized assets
            PackSvc = new PackService(Wallet, Collection, allCards);

            // Subscribe to events
            PackSvc.OnNewCardUnlocked += card =>
                Debug.Log($"[Pack] NEW CARD: {card.data.cardName} [{card.data.rank}]");

            PackSvc.OnDuplicateGained += card =>
                Debug.Log($"[Pack] DUPLICATE: {card.data.cardName} (dupes: {card.duplicateCount})");

            // Generate today's contents
            PackSvc.GenerateAllDailyContents(packs);

            ui.Initialise(this);
        }
    }
}
