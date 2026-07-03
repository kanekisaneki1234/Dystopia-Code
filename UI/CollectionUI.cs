using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Dystopia.Cards;
using Dystopia.Core;

namespace Dystopia.UI
{
    public class CollectionUI : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject cardSlotPrefab;

        [Header("Grid")]
        [SerializeField] private Transform  cardGridParent;
        [SerializeField] private GameObject emptyStateText;

        [Header("Search & Sort")]
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private TMP_Dropdown   sortDropdown;     // Alphabetical/Level/Tier/Resonance/Power
        [SerializeField] private Button         sortDirectionToggle;
        [SerializeField] private TMP_Text       sortDirectionLabel;

        [Header("Filters")]
        [SerializeField] private TMP_Dropdown classFilterDropdown;  // options: All, Assassin, Tank, Mage, Healer, Support, Guardian
        [SerializeField] private TMP_Dropdown rankFilterDropdown;   // options: All, D, C, B, A, S

        [Header("Tabs")]
        [SerializeField] private GameObject collectionViewRoot;
        [SerializeField] private GameObject deckViewRoot;
        [SerializeField] private Button     collectionTabButton;
        [SerializeField] private Button     deckTabButton;

        [Header("Top-bar currency")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text fragmentsText;

        [Header("Assign-mode label")]
        [SerializeField] private GameObject assignModeLabel;

        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private string mainMenuScene = "MainMenuScene";

        // ── Filter state ──────────────────────────────────────────────────────
        private CardClass? _filterClass;
        private CardRank?  _filterRank;
        private string     _searchText  = "";
        private int        _sortMode;      // 0=Alpha 1=Level 2=Tier 3=Resonance 4=Power
        private bool       _sortAsc      = true;

        // ── Deck assignment ───────────────────────────────────────────────────
        private bool                  _deckAssignMode;
        private Action<CardInstance>  _deckAssignCallback;

        // ── Card tracking ─────────────────────────────────────────────────────
        private readonly Dictionary<CardInstance, GameObject> _cardObjects = new();

        private CollectionBootstrapper _boot;

        private void OnDestroy()
        {
            if (_boot == null) return;
            _boot.Collection.OnCardAdded   -= AddCardToGrid;
            _boot.Collection.OnCardRemoved -= RemoveCardFromGrid;
            _boot.Wallet.OnCurrencyChanged -= UpdateCurrencyDisplay;
        }

        // ── Initialise ────────────────────────────────────────────────────────

        public void Initialise(CollectionBootstrapper boot)
        {
            _boot = boot;

            boot.Collection.OnCardAdded   += AddCardToGrid;
            boot.Collection.OnCardRemoved += RemoveCardFromGrid;
            boot.Wallet.OnCurrencyChanged += UpdateCurrencyDisplay;

            // Seed cards already loaded before this scene opened (singleton fetch happens at login)
            foreach (var card in boot.Collection.OwnedCards)
                AddCardToGrid(card);

            // Initial currency display
            UpdateCurrencyDisplay("Gold",      boot.Wallet.Gold);
            UpdateCurrencyDisplay("Fragments", boot.Wallet.Fragments);

            if (backButton != null)
                backButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));

            // Search
            if (searchField != null)
                searchField.onValueChanged.AddListener(text => { _searchText = text; ApplyFilters(); });

            // Sort dropdown
            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(idx => { _sortMode = idx; ApplyFilters(); });

            // Sort direction
            if (sortDirectionToggle != null)
                sortDirectionToggle.onClick.AddListener(ToggleSortDirection);

            // Class filter
            if (classFilterDropdown != null)
                classFilterDropdown.onValueChanged.AddListener(idx =>
                {
                    _filterClass = idx == 0 ? (CardClass?)null : ClassOrder[idx - 1];
                    ApplyFilters();
                });

            // Rank filter
            if (rankFilterDropdown != null)
                rankFilterDropdown.onValueChanged.AddListener(idx =>
                {
                    _filterRank = idx == 0 ? (CardRank?)null : RankOrder[idx - 1];
                    ApplyFilters();
                });

            // Tabs
            if (collectionTabButton != null)
                collectionTabButton.onClick.AddListener(ShowCollectionTab);
            if (deckTabButton != null)
                deckTabButton.onClick.AddListener(ShowDeckTab);

