using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Dystopia.UI
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(CanvasGroup))]
    public class AbilityButtonSlot : MonoBehaviour
    {
        [Header("Text References")]
        public TMP_Text cardNameText;
        public TMP_Text abilityNameText;
        public TMP_Text descriptionText;
        public TMP_Text manaCostText;

        [Header("Visual")]
        public GameObject nextMarker;   // "NEXT" round-robin indicator
        public Image      background;

        [Header("Colours")]
        public Color affordableColour   = new Color(0.18f, 0.52f, 0.89f);
        public Color unaffordableColour = new Color(0.35f, 0.35f, 0.35f);
        public Color selectedColour     = new Color(0.18f, 0.82f, 0.34f);
        public Color defaultHighlight   = new Color(0.95f, 0.77f, 0.06f);

        public event Action<int> OnSlotClicked;

        private Button      _button;
        private CanvasGroup _canvasGroup;
        private int         _slotIndex;

        private bool _isAffordable = true;
        private bool _isDefault    = false;
        private bool _isSelected   = false;

        private void Awake()
        {
            _button      = GetComponent<Button>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _button.onClick.AddListener(() => OnSlotClicked?.Invoke(_slotIndex));
        }

        public void Configure(int slotIndex, string cardName, string abilityName, string description, int manaCost)
        {
            _slotIndex = slotIndex;
            if (cardNameText)    cardNameText.text    = cardName;
            if (abilityNameText) abilityNameText.text = abilityName;
            if (descriptionText) descriptionText.text = description;
            if (manaCostText)    manaCostText.text    = $"{manaCost} MP";
        }

        public void SetAffordable(bool canAfford)
        {
            _isAffordable = canAfford;
            if (_button)      _button.interactable = canAfford;
            if (_canvasGroup) _canvasGroup.alpha   = canAfford ? 1f : 0.5f;
            UpdateBackground();
        }

        public void SetIsDefault(bool isDefault)
        {
            _isDefault = isDefault;
            if (nextMarker) nextMarker.SetActive(isDefault);
            UpdateBackground();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateBackground();
        }

        public void Lock()
        {
            if (_button) _button.interactable = false;
        }

        private void UpdateBackground()
        {
            if (!background) return;
            if (_isSelected) { background.color = selectedColour;   return; }
            if (_isDefault)  { background.color = defaultHighlight; return; }
            background.color = _isAffordable ? affordableColour : unaffordableColour;
        }
    }
}
