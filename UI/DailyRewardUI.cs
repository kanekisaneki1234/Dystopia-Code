using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Networking;
using Dystopia.Economy.Data;

namespace Dystopia.UI
{
    public class DailyRewardUI : MonoBehaviour
    {
        private static readonly int[] RewardSchedule = { 10, 15, 20, 25, 30, 40, 50 };

        [Header("Main Menu Button")]
        [SerializeField] private Button openButton;
        [SerializeField] private Image  notificationBadge;

        [Header("Popup Root")]
        [SerializeField] private GameObject popupPanel;

        [Header("Info")]
        [SerializeField] private TMP_Text streakText;
        [SerializeField] private TMP_Text rewardAmountText;

        [Header("Day Indicators (7 elements each)")]
        [SerializeField] private Image[]    dayIndicators;
        [SerializeField] private TMP_Text[] dayRewardTexts;

        [Header("States")]
        [SerializeField] private GameObject claimableState;
        [SerializeField] private Button     claimButton;
        [SerializeField] private GameObject alreadyClaimedState;
        [SerializeField] private Button     alreadyClaimedDismissButton;
        [SerializeField] private GameObject claimedResultState;
        [SerializeField] private TMP_Text   claimedAmountText;
        [SerializeField] private Button     dismissButton;

        [Header("Day Sprites")]
        [SerializeField] private Sprite spriteUnclaimed;
        [SerializeField] private Sprite spriteClaimed;

        private CloudScriptService _cloudSvc;
        private DailyRewardStatus  _cachedStatus;

        private void Start()
        {
            var net = NetworkBootstrapper.Instance;
            if (net == null)
            {
                Debug.LogWarning("[DailyRewardUI] No NetworkBootstrapper — daily reward disabled.");
                return;
            }
            _cloudSvc = net.CloudSvc;

            openButton?.onClick.AddListener(OpenPopup);
            claimButton?.onClick.AddListener(OnClaim);
            dismissButton?.onClick.AddListener(ClosePopup);
            alreadyClaimedDismissButton?.onClick.AddListener(ClosePopup);

            popupPanel?.SetActive(false);
            notificationBadge?.gameObject.SetActive(false);

            for (int i = 0; i < dayRewardTexts?.Length && i < RewardSchedule.Length; i++)
                if (dayRewardTexts[i] != null)
                    dayRewardTexts[i].text = RewardSchedule[i] + " DM";

            FetchStatus();
        }

        private void FetchStatus()
        {
            _cloudSvc.GetDailyRewardStatus(
                status =>
                {
                    _cachedStatus = status;
                    notificationBadge?.gameObject.SetActive(status.canClaim);
                },
                err => Debug.LogWarning($"[DailyRewardUI] Status check failed: {err}"));
        }

        private void OpenPopup()
        {
            popupPanel?.SetActive(true);
            if (_cachedStatus != null)
            {
                PopulatePopup(_cachedStatus);
            }
            else
            {
                _cloudSvc.GetDailyRewardStatus(
                    status => { _cachedStatus = status; PopulatePopup(status); },
                    err    => { Debug.LogWarning($"[DailyRewardUI] Status fetch on open failed: {err}"); ClosePopup(); });
            }
        }

        private void PopulatePopup(DailyRewardStatus status)
        {
            if (streakText != null)
                streakText.text = status.canClaim
                    ? $"Day {status.currentStreak} — Login Streak"
                    : $"Day {status.currentStreak} Streak Active";

            if (rewardAmountText != null)
                rewardAmountText.text = $"{status.nextReward} Diamonds";

            // (streak-1) % 7 gives the 0-based index of the current day in the weekly cycle
            int todayIdx = (status.currentStreak - 1) % 7;
            for (int i = 0; i < dayIndicators?.Length && i < 7; i++)
            {
                if (dayIndicators[i] == null) continue;
                bool claimed = i < todayIdx || (i == todayIdx && !status.canClaim);
                dayIndicators[i].sprite = claimed ? spriteClaimed : spriteUnclaimed;
            }

            claimableState?.SetActive(status.canClaim);
            alreadyClaimedState?.SetActive(!status.canClaim);
            claimedResultState?.SetActive(false);

            if (claimButton != null) claimButton.interactable = true;
        }

        private void OnClaim()
        {
            if (claimButton != null) claimButton.interactable = false;

            _cloudSvc.ClaimDailyReward(
                result =>
                {
                    _cachedStatus = null;
                    notificationBadge?.gameObject.SetActive(false);

                    if (claimedAmountText != null)
                        claimedAmountText.text = $"+{result.reward} Diamonds!";

                    claimableState?.SetActive(false);
                    claimedResultState?.SetActive(true);
                },
                err =>
                {
                    Debug.LogError($"[DailyRewardUI] ClaimDailyReward failed: {err}");
                    if (claimButton != null) claimButton.interactable = true;
                });
        }

        private void ClosePopup()
        {
            popupPanel?.SetActive(false);
            if (_cachedStatus == null) FetchStatus();
        }
    }
}
