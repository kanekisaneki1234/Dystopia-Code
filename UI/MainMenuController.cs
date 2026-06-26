using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Dystopia.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Navigation Buttons")]
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button marketButton;

        [Header("Scene Names")]
        [SerializeField] private string collectionScene = "CollectionScene";
        [SerializeField] private string battleModeScene = "BattleMode";
        [SerializeField] private string shopScene       = "ShopTest";
        [SerializeField] private string marketScene     = "MarketScene";

        private void Start()
        {
            collectionButton?.onClick.AddListener(() => SceneManager.LoadScene(collectionScene));
            battleButton?.onClick.AddListener(   () => SceneManager.LoadScene(battleModeScene));
            shopButton?.onClick.AddListener(     () => SceneManager.LoadScene(shopScene));
            marketButton?.onClick.AddListener(   () => SceneManager.LoadScene(marketScene));
        }
    }
}
