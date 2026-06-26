using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Dystopia.Battle;
using Dystopia.Events;
using Dystopia.Economy.Services;
using Dystopia.Networking;

namespace Dystopia.UI
{
    public class BattleResultPanel : MonoBehaviour
    {
        [Header("Outcome")]
        [SerializeField] private TMP_Text outcomeText;
        [SerializeField] private TMP_Text roundsText;

        [Header("Rewards")]
        [SerializeField] private TMP_Text goldRewardText;
        [SerializeField] private TMP_Text fragmentRewardText;
        [SerializeField] private TMP_Text payoutStatusText;

        [Header("Balances")]
        [SerializeField] private TMP_Text goldBalanceText;
        [SerializeField] private TMP_Text fragmentBalanceText;

        [Header("Actions")]
        [SerializeField] private Button continueButton;
        [SerializeField] private string battleModeScene = "BattleMode";

        private CloudScriptService _cloudSvc;
        private WalletService      _wallet;

        public void Initialise(CloudScriptService cloudSvc, WalletService wallet)
        {
            _cloudSvc = cloudSvc;
            _wallet   = wallet;
            gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinue);
            BattleEvents.OnBattleEnded += ShowResult;
        }

        private void OnDestroy()
        {
            BattleEvents.OnBattleEnded -= ShowResult;
            if (_wallet != null) _wallet.OnCurrencyChanged -= OnCurrencyChanged;
        }

        private void ShowResult(BattleResult result)
        {
            gameObject.SetActive(true);

            outcomeText.text  = result.Outcome.ToUpper();
            outcomeText.color = result.PlayerWon ? new Color(0.18f, 0.82f, 0.34f)
                              : result.IsDraw    ? new Color(0.95f, 0.77f, 0.06f)
                              :                    new Color(0.80f, 0.20f, 0.20f);

            roundsText.text         = $"Battle lasted {result.RoundsPlayed} round{(result.RoundsPlayed == 1 ? "" : "s")}";
            goldRewardText.text     = $"+{result.GoldReward} Gold";
            fragmentRewardText.text = $"+{result.FragmentReward} Fragments";

            if (_wallet != null)
            {
                RefreshBalanceDisplay();
                _wallet.OnCurrencyChanged += OnCurrencyChanged;
            }

            if (_cloudSvc != null)
            {
                payoutStatusText.text       = "Claiming rewards...";
                continueButton.interactable = false;

                _cloudSvc.ClaimBattlePayout(result.GoldReward, result.FragmentReward,
                    ()  => { payoutStatusText.text = "Rewards claimed!";         continueButton.interactable = true; },
                    err => { payoutStatusText.text = "Failed to claim rewards.";
                             Debug.LogError($"[BattleResultPanel] Payout failed: {err}");
                             continueButton.interactable = true; });
            }
            else
            {
                payoutStatusText.text       = "(offline — no rewards granted)";
                continueButton.interactable = true;
            }
        }

        private void OnCurrencyChanged(string name, int value)
        {
            if (name == "Gold" || name == "Fragments") RefreshBalanceDisplay();
        }

        private void RefreshBalanceDisplay()
        {
            if (goldBalanceText     != null) goldBalanceText.text     = $"Gold: {_wallet.Gold}";
            if (fragmentBalanceText != null) fragmentBalanceText.text = $"Fragments: {_wallet.Fragments}";
        }

        private void OnContinue() => SceneManager.LoadScene(battleModeScene);
    }
}
