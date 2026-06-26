using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Core;
using Dystopia.Battle;

namespace Dystopia.UI
{
    public class AbilitySelectionUI : MonoBehaviour, IAbilitySelector
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Transform  _buttonContainer;
        [SerializeField] private Slider     _timerSlider;
        [SerializeField] private TMP_Text   _timerText;
        [SerializeField] private Button     _skipButton;

        // ── IAbilitySelector ──────────────────────────────────────────────
        public bool  IsReady      { get; private set; }
        public int?  SelectedSlot { get; private set; }
        public event Action OnReady;

        private BattleTeam              _team;
        private float                   _windowSeconds;
        private float                   _elapsed;
        private bool                    _active;
        private List<AbilityButtonSlot> _slots = new List<AbilityButtonSlot>();

        // ── IAbilitySelector impl ─────────────────────────────────────────
        public void BeginSelection(BattleTeam team, float windowSeconds)
        {
            _team          = team;
            _windowSeconds = windowSeconds;
            _elapsed       = 0f;
            IsReady        = false;
            SelectedSlot   = null;
            _active        = true;

            // Must enable the panel BEFORE building buttons so that Unity calls
            // Awake on instantiated slots immediately. If the panel is disabled,
            // Awake is deferred until the hierarchy becomes active — meaning
            // _button and _canvasGroup are null when RefreshAffordability runs.
            if (_panelRoot) _panelRoot.SetActive(true);

            BuildButtons();
            RefreshAffordability();
            HighlightDefault();

            if (_timerSlider)
            {
                _timerSlider.minValue = 0f;
                _timerSlider.maxValue = windowSeconds;
                _timerSlider.value   = windowSeconds;
            }

            if (_skipButton)
            {
                _skipButton.onClick.RemoveAllListeners();
                _skipButton.onClick.AddListener(OnSkip);
                _skipButton.gameObject.SetActive(true);
            }
        }

        public void ForceEnd()
        {
            if (IsReady) return;
            SelectedSlot = null;
            IsReady      = true;
            _active      = false;
            if (_panelRoot) _panelRoot.SetActive(false);
        }

        // ── Frame update (timer + affordability refresh) ──────────────────
        private void Update()
        {
            if (!_active || IsReady) return;

            _elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, _windowSeconds - _elapsed);

            if (_timerSlider) _timerSlider.value = remaining;
            if (_timerText)   _timerText.text    = $"{remaining:F1}s";

            RefreshAffordability();
        }

        // ── Button construction ───────────────────────────────────────────
        private void BuildButtons()
        {
            foreach (var s in _slots)
                if (s) Destroy(s.gameObject);
            _slots.Clear();

            if (!_buttonPrefab || !_buttonContainer) return;

            var activeCards = _team.GetActiveAbilityCards();
            for (int i = 0; i < activeCards.Count; i++)
            {
                var card = activeCards[i];
                var go   = Instantiate(_buttonPrefab, _buttonContainer);
                var slot = go.GetComponent<AbilityButtonSlot>();

                if (slot == null) { Destroy(go); continue; }

                int    cost = Mathf.RoundToInt(card.data.ability.ManaCost * card.ManaCostMultiplier);
                string desc = card.data.ability.GetShortDescription(card);

                slot.Configure(i, card.data.cardName, card.data.ability.abilityName, desc, cost);
                slot.OnSlotClicked += OnSlotSelected;
                _slots.Add(slot);
            }
        }

        private void RefreshAffordability()
        {
            var activeCards = _team.GetActiveAbilityCards();
            for (int i = 0; i < _slots.Count && i < activeCards.Count; i++)
            {
                var card = activeCards[i];
                int cost = Mathf.RoundToInt(card.data.ability.ManaCost * card.ManaCostMultiplier);
                _slots[i].SetAffordable(_team.CurrentMana >= cost);
            }
        }

        private void HighlightDefault()
        {
            int defaultSlot = _team.CurrentAbilitySlot;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetIsDefault(i == defaultSlot);
        }

        // ── Button event handlers ─────────────────────────────────────────
        private void OnSlotSelected(int slotIndex)
        {
            SelectedSlot = slotIndex;
            IsReady      = true;
            _active      = false;

            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetSelected(i == slotIndex);
                _slots[i].Lock();
            }

            if (_skipButton) _skipButton.gameObject.SetActive(false);
            if (_panelRoot) _panelRoot.SetActive(false);
            OnReady?.Invoke();
        }

        private void OnSkip()
        {
            SelectedSlot = null;
            IsReady      = true;
            _active      = false;

            foreach (var s in _slots) s.Lock();
            if (_skipButton) _skipButton.gameObject.SetActive(false);
            if (_panelRoot) _panelRoot.SetActive(false);
            OnReady?.Invoke();
        }
    }
}
