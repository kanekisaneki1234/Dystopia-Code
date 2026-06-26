using UnityEngine;

namespace Dystopia.UI
{
    public class MarketBootstrapper : MonoBehaviour
    {
        [SerializeField] private MarketUI marketUI;

        private void Start()
        {
            var net = NetworkBootstrapper.Instance;
            if (net == null)
            {
                Debug.LogError("[MarketBootstrapper] No NetworkBootstrapper instance found. " +
                               "Ensure the TitleScene is loaded first.");
                return;
            }
            marketUI.Initialise(net.Market, net.Wallet, net.Collection, net.CloudSvc, net.CardDatabase);
        }
    }
}