            ShowCollectionTab();
        }

        // ── Tab switching ─────────────────────────────────────────────────────

        public void ShowCollectionTab()
        {
            if (collectionViewRoot != null) collectionViewRoot.SetActive(true);
            if (deckViewRoot       != null) deckViewRoot.SetActive(false);
            SetTabColors(activeTab: collectionTabButton, inactiveTab: deckTabButton);
        }

        public void ShowDeckTab()
        {
            if (collectionViewRoot != null) collectionViewRoot.SetActive(false);
            if (deckViewRoot       != null) deckViewRoot.SetActive(true);
            SetTabColors(activeTab: deckTabButton, inactiveTab: collectionTabButton);
        }

        private static readonly Color TabActiveColor   = Color.white;
        private static readonly Color TabInactiveColor = new Color(0.6f, 0.6f, 0.6f);

        private void SetTabColors(Button activeTab, Button inactiveTab)
        {
            if (activeTab   != null && activeTab.image   != null) activeTab.image.color   = TabActiveColor;
            if (inactiveTab != null && inactiveTab.image != null) inactiveTab.image.color = TabInactiveColor;
        }

        // ── Card grid ─────────────────────────────────────────────────────────

        private void AddCardToGrid(CardInstance card)
        {
            // Duplicate incoming — card is already displayed; just refresh its slot.
            if (_cardObjects.TryGetValue(card, out var existing) && existing != null)
            {
                existing.GetComponent<CardSlotUI>()?.Configure(card);
                return;
            }

            var go = Instantiate(cardSlotPrefab, cardGridParent);
            go.GetComponent<CardSlotUI>().Configure(card);

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnCardClicked(card));
                btn.interactable = true;
            }

            _cardObjects[card] = go;
            ApplyFilters();
        }

        private void RemoveCardFromGrid(CardInstance card)
        {
            if (_cardObjects.TryGetValue(card, out var go))
            {
                Destroy(go);
                _cardObjects.Remove(card);
            }
        }

        public void RefreshCard(CardInstance card)
        {
            if (_cardObjects.TryGetValue(card, out var go))
                go.GetComponent<CardSlotUI>()?.Configure(card);
        }

        // ── Filter & sort ─────────────────────────────────────────────────────

        private void ApplyFilters()
        {
            var visible = new List<(CardInstance card, GameObject go)>();
            var stale   = new List<CardInstance>();

            foreach (var (card, go) in _cardObjects)
            {
                if (go == null) { stale.Add(card); continue; }
                bool show = PassesFilter(card);
                go.SetActive(show);
                if (show) visible.Add((card, go));
            }

            foreach (var card in stale) _cardObjects.Remove(card);

            // Sort and reorder siblings
            IOrderedEnumerable<(CardInstance card, GameObject go)> sorted = _sortMode switch
            {
                1 => _sortAsc ? visible.OrderBy(x => x.card.CurrentLevel)
                              : visible.OrderByDescending(x => x.card.CurrentLevel),
                2 => _sortAsc ? visible.OrderBy(x => x.card.CurrentTier)
                              : visible.OrderByDescending(x => x.card.CurrentTier),
                3 => _sortAsc ? visible.OrderBy(x => (int)x.card.resonanceLevel)
                              : visible.OrderByDescending(x => (int)x.card.resonanceLevel),
                4 => _sortAsc ? visible.OrderBy(x => x.card.Attack + x.card.Defence + x.card.Speed + x.card.HP)
                              : visible.OrderByDescending(x => x.card.Attack + x.card.Defence + x.card.Speed + x.card.HP),
                _ => _sortAsc ? visible.OrderBy(x => x.card.data.cardName)
                              : visible.OrderByDescending(x => x.card.data.cardName),
            };

            int i = 0;
            foreach (var (_, go) in sorted)
                go.transform.SetSiblingIndex(i++);

            if (emptyStateText != null)
                emptyStateText.SetActive(visible.Count == 0 && _cardObjects.Count > 0);
        }

        private bool PassesFilter(CardInstance card)
        {
            if (_filterClass.HasValue && card.data.cardClass != _filterClass.Value) return false;
            if (_filterRank.HasValue  && card.data.rank      != _filterRank.Value)  return false;
            if (!string.IsNullOrEmpty(_searchText) &&
                !card.data.cardName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private void ToggleSortDirection()
        {
            _sortAsc = !_sortAsc;
            if (sortDirectionLabel != null) sortDirectionLabel.text = _sortAsc ? "↑" : "↓";
            ApplyFilters();
        }

        // ── Filter lookup tables ──────────────────────────────────────────────

        private static readonly CardClass[] ClassOrder =
            { CardClass.Assassin, CardClass.Tank, CardClass.Mage,
              CardClass.Healer,   CardClass.Support, CardClass.Guardian };

        private static readonly CardRank[] RankOrder =
            { CardRank.D, CardRank.C, CardRank.B, CardRank.A, CardRank.S };

        // ── Card click ────────────────────────────────────────────────────────

        private void OnCardClicked(CardInstance card)
        {
            if (_deckAssignMode)
            {
                var cb = _deckAssignCallback;
                ExitDeckAssignMode();
                cb?.Invoke(card);
                ShowDeckTab();
            }
            else
            {
                _boot.DetailSheet?.Open(card);
            }
        }

        // ── Deck assignment mode ──────────────────────────────────────────────

        public void EnterDeckAssignMode(Action<CardInstance> onSelected)
        {
            _deckAssignMode     = true;
            _deckAssignCallback = onSelected;
            if (assignModeLabel != null) assignModeLabel.SetActive(true);
            ShowCollectionTab();
        }

        public void ExitDeckAssignMode()
        {
            _deckAssignMode     = false;
            _deckAssignCallback = null;
            if (assignModeLabel != null) assignModeLabel.SetActive(false);
        }

        // ── Currency display ──────────────────────────────────────────────────

        private void UpdateCurrencyDisplay(string name, int value)
        {
            if (name == "Gold"      && goldText      != null) goldText.text      = $"Gold: {value}";
            if (name == "Fragments" && fragmentsText != null) fragmentsText.text = $"Frags: {value}";
        }
    }
}