using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Core;

namespace Dystopia.UI
{
    public class EconomyTestUI : MonoBehaviour
    {
        [Header("Info Displays")]
        public TMP_Text cardInfoText;
        public ScrollRect cardInfoScrollRect;

        public TMP_Text walletText;
        public ScrollRect walletScrollRect;

        public TMP_Text costPreviewText;
        public ScrollRect costPreviewScrollRect;

        [Header("Buttons")]
        public Button levelUpButton;
        public Button levelUpMaxButton;
        public Button tierUpButton;
        public Button resonanceUpButton;
        public Button addGoldButton;
        public Button addFragmentsButton;
        public Button addDuplicatesButton;

        [Header("Log")]
        public TMP_Text logText;
        public ScrollRect logScrollRect;

        private EconomyTestBootstrapper _boot;
        private string _log = "";

        public void Initialise(EconomyTestBootstrapper boot)
        {
            _boot = boot;

            // ── Wire buttons ─────────────────────────────────────────
            levelUpButton.onClick.AddListener(OnLevelUp);
            levelUpMaxButton.onClick.AddListener(OnLevelUpMax);
            tierUpButton.onClick.AddListener(OnTierUp);
            resonanceUpButton.onClick.AddListener(OnResonanceUp);
            addGoldButton.onClick.AddListener(OnAddGold);
            addFragmentsButton.onClick.AddListener(OnAddFragments);
            addDuplicatesButton.onClick.AddListener(OnAddDuplicates);

            RefreshAll();
        }

        // ── Button handlers ──────────────────────────────────────────

        private void OnLevelUp()
        {
            var card = _boot.ActiveCard;
            _boot.LevelMgr.PreviewLevelUpCost(card, out int frag, out int gold);

            bool success = _boot.LevelMgr.TryLevelUp(card);

            if (success)
                Log($"Levelled up to {card.CurrentLevel} (cost: {frag}F, {gold}G)");
            else
                Log($"Level up FAILED — need {frag}F, {gold}G");

            RefreshAll();
        }

        private void OnLevelUpMax()
        {
            var card = _boot.ActiveCard;
            _boot.LevelMgr.PreviewCostToMax(card, out int frag, out int gold);

            int gained = _boot.LevelMgr.TryLevelUpToMax(card);

            if (gained > 0)
                Log($"Levelled up {gained} times to {card.CurrentLevel} (total: {frag}F, {gold}G)");
            else
                Log("Already at max level for current tier");

            RefreshAll();
        }

        private void OnTierUp()
        {
            var card = _boot.ActiveCard;
            _boot.TierMgr.PreviewTierUpgradeCost(card, out int frag, out int gold, out int mat);

            bool success = _boot.TierMgr.TryTierUpgrade(card);

            if (success)
                Log($"Tier upgraded to {card.CurrentTier} (cost: {frag}F, {gold}G, {mat}M)");
            else if (!_boot.TierMgr.IsAtLevelCap(card))
                Log($"Tier up FAILED — reach level {card.data.MaxLevelForTier(card.CurrentTier)} first");
            else if (_boot.TierMgr.IsMaxTier(card))
                Log("Already at max tier");
            else
                Log($"Tier up FAILED — need {frag}F, {gold}G, {mat}M");

            RefreshAll();
        }

        private void OnResonanceUp()
        {
            var card = _boot.ActiveCard;
            _boot.ResMgr.PreviewResonanceCost(card, out int dupes, out int gold, out int frag, out ResonanceLevel target);

            bool success = _boot.ResMgr.TryResonanceUp(card);

            if (success)
                Log($"Resonance upgraded to {card.resonanceLevel} (cost: {dupes}D, {gold}G, {frag}F)");
            else if (_boot.ResMgr.IsMaxResonance(card))
                Log("Already at max resonance");
            else if (!_boot.ResMgr.HasEnoughDuplicates(card))
                Log($"Resonance FAILED — need {dupes} duplicates, have {card.duplicateCount}");
            else
                Log($"Resonance FAILED — need {gold}G, {frag}F");

            RefreshAll();
        }

