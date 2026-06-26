using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Services;

namespace Dystopia.UI
{
    public class DeckView : MonoBehaviour
    {
        [Header("Deck Selectors")]
        [SerializeField] private Button[] deckSelectorButtons;   // 3 buttons: Deck 1 / 2 / 3

        [Header("Deck Slots (one root per slot)")]
        [SerializeField] private GameObject[] slotRoots;         // 3 GameObjects, each wraps a card display
        [SerializeField] private CardSlotUI[]  slotCardUIs;      // CardSlotUI per slot (child of slotRoot)
        [SerializeField] private TMP_Text[]    slotEmptyLabels;  // "+" TMP_Text per slot
        [SerializeField] private Button[]      slotAssignButtons;
        [SerializeField] private Button[]      slotRemoveButtons;

        [Header("Deck Stats")]
        [SerializeField] private TMP_Text deckAtkText;
        [SerializeField] private TMP_Text deckDefText;
        [SerializeField] private TMP_Text deckSpdText;
        [SerializeField] private TMP_Text deckHpText;

        [Header("Battle Selection")]
        [SerializeField] private Button   selectForBattleButton;
        [SerializeField] private TMP_Text selectedFeedbackText;  // brief "Deck selected!" label (optional)

        // ── Private state ─────────────────────────────────────────────────────
        private CollectionBootstrapper _boot;
        private int _activeDeck;

        // ── Initialise ────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (_boot == null) return;
            _boot.DeckSvc.OnDecksLoaded -= OnDecksLoaded;
            _boot.DeckSvc.OnDeckChanged -= RefreshAllSlots;
        }

        public void Initialise(CollectionBootstrapper boot)
        {
            _boot = boot;

            boot.DeckSvc.OnDecksLoaded += OnDecksLoaded;
            boot.DeckSvc.OnDeckChanged += RefreshAllSlots;

            // Deck selector buttons
            for (int i = 0; i < deckSelectorButtons.Length; i++)
            {
                if (deckSelectorButtons[i] == null) continue;
                int captured = i;
                deckSelectorButtons[i].onClick.AddListener(() => SelectDeck(captured));
            }

            // Slot assign buttons
            for (int i = 0; i < slotAssignButtons.Length; i++)
            {
                if (slotAssignButtons[i] == null) continue;
                int captured = i;
                slotAssignButtons[i].onClick.AddListener(() => OnSlotAssignPressed(captured));
            }

            // Slot remove buttons
            for (int i = 0; i < slotRemoveButtons.Length; i++)
            {
                if (slotRemoveButtons[i] == null) continue;
                int captured = i;
                slotRemoveButtons[i].onClick.AddListener(() => OnSlotRemovePressed(captured));
            }

            if (selectForBattleButton != null)
                selectForBattleButton.onClick.AddListener(OnSelectForBattle);

            if (selectedFeedbackText != null) selectedFeedbackText.gameObject.SetActive(false);

            RefreshDeckSelectorColors();
            RefreshAllSlots();
        }

        // ── Deck selection ────────────────────────────────────────────────────

        private static readonly Color DeckBtnActive   = Color.white;
        private static readonly Color DeckBtnInactive = new(0.6f, 0.6f, 0.6f);

        private void SelectDeck(int index)
        {
            _activeDeck = index;
            RefreshDeckSelectorColors();
            RefreshAllSlots();
        }

        private void RefreshDeckSelectorColors()
        {
            for (int i = 0; i < deckSelectorButtons.Length; i++)
            {
                if (deckSelectorButtons[i] == null || deckSelectorButtons[i].image == null) continue;
                deckSelectorButtons[i].image.color = i == _activeDeck ? DeckBtnActive : DeckBtnInactive;
            }
        }

        // ── Slot display ──────────────────────────────────────────────────────

        private void RefreshAllSlots()
        {
            var deck = _boot.DeckSvc.GetDeck(_activeDeck);

            int totalAtk = 0, totalDef = 0, totalSpd = 0, totalHp = 0;

            for (int s = 0; s < DeckService.SlotCount; s++)
            {
                var card = s < deck.Length ? deck[s] : null;
                bool hasCard = card != null;

                if (slotCardUIs != null && s < slotCardUIs.Length && slotCardUIs[s] != null)
                {
                    slotCardUIs[s].gameObject.SetActive(hasCard);
                    if (hasCard) slotCardUIs[s].Configure(card);
                }

                // Show/hide empty label
                if (slotEmptyLabels != null && s < slotEmptyLabels.Length && slotEmptyLabels[s] != null)
                    slotEmptyLabels[s].gameObject.SetActive(!hasCard);

                // Show/hide card UI root (if distinct from empty label parent)
                if (slotRoots != null && s < slotRoots.Length && slotRoots[s] != null)
                    slotRoots[s].SetActive(true);

                // Assign button only visible when slot is empty
                if (slotAssignButtons != null && s < slotAssignButtons.Length && slotAssignButtons[s] != null)
                    slotAssignButtons[s].gameObject.SetActive(!hasCard);

                // Remove button only visible when a card is assigned
                if (slotRemoveButtons != null && s < slotRemoveButtons.Length && slotRemoveButtons[s] != null)
                    slotRemoveButtons[s].gameObject.SetActive(hasCard);

                if (hasCard)
                {
                    totalAtk += card.Attack;
                    totalDef += card.Defence;
                    totalSpd += card.Speed;
                    totalHp  += card.HP;
                }
            }

            // Aggregate stats
            if (deckAtkText != null) deckAtkText.text = $"ATK: {totalAtk}";
            if (deckDefText != null) deckDefText.text = $"DEF: {totalDef}";
            if (deckSpdText != null) deckSpdText.text = $"SPD: {totalSpd}";
            if (deckHpText  != null) deckHpText.text  = $"HP: {totalHp}";
        }

        // ── Slot interactions ─────────────────────────────────────────────────

        private void OnSlotAssignPressed(int slotIdx)
        {
            _boot.CollectionUI?.EnterDeckAssignMode(card =>
            {
                if (_boot.DeckSvc.IsDuplicate(_activeDeck, card))
                {
                    ShowFeedback("Card already in this deck!");
                    return;
                }
                _boot.DeckSvc.SetCard(_activeDeck, slotIdx, card);
            });
        }

        private void OnSlotRemovePressed(int slotIdx)
        {
            _boot.DeckSvc.RemoveCard(_activeDeck, slotIdx);
        }

        // ── Battle selection ──────────────────────────────────────────────────

        private void OnDecksLoaded()
        {
            _activeDeck = _boot.DeckSvc.ActiveBattleDeckIndex;
            RefreshDeckSelectorColors();
            RefreshAllSlots();
            var deck = _boot.DeckSvc.GetDeck(_activeDeck);
            GameSession.SetActiveDeck(_activeDeck, deck.Where(c => c != null).ToList());
        }

        private void OnSelectForBattle()
        {
            _boot.DeckSvc.SelectForBattle(_activeDeck);
            var deck  = _boot.DeckSvc.GetDeck(_activeDeck);
            var cards = deck.Where(c => c != null).ToList();
            GameSession.SetActiveDeck(_activeDeck, cards);
            ShowFeedback($"Deck {_activeDeck + 1} selected!");
        }

        private void ShowFeedback(string message)
        {
            if (selectedFeedbackText == null) return;
            selectedFeedbackText.text = message;
            selectedFeedbackText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), 2f);
        }

        private void HideFeedback()
        {
            if (selectedFeedbackText != null) selectedFeedbackText.gameObject.SetActive(false);
        }

    }
}