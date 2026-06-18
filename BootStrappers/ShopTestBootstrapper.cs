using UnityEngine;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Data;
using Dystopia.Economy.Services;

namespace Dystopia.UI
{
    public class ShopTestBootstrapper : MonoBehaviour
    {
        [Header("Card Pool")]
        public CardData[] allCards;

        [Header("Pack Assets")]
        public PackData[] packs;

        [Header("UI")]
        public ShopTestUI ui;

        [Header("Starting Resources")]
        public int startingDiamonds = 10000;

        // ── Services (accessible by UI) ──────────────────────────────
        public PlayerProfile     Profile    { get; private set; }
        public WalletService     Wallet     { get; private set; }
        public CollectionService Collection { get; private set; }
        public PackService       PackSvc    { get; private set; }

        private void Start()
        {
            Profile    = new PlayerProfile();
            Wallet     = new WalletService(Profile.Wallet);
            Collection = new CollectionService(Profile.Collection);
            PackSvc    = new PackService(Wallet, Collection, allCards);

            Wallet.AddDiamonds(startingDiamonds);

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