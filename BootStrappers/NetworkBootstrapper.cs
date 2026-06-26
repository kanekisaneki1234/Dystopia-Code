using UnityEngine;
using Dystopia.Cards;
using Dystopia.Economy.Data;
using Dystopia.Economy.Services;
using Dystopia.Networking;
using Dystopia.Progression;

namespace Dystopia.UI
{
    public class NetworkBootstrapper : MonoBehaviour
    {
        public static NetworkBootstrapper Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private CardDatabase cardDatabase;
        public CardDatabase CardDatabase => cardDatabase;

        // ── Core network services ─────────────────────────────────────────────
        public PlayFabAuthService    Auth  { get; private set; }
        public PlayFabOperationQueue Queue { get; private set; }

        // ── Application services (created once, shared across all scenes) ─────
        public PlayerProfile      Profile    { get; private set; }
        public WalletService      Wallet     { get; private set; }
        public CollectionService  Collection { get; private set; }
        public CloudScriptService CloudSvc   { get; private set; }
        public DeckService        DeckSvc    { get; private set; }
        public LevelManager       LevelMgr   { get; private set; }
        public TierManager        TierMgr    { get; private set; }
        public ResonanceManager   ResMgr     { get; private set; }
        public MarketService      Market     { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Core network
            Auth  = new PlayFabAuthService(this);
            Queue = new PlayFabOperationQueue(this);

            // Application services
            Profile    = new PlayerProfile();
            Wallet     = new WalletService(Profile.Wallet);
            CloudSvc   = new CloudScriptService(Auth, Wallet, Queue);
            Collection = new CollectionService(Profile.Collection);
            DeckSvc    = new DeckService();
            LevelMgr   = new LevelManager(Wallet);
            TierMgr    = new TierManager(Wallet);
            ResMgr     = new ResonanceManager(Wallet, Collection);
            Market     = new MarketService(CloudSvc, Wallet, Collection, cardDatabase);

            // Wire PlayFab — each ConnectPlayFab subscribes to Auth.OnLoginSuccess internally.
            // Subscription order controls queue ordering when login fires:
            //   1. Wallet.FetchAll  → GetUserInventory (currencies) + GetUserData (materials)
            //   2. Collection.FetchAll → GetUserInventory (card inventory)
            //   3. DeckSvc.FetchDecks  → GetUserData("PlayerDecks") — runs after inventory is populated
            Wallet.ConnectPlayFab(Auth, this, Queue);
            Collection.ConnectPlayFab(Auth, this, CloudSvc, cardDatabase, Queue);
            DeckSvc.ConnectPlayFab(Auth, this, CloudSvc, Queue);
            Auth.OnLoginSuccess += () => DeckSvc.FetchDecks(Collection);

            Auth.OnLoginSuccess += () => Debug.Log("[NetworkBootstrapper] All services ready.");
            Auth.OnLoginFailed  += err => Debug.LogError($"[NetworkBootstrapper] Auth failed: {err}");
        }
    }
}
