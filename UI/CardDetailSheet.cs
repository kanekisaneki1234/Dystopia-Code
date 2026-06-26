using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Cards;
using Dystopia.Core;

namespace Dystopia.UI
{
    public class CardDetailSheet : MonoBehaviour
    {
        // ── Header ────────────────────────────────────────────────────────────
        [Header("Header")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text classText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text levelSummaryText;
        [SerializeField] private TMP_Text dupeText;

        // ── Stats ─────────────────────────────────────────────────────────────
        [Header("Stats")]
        [SerializeField] private TMP_Text atkText;
        [SerializeField] private TMP_Text defText;
        [SerializeField] private TMP_Text spdText;
        [SerializeField] private TMP_Text hpText;

        // ── Ability ───────────────────────────────────────────────────────────
        [Header("Ability")]
        [SerializeField] private TMP_Text abilityNameText;
        [SerializeField] private TMP_Text manaCostText;
        [SerializeField] private TMP_Text abilityDescText;

        // ── Upgrade preview ───────────────────────────────────────────────────
        [Header("Upgrade Preview")]
        [SerializeField] private GameObject upgradePreviewRoot;
        [SerializeField] private TMP_Text   previewHeaderText;
        [SerializeField] private TMP_Text   previewCostText;
        [SerializeField] private TMP_Text   previewAffordText;

        // ── Action buttons ────────────────────────────────────────────────────
        [Header("Action Buttons")]
        [SerializeField] private Button   levelUpButton;
        [SerializeField] private Button   tierUpButton;
        [SerializeField] private Button   resonateButton;
        [SerializeField] private TMP_Text levelUpLabel;
        [SerializeField] private TMP_Text tierUpLabel;
        [SerializeField] private TMP_Text resonateLabel;

        // ── Close ─────────────────────────────────────────────────────────────
        [Header("Close")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;

        // ── Private state ─────────────────────────────────────────────────────
        private CardInstance           _card;
        private Action<CardInstance, int>            _onLevelUp;
        private Action<CardInstance, int>            _onTierUpgrade;
        private Action<CardInstance, ResonanceLevel> _onResonanceUp;
        private Action<string, int>                  _onCurrencyChanged;
        private CollectionBootstrapper _boot;

        private enum PendingAction { None, LevelUp, TierUp, Resonate }
        private PendingAction _pending;

        private const string DefaultLevelUpLabel  = "Level Up";
        private const string DefaultTierUpLabel   = "Tier Up";
        private const string DefaultResonateLabel = "Resonate";
        private const string ConfirmLabel         = "Confirm";

        private void OnDestroy()
        {
            if (_boot == null) return;
            _boot.LevelMgr.OnLevelUp       -= _onLevelUp;
            _boot.TierMgr.OnTierUpgrade    -= _onTierUpgrade;
            _boot.ResMgr.OnResonanceUp     -= _onResonanceUp;
            _boot.Wallet.OnCurrencyChanged -= _onCurrencyChanged;
        }

        // ── Initialise ────────────────────────────────────────────────────────

        public void Initialise(CollectionBootstrapper boot)
        {
            _boot = boot;

            // Progression events — refresh sheet + grid card after any upgrade
            _onLevelUp        = (card, _) => OnUpgradeDone(card);
            _onTierUpgrade    = (card, _) => OnUpgradeDone(card);
            _onResonanceUp    = (card, _) => OnUpgradeDone(card);
            _onCurrencyChanged = (_, __) => { if (_card != null) RefreshAffordability(); };

            boot.LevelMgr.OnLevelUp       += _onLevelUp;
            boot.TierMgr.OnTierUpgrade    += _onTierUpgrade;
            boot.ResMgr.OnResonanceUp     += _onResonanceUp;
            boot.Wallet.OnCurrencyChanged += _onCurrencyChanged;

            // Button wiring
            if (levelUpButton  != null) levelUpButton.onClick.AddListener( () => OnActionButton(PendingAction.LevelUp));
            if (tierUpButton   != null) tierUpButton.onClick.AddListener(  () => OnActionButton(PendingAction.TierUp));
            if (resonateButton != null) resonateButton.onClick.AddListener(() => OnActionButton(PendingAction.Resonate));
            if (closeButton    != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);

            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open(CardInstance card)
        {
            _card   = card;
            _pending = PendingAction.None;
            gameObject.SetActive(true);
            Populate();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            _card    = null;
            _pending = PendingAction.None;
        }

        // ── Populate ──────────────────────────────────────────────────────────

        private void Populate()
        {
            if (_card == null) return;

            // Header
            if (nameText         != null) nameText.text         = _card.data.cardName;
            if (classText        != null) classText.text        = _card.data.cardClass.ToString();
            if (rankText         != null) rankText.text         = _card.data.rank.ToString();
            if (levelSummaryText != null)
                levelSummaryText.text = $"Lv.{_card.CurrentLevel} · T{_card.CurrentTier} · {_card.resonanceLevel}";

            // Dupe count vs next resonance requirement
            if (dupeText != null)
            {
                _boot.ResMgr.PreviewResonanceCost(_card, out int dupsNeeded, out _, out _, out _);
                dupeText.text = $"Dupes: {_card.duplicateCount}/{dupsNeeded}";
            }

            // Stats
            if (atkText != null) atkText.text = $"ATK: {_card.Attack}";
            if (defText != null) defText.text = $"DEF: {_card.Defence}";
            if (spdText != null) spdText.text = $"SPD: {_card.Speed}";
            if (hpText  != null) hpText.text  = $"HP: {_card.HP}";

            // Ability
            var ability = _card.data.ability;
            if (abilityNameText != null) abilityNameText.text = ability?.abilityName ?? "—";
            if (manaCostText    != null) manaCostText.text    = ability != null ? $"Mana: {ability.ManaCost}" : "";
            if (abilityDescText != null) abilityDescText.text = ability?.GetShortDescription(_card) ?? "";

            // Reset preview and button state
            if (upgradePreviewRoot != null) upgradePreviewRoot.SetActive(false);
            ResetButtonLabels();
            RefreshAffordability();
        }

        // ── Action button logic ───────────────────────────────────────────────

        private void OnActionButton(PendingAction action)
        {
            if (_card == null) return;

            if (_pending == action)
            {
                ExecutePending();
                return;
            }

            _pending = action;
            ShowPreview(action);

            // Change tapped button to "Confirm", reset others
            if (levelUpLabel  != null) levelUpLabel.text  = action == PendingAction.LevelUp  ? ConfirmLabel : DefaultLevelUpLabel;
            if (tierUpLabel   != null) tierUpLabel.text   = action == PendingAction.TierUp   ? ConfirmLabel : DefaultTierUpLabel;
            if (resonateLabel != null) resonateLabel.text = action == PendingAction.Resonate ? ConfirmLabel : DefaultResonateLabel;
        }

        private void ExecutePending()
        {
            if (_card == null) return;

            switch (_pending)
            {
                case PendingAction.LevelUp:  _boot.LevelMgr.TryLevelUp(_card);     break;
                case PendingAction.TierUp:   _boot.TierMgr.TryTierUpgrade(_card);  break;
                case PendingAction.Resonate: _boot.ResMgr.TryResonanceUp(_card);   break;
            }

            _pending = PendingAction.None;
            ResetButtonLabels();
            if (upgradePreviewRoot != null) upgradePreviewRoot.SetActive(false);
        }

        // ── Preview panel ─────────────────────────────────────────────────────

        private void ShowPreview(PendingAction action)
        {
            if (upgradePreviewRoot == null || _card == null) return;
            upgradePreviewRoot.SetActive(true);

            switch (action)
            {
                case PendingAction.LevelUp:
                {
                    _boot.LevelMgr.PreviewLevelUpCost(_card, out int frags, out int gold);
                    if (previewHeaderText != null)
                        previewHeaderText.text = $"Lv.{_card.CurrentLevel} → {_card.CurrentLevel + 1}";
                    if (previewCostText != null)
                        previewCostText.text = $"Cost: {gold} GD + {frags} FR";
                    SetAffordColor(_boot.LevelMgr.CanAffordLevelUp(_card));
                    break;
                }
                case PendingAction.TierUp:
                {
                    _boot.TierMgr.PreviewTierUpgradeCost(_card, out int frags, out int gold, out int mats);
                    if (previewHeaderText != null)
                        previewHeaderText.text = $"T{_card.CurrentTier} → T{_card.CurrentTier + 1}";
                    if (previewCostText != null)
                        previewCostText.text = $"Cost: {gold} GD + {frags} FR + {mats} {_card.data.cardClass} Material";
                    SetAffordColor(_boot.TierMgr.CanAffordTierUpgrade(_card));
                    break;
                }
                case PendingAction.Resonate:
                {
                    _boot.ResMgr.PreviewResonanceCost(_card, out int dups, out int gold, out int frags, out var target);
                    if (previewHeaderText != null)
                        previewHeaderText.text = $"{_card.resonanceLevel} → {target}";
                    if (previewCostText != null)
                        previewCostText.text = $"Cost: {dups} dupes + {gold} GD + {frags} FR";
                    SetAffordColor(_boot.ResMgr.CanAffordResonanceUp(_card));
                    break;
                }
            }
        }

        private void SetAffordColor(bool canAfford)
        {
            if (previewAffordText == null) return;
            previewAffordText.text  = canAfford ? "Can afford" : "Cannot afford";
            previewAffordText.color = canAfford ? new Color(0.18f, 0.82f, 0.34f) : new Color(0.80f, 0.20f, 0.20f);
        }

        // ── Affordability refresh ─────────────────────────────────────────────

        private void RefreshAffordability()
        {
            if (_card == null) return;

            if (levelUpButton != null)
                levelUpButton.interactable = _boot.LevelMgr.CanAffordLevelUp(_card);

            if (tierUpButton != null)
                tierUpButton.interactable = _boot.TierMgr.CanAffordTierUpgrade(_card);

            if (resonateButton != null)
                resonateButton.interactable = _boot.ResMgr.CanAffordResonanceUp(_card);

            // Refresh preview affordability color if one is active
            if (_pending != PendingAction.None && upgradePreviewRoot != null && upgradePreviewRoot.activeSelf)
                ShowPreview(_pending);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ResetButtonLabels()
        {
            if (levelUpLabel  != null) levelUpLabel.text  = DefaultLevelUpLabel;
            if (tierUpLabel   != null) tierUpLabel.text   = DefaultTierUpLabel;
            if (resonateLabel != null) resonateLabel.text = DefaultResonateLabel;
        }

        public void RefreshIfOpen(CardInstance card)
        {
            if (_card == card) Populate();
        }

        private void OnUpgradeDone(CardInstance card)
        {
            _boot.CollectionUI?.RefreshCard(card);
            RefreshIfOpen(card);
        }
    }
}