using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Dystopia.Cards;
using Dystopia.Economy.Data;
using Dystopia.Economy.Services;
using Dystopia.Networking;

namespace Dystopia.UI
{
    public class MarketUI : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Button    backButton;
        [SerializeField] private TMP_Text  goldText;
        [SerializeField] private Button    myListingsButton;
        [SerializeField] private Button    browseButton;

        [Header("Market Browse View")]
        [SerializeField] private GameObject     marketViewRoot;
        [SerializeField] private Transform      listingsGrid;
        [SerializeField] private GameObject     listingCardPrefab;
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private TMP_Dropdown   sortDropdown;
        [SerializeField] private TMP_Text       browseEmptyText;

        [Header("Selected Card Panel")]
        [SerializeField] private GameObject selectedPanel;
        [SerializeField] private TMP_Text   selectedCardName;
        [SerializeField] private TMP_Text   selectedTypeLabel;
        [SerializeField] private TMP_Text   selectedLevelText;
        [SerializeField] private TMP_Text   selectedPriceText;
        [SerializeField] private TMP_Text   selectedSellerText;
        [SerializeField] private TMP_Text   selectedCooldownText;
        [SerializeField] private Button     buyButton;
        [SerializeField] private TMP_Text   affordabilityText;

        [Header("My Listings View")]
        [SerializeField] private GameObject myListingsViewRoot;
        [SerializeField] private Transform  myListingsGrid;
        [SerializeField] private GameObject myListingPrefab;
        [SerializeField] private TMP_Text   myListingsEmptyText;

        [Header("Sell Panel")]
        [SerializeField] private GameObject     sellPanelRoot;
        [SerializeField] private Button         sellPanelBackdrop;
        [SerializeField] private Transform      sellCardGrid;
        [SerializeField] private GameObject     cardSlotPrefab;
        [SerializeField] private GameObject     selectedSellInfo;
        [SerializeField] private TMP_Text       sellCardNameText;
        [SerializeField] private TMP_Text       sellDupeCountText;
        [SerializeField] private TMP_Text       sellTypeText;    // shows "Selling: Duplicate" or "Selling: Original"
        [SerializeField] private TMP_Text       sellTypeWarning; // shown only when selling original
        [SerializeField] private TMP_InputField priceInput;
        [SerializeField] private TMP_Text       listingFeeText;
        [SerializeField] private Button         confirmSellButton;
        [SerializeField] private Button         cancelSellButton;

        [Header("Sell Float Button")]
        [SerializeField] private Button sellButton;

        [Header("Scene")]
        [SerializeField] private string mainMenuScene = "MainMenuScene";

        // ── Services ──────────────────────────────────────────────────────────
        private MarketService      _market;
        private WalletService      _wallet;
        private CollectionService  _collection;
        private CloudScriptService _cloudSvc;
        private CardDatabase       _cardDatabase;

        // ── State ─────────────────────────────────────────────────────────────
        private List<MarketListing> _browseListings  = new List<MarketListing>();
        private MarketListing       _selectedListing;
        private CardInstance        _selectedSellCard;
        private string              _selectedListingType;
        private bool                _purchaseInFlight;
        private bool                _listingInFlight;

        // ── Events ────────────────────────────────────────────────────────────
        private Action<string, int> _onCurrencyChanged;