        private void OnAddGold()
        {
            _boot.Wallet.AddGold(10000);
            Log("Added 10,000 Gold");
            RefreshAll();
        }

        private void OnAddFragments()
        {
            _boot.Wallet.AddFragments(1000);
            Log("Added 1,000 Fragments");
            RefreshAll();
        }

        private void OnAddDuplicates()
        {
            _boot.ActiveCard.AddDuplicate();
            _boot.ActiveCard.AddDuplicate();
            _boot.ActiveCard.AddDuplicate();
            _boot.ActiveCard.AddDuplicate();
            _boot.ActiveCard.AddDuplicate();
            Log("Added 5 duplicates");
            RefreshAll();
        }

        // ── Refresh displays ─────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshCardInfo();
            RefreshWallet();
            RefreshCostPreview();
            RefreshButtonStates();
        }

        private void RefreshCardInfo()
        {
            var card = _boot.ActiveCard;
            cardInfoText.text =
                $"{card.data.cardName} [{card.data.rank}] [{card.data.cardClass}]\n" +
                $"Tier {card.CurrentTier} | Level {card.CurrentLevel}/{card.data.MaxLevelForTier(card.CurrentTier)} | {card.resonanceLevel}\n" +
                $"HP:{card.HP}  ATK:{card.Attack}  DEF:{card.Defence}  SPD:{card.Speed}\n" +
                $"Mana Cost Mult: {card.ManaCostMultiplier:F2}\n" +
                $"Duplicates: {card.duplicateCount}";
        }

        private void RefreshWallet()
        {
            var w = _boot.Wallet;
            walletText.text =
                $"Gold: {w.Gold}\n" +
                $"Fragments: {w.Fragments}\n" +
                $"Materials ({_boot.ActiveCard.data.cardClass}): {w.GetMaterial(_boot.ActiveCard.data.cardClass)}";
        }

        private void RefreshCostPreview()
        {
            var card = _boot.ActiveCard;
            string preview = "";

            // Next level cost
            if (card.CurrentLevel < card.data.MaxLevelForTier(card.CurrentTier))
            {
                _boot.LevelMgr.PreviewLevelUpCost(card, out int lf, out int lg);
                preview += $"Next level: {lf}F, {lg}G\n";
            }
            else
            {
                preview += "Level: MAX for tier\n";
            }

            // Tier upgrade cost
            if (!_boot.TierMgr.IsMaxTier(card))
            {
                _boot.TierMgr.PreviewTierUpgradeCost(card, out int tf, out int tg, out int tm);
                preview += $"Tier up: {tf}F, {tg}G, {tm}M\n";
            }
            else
            {
                preview += "Tier: MAX\n";
            }

            // Resonance cost
            if (!_boot.ResMgr.IsMaxResonance(card))
            {
                _boot.ResMgr.PreviewResonanceCost(card, out int rd, out int rg, out int rf, out ResonanceLevel rt);
                preview += $"Resonance ({rt}): {rd}D, {rg}G, {rf}F";
            }
            else
            {
                preview += "Resonance: MAX";
            }

            costPreviewText.text = preview;
        }

        private void RefreshButtonStates()
        {
            var card = _boot.ActiveCard;

            levelUpButton.interactable =
                card.CurrentLevel < card.data.MaxLevelForTier(card.CurrentTier)
                && _boot.LevelMgr.CanAffordLevelUp(card);

            levelUpMaxButton.interactable =
                card.CurrentLevel < card.data.MaxLevelForTier(card.CurrentTier);

            tierUpButton.interactable =
                _boot.TierMgr.CanAffordTierUpgrade(card);

            resonanceUpButton.interactable =
                _boot.ResMgr.CanAffordResonanceUp(card);
        }

        // ── Log ──────────────────────────────────────────────────────

        private void Log(string message)
        {
            _log += message + "\n";
            logText.text = _log;
            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
                logScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}