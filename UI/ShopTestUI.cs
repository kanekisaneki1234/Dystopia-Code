using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Dystopia.Cards;
using Dystopia.Economy.Data;

namespace Dystopia.UI
{
    public class ShopTestUI : MonoBehaviour
    {
        [Header("Top Bar")]
        public TMP_Text diamondText;
        public TMP_Text collectionText;
        public Button rerollButton;

        [Header("Pack Display")]
        public Transform packContainer;
        public GameObject packCardPrefab;

        [Header("Preview Panel")]
        public GameObject previewPanel;
        public TMP_Text previewTitle;
        public TMP_Text previewContents;
        public TMP_Text previewCost;
        public Button buyButton;
        public Button closePreviewButton;

        [Header("Navigation")]
        public Button backButton;
        public string mainMenuScene = "MainMenuScene";

        [Header("Log")]
        public TMP_Text logText;
        public ScrollRect logScrollRect;

        private ShopTestBootstrapper _boot;
        private PackData _selectedPack;
        private bool     _purchaseInFlight;
        private string _log = "";

        public void Initialise(ShopTestBootstrapper boot)
        {
            _boot = boot;

            buyButton.onClick.AddListener(OnBuy);
            closePreviewButton.onClick.AddListener(ClosePreview);
            rerollButton.onClick.AddListener(OnReroll);
            backButton?.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));

            previewPanel.SetActive(false);

            _boot.Wallet.OnCurrencyChanged += (_, __) => RefreshTopBar();
            _boot.Collection.OnCardAdded   += _       => RefreshTopBar();

            CreatePackCards();
            RefreshTopBar();
        }

        // ── Build pack display cards ─────────────────────────────────
        private void CreatePackCards()
        {
            // Clear existing
            foreach (Transform child in packContainer)
                Destroy(child.gameObject);

            foreach (var pack in _boot.packs)
            {
                var go = Instantiate(packCardPrefab, packContainer);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                var btn = go.GetComponent<Button>();

                // Expect prefab to have 3 TMP_Text children:
                // [0] = pack name, [1] = cost, [2] = card count
                if (texts.Length >= 3)
                {
                    texts[0].text = pack.packName;
                    texts[1].text = $"{pack.diamondCost} Diamonds";

                    var contents = _boot.PackSvc.GetDailyContents(pack);
                    texts[2].text = contents != null
                        ? $"{contents.Count} cards"
                        : "SOLD OUT";
                }

                var captured = pack;
                btn.onClick.AddListener(() => OnSelectPack(captured));

                // Grey out sold-out packs
                if (_boot.PackSvc.GetDailyContents(pack) == null)
                    btn.interactable = false;
            }
        }

        // ── Select pack to preview ───────────────────────────────────
        private void OnSelectPack(PackData pack)
        {
            _selectedPack = pack;

            var contents = _boot.PackSvc.GetDailyContents(pack);
            if (contents == null)
            {
                Log($"{pack.packName} is sold out for today");
                return;
            }

            previewPanel.SetActive(true);
            previewTitle.text = pack.packName;
            previewCost.text = $"Buy for {pack.diamondCost} Diamonds";

            // Build grouped display
            string display = "";
            var grouped = contents
                .GroupBy(c => c.rank)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                display += $"── {group.Key} rank ──\n";

                var cardCounts = group
                    .GroupBy(c => c.cardName)
                    .OrderBy(c => c.Key);

                foreach (var card in cardCounts)
                {
                    string status = _boot.Collection.OwnsCardOfType(card.First())
                        ? "(owned → +duplicate)"
                        : "(new → unlock)";
                    display += $"  {card.Key} x{card.Count()} {status}\n";
                }
            }

            display += $"\nTotal: {contents.Count} cards";
            previewContents.text = display;

            buyButton.interactable = _boot.PackSvc.CanAffordPack(pack);
        }

        // ── Buy ──────────────────────────────────────────────────────
        private void OnBuy()
        {
            if (_selectedPack == null || _purchaseInFlight) return;

            // Capture before any async work — _selectedPack may change while awaiting PlayFab
            var pack = _selectedPack;
            _purchaseInFlight      = true;
            buyButton.interactable = false;

            _boot.PackSvc.TryBuyPack(pack, results =>
            {
                _purchaseInFlight      = false;
                buyButton.interactable = true;

                if (results == null)
                {
                    Log("Purchase FAILED — not enough Diamonds");
                    return;
                }

                Log($"═══ Purchased {pack.packName} ═══");

                var resultGroups = results
                    .Where(c => c?.data != null)
                    .GroupBy(c => c.data.cardName)
                    .OrderBy(g => g.Key);

                foreach (var group in resultGroups)
                {
                    var card  = group.First();
                    int count = group.Count();
                    Log($"  {card.data.cardName} [{card.data.rank}] x{count} → dupes: {card.duplicateCount}");
                }

                Log($"Diamonds: {_boot.Wallet.Diamonds} | Collection: {_boot.Collection.CardCount} cards");

                ClosePreview();
                CreatePackCards();
                RefreshTopBar();
            });
        }

        // ── Reroll daily contents ────────────────────────────────────
        private void OnReroll()
        {
            _boot.PackSvc.GenerateAllDailyContents(_boot.packs);
            Log("Daily contents rerolled (premium feature)");
            CreatePackCards();
            ClosePreview();
        }

        // ── Close preview ────────────────────────────────────────────
        private void ClosePreview()
        {
            previewPanel.SetActive(false);
            _selectedPack = null;
        }

        // ── Refresh top bar ──────────────────────────────────────────
        private void RefreshTopBar()
        {
            diamondText.text = $"Diamonds: {_boot.Wallet.Diamonds}";
            collectionText.text = $"Collection: {_boot.Collection.CardCount} cards";
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