using System.Linq;
using UnityEngine;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Services;
using Dystopia.Networking;

namespace Dystopia.UI
{
    public class BattleModeBootstrapper : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private BattleModeUI ui;

        public CollectionService  Collection { get; private set; }
        public DeckService        DeckSvc    { get; private set; }
        public CloudScriptService CloudSvc   { get; private set; }
        public WalletService      Wallet     { get; private set; }

        private void Start()
        {
            ui?.ShowLoading("Syncing your deck...");

            var net = NetworkBootstrapper.Instance;
            if (net == null)
            {
                Debug.LogError("[BattleModeBootstrapper] NetworkBootstrapper.Instance is null. Start from TitleScene.");
                ui?.ShowNoDeckError();
                return;
            }

            Collection = net.Collection;
            DeckSvc    = net.DeckSvc;
            CloudSvc   = net.CloudSvc;
            Wallet     = net.Wallet;

            if (DeckSvc.IsDecksLoaded)
                OnDecksLoaded();
            else
                DeckSvc.OnDecksLoaded += OnDecksLoaded;
        }

        private void OnDecksLoaded()
        {
            var slots = DeckSvc.GetDeck(DeckSvc.ActiveBattleDeckIndex);
            var cards = slots.Where(c => c != null).ToList();

            if (cards.Count > 0)
            {
                GameSession.SetActiveDeck(DeckSvc.ActiveBattleDeckIndex, cards);
                ui?.ShowDeckReady(DeckSvc.ActiveBattleDeckIndex + 1, cards);
            }
            else
            {
                ui?.ShowNoDeckError();
            }
        }
    }
}