        public void Initialise(MarketService market, WalletService wallet,
            CollectionService collection, CloudScriptService cloudSvc, CardDatabase cardDatabase)
        {
            _market       = market;
            _wallet       = wallet;
            _collection   = collection;
            _cloudSvc     = cloudSvc;
            _cardDatabase = cardDatabase;

            _onCurrencyChanged = (_, __) => RefreshGoldDisplay();
            _wallet.OnCurrencyChanged += _onCurrencyChanged;

            backButton?.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));
            myListingsButton?.onClick.AddListener(ShowMyListings);
            browseButton?.onClick.AddListener(ShowBrowse);
            buyButton?.onClick.AddListener(OnBuyClicked);
            sellButton?.onClick.AddListener(OnSellButtonClicked);
            cancelSellButton?.onClick.AddListener(CloseSellPanel);
            sellPanelBackdrop?.onClick.AddListener(CloseSellPanel);
            confirmSellButton?.onClick.AddListener(OnConfirmSell);
            priceInput?.onValueChanged.AddListener(_ => UpdateListingFeeDisplay());
            searchField?.onValueChanged.AddListener(_ => ApplySortFilter());
            sortDropdown?.onValueChanged.AddListener(_ => ApplySortFilter());

            selectedPanel?.SetActive(false);
            sellPanelRoot?.SetActive(false);
            myListingsViewRoot?.SetActive(false);
            marketViewRoot?.SetActive(true);

            RefreshGoldDisplay();
            LoadBrowseListings();

            StartCoroutine(UpdateCooldownTimers());
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.OnCurrencyChanged -= _onCurrencyChanged;
        }

        // ── Browse ────────────────────────────────────────────────────────────

        private void LoadBrowseListings()
        {
            _market.GetMarketListings(listings =>
            {
                _browseListings = listings;
                ApplySortFilter();
            }, err => Debug.LogError($"[MarketUI] GetMarketListings: {err}"));
        }

        private void ApplySortFilter()
        {
            string search = searchField != null ? searchField.text.ToLower() : "";
            var filtered = _browseListings
                .Where(l => string.IsNullOrEmpty(search) || l.cardId.ToLower().Contains(search))
                .ToList();

            int sort = sortDropdown != null ? sortDropdown.value : 0;
            filtered = sort switch
            {
                1 => filtered.OrderBy(l => l.price).ToList(),
                2 => filtered.OrderByDescending(l => l.price).ToList(),
                _ => filtered.OrderBy(l => l.cardId).ToList()
            };

            RebuildBrowseGrid(filtered);
        }

        private void RebuildBrowseGrid(List<MarketListing> listings)
        {
            foreach (Transform child in listingsGrid) Destroy(child.gameObject);

            bool empty = listings.Count == 0;
            if (browseEmptyText) browseEmptyText.gameObject.SetActive(empty);

            foreach (var listing in listings)
            {
                var go       = Instantiate(listingCardPrefab, listingsGrid);
                var ui       = go.GetComponent<MarketListingUI>();
                var cardData = _cardDatabase != null ? _cardDatabase.GetByCardId(listing.cardId) : null;
                ui?.Configure(listing, cardData, OnListingSelected);
            }
        }

        private void OnListingSelected(MarketListing listing)
        {
            _selectedListing = listing;
            selectedPanel?.SetActive(true);

            var cd = _cardDatabase != null ? _cardDatabase.GetByCardId(listing.cardId) : null;
            if (selectedCardName)   selectedCardName.text   = cd != null ? cd.cardName : listing.cardId;
            if (selectedTypeLabel)  selectedTypeLabel.text  = listing.listingType == "original" ? "Original" : "Duplicate";
            if (selectedPriceText)  selectedPriceText.text  = $"{listing.price} GD";
            if (selectedSellerText) selectedSellerText.text = $"Seller: {listing.sellerName}";
            if (selectedLevelText && listing.cardData != null)
                selectedLevelText.text = $"Lv.{listing.cardData.level}  T{listing.cardData.tier}  R{listing.cardData.resonance}";

            UpdateBuyAffordability();
            UpdateSelectedCooldownText();
        }

        private void UpdateBuyAffordability()
        {
            if (_selectedListing == null) return;
            int gold = _wallet?.Gold ?? 0;
            bool canAfford = gold >= _selectedListing.price;
            if (buyButton) buyButton.interactable = canAfford && !_purchaseInFlight;
            if (affordabilityText)
            {
                affordabilityText.text  = canAfford ? "" : $"Need {_selectedListing.price - gold} more GD";
                affordabilityText.color = canAfford ? Color.green : Color.red;
            }
        }

        private void UpdateSelectedCooldownText()
        {
            if (_selectedListing == null || selectedCooldownText == null) return;
            var end = System.DateTime.Parse(_selectedListing.cooldownEndsAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind);
            var remaining = end - System.DateTime.UtcNow;
            selectedCooldownText.text = remaining.TotalSeconds <= 0
                ? "Available"
                : remaining.TotalHours >= 1
                    ? $"Cooldown: {(int)remaining.TotalHours}h {remaining.Minutes}m"
                    : $"Cooldown: {remaining.Minutes}m {remaining.Seconds}s";
        }

        private IEnumerator UpdateCooldownTimers()
        {
            while (true)
            {
                yield return new WaitForSeconds(30f);
                if (selectedPanel != null && selectedPanel.activeSelf)
                    UpdateSelectedCooldownText();
            }
        }

        private void OnBuyClicked()
        {
            if (_purchaseInFlight || _selectedListing == null) return;
            _purchaseInFlight = true;
            if (buyButton) buyButton.interactable = false;

            _market.BuyCard(_selectedListing, result =>
            {
                _purchaseInFlight = false;
                selectedPanel?.SetActive(false);
                _selectedListing = null;
                LoadBrowseListings();
            }, err =>
            {
                _purchaseInFlight = false;
                if (buyButton) buyButton.interactable = true;
                Debug.LogError($"[MarketUI] BuyCard failed: {err}");
            });
        }

        // ── My Listings ───────────────────────────────────────────────────────

        private void ShowMyListings()
        {
            marketViewRoot?.SetActive(false);
            myListingsViewRoot?.SetActive(true);
            selectedPanel?.SetActive(false);
            LoadMyListings();
        }

        private void ShowBrowse()
        {
            myListingsViewRoot?.SetActive(false);
            marketViewRoot?.SetActive(true);
            LoadBrowseListings();
        }

        private void LoadMyListings()
        {
            _market.GetMyListings(listings =>
            {
                RebuildMyListingsGrid(listings);
            }, err => Debug.LogError($"[MarketUI] GetMyListings: {err}"));
        }

        private void RebuildMyListingsGrid(List<MarketListing> listings)
        {
            foreach (Transform child in myListingsGrid) Destroy(child.gameObject);

            bool empty = listings.Count == 0;
            if (myListingsEmptyText) myListingsEmptyText.gameObject.SetActive(empty);

            foreach (var listing in listings)
            {
                var go       = Instantiate(myListingPrefab, myListingsGrid);
                var ui       = go.GetComponent<MarketMyListingUI>();
                var cardData = _cardDatabase != null ? _cardDatabase.GetByCardId(listing.cardId) : null;
                ui?.Configure(listing, cardData, OnDelistClicked);
            }
        }

        private void OnDelistClicked(MarketListing listing)
        {
            _market.DelistCard(listing, _ => LoadMyListings(),
                err => Debug.LogError($"[MarketUI] DelistCard: {err}"));
        }

        // ── Sell Panel ────────────────────────────────────────────────────────

        private void OnSellButtonClicked()
        {
            sellPanelRoot?.SetActive(true);
            if (selectedSellInfo) selectedSellInfo.SetActive(false);
            _selectedSellCard    = null;
            _selectedListingType = null;
            PopulateSellCardGrid();
        }

        private void CloseSellPanel()
        {
            sellPanelRoot?.SetActive(false);
            _selectedSellCard    = null;
            _selectedListingType = null;
        }

        private void PopulateSellCardGrid()
        {
            if (sellCardGrid == null || cardSlotPrefab == null) return;
            foreach (Transform child in sellCardGrid) Destroy(child.gameObject);

            foreach (var card in _collection.OwnedCards)
            {
                var go  = Instantiate(cardSlotPrefab, sellCardGrid);
                var slot = go.GetComponent<CardSlotUI>();
                slot?.Configure(card);

                bool onCooldown = !string.IsNullOrEmpty(card.MarketCooldownUntil) &&
                    System.DateTime.TryParse(card.MarketCooldownUntil, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var cooldownEnd) &&
                    cooldownEnd > System.DateTime.UtcNow;

                var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
                if (btn)
                {
                    btn.interactable = !onCooldown;
                    var captured = card;
                    btn.onClick.RemoveAllListeners();
                    if (!onCooldown)
                        btn.onClick.AddListener(() => OnSellCardSelected(captured));
                }
            }
        }

        private void OnSellCardSelected(CardInstance card)
        {
            _selectedSellCard = card;
            if (selectedSellInfo) selectedSellInfo.SetActive(true);

            if (sellCardNameText)  sellCardNameText.text  = card.data.cardName;
            if (sellDupeCountText) sellDupeCountText.text = $"Duplicates: {card.duplicateCount}";

            // Type is always determined by duplicates — no user choice allowed.
            // Must sell a duplicate first if any exist; only then can the original be listed.
            _selectedListingType = card.duplicateCount >= 1 ? "duplicate" : "original";

            bool isOriginal = _selectedListingType == "original";
            if (sellTypeText)    sellTypeText.text    = isOriginal ? "Selling: Original" : "Selling: Duplicate";
            if (sellTypeWarning) sellTypeWarning.text = isOriginal
                ? "This will remove the card from your collection."
                : "";

            UpdateListingFeeDisplay();
        }

        private void UpdateListingFeeDisplay()
        {
            if (!int.TryParse(priceInput != null ? priceInput.text : "0", out int price) || price < 1)
            {
                if (listingFeeText) listingFeeText.text = "Fee: -";
                return;
            }
            int fee = Math.Max(10, Mathf.FloorToInt(price * 0.05f));
            if (listingFeeText) listingFeeText.text = $"Fee: {fee} GD";
        }

        private void OnConfirmSell()
        {
            if (_listingInFlight || _selectedSellCard == null || string.IsNullOrEmpty(_selectedListingType))
                return;
            if (!int.TryParse(priceInput != null ? priceInput.text : "", out int price) || price < 1)
                return;

            _listingInFlight = true;
            if (confirmSellButton) confirmSellButton.interactable = false;

            _market.ListCard(_selectedSellCard, price, _selectedListingType, result =>
            {
                _listingInFlight = false;
                CloseSellPanel();
                PopulateSellCardGrid();
            }, err =>
            {
                _listingInFlight = false;
                if (confirmSellButton) confirmSellButton.interactable = true;
                Debug.LogError($"[MarketUI] ListCard failed: {err}");
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshGoldDisplay()
        {
            if (goldText != null) goldText.text = $"{_wallet?.Gold ?? 0} GD";
            UpdateBuyAffordability();
        }
    }
}
