using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Cards;
using Dystopia.Economy.Data;

namespace Dystopia.UI
{
    public class MarketListingUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text   cardNameText;
        [SerializeField] private Image      artworkImage;
        [SerializeField] private TMP_Text   typeLabel;
        [SerializeField] private TMP_Text   levelTierText;
        [SerializeField] private TMP_Text   priceText;
        [SerializeField] private TMP_Text   sellerText;
        [SerializeField] private GameObject cooldownOverlay;
        [SerializeField] private TMP_Text   cooldownText;
        [SerializeField] private Button     selectButton;

        private MarketListing _listing;

        public void Configure(MarketListing listing, CardData cardData, Action<MarketListing> onSelected)
        {
            _listing = listing;

            if (cardNameText)  cardNameText.text   = cardData != null ? cardData.cardName : listing.cardId;
            if (artworkImage && cardData?.artwork)  artworkImage.sprite = cardData.artwork;
            if (typeLabel)     typeLabel.text       = listing.listingType == "original" ? "Original" : "Duplicate";
            if (levelTierText && listing.cardData != null)
                levelTierText.text = $"Lv.{listing.cardData.level}  T{listing.cardData.tier}  R{listing.cardData.resonance}";
            if (priceText)     priceText.text       = $"{listing.price} GD";
            if (sellerText)    sellerText.text       = $"by {listing.sellerName}";

            bool onCooldown = !listing.buyable;
            if (cooldownOverlay) cooldownOverlay.SetActive(onCooldown);
            if (cooldownText && onCooldown)
            {
                var remaining = System.DateTime.Parse(listing.cooldownEndsAt, null,
                    System.Globalization.DateTimeStyles.RoundtripKind) - System.DateTime.UtcNow;
                if (remaining.TotalSeconds > 0)
                    cooldownText.text = remaining.TotalHours >= 1
                        ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                        : $"{remaining.Minutes}m {remaining.Seconds}s";
                else
                    cooldownText.text = "Soon";
            }

            selectButton?.onClick.RemoveAllListeners();
            if (!onCooldown)
                selectButton?.onClick.AddListener(() => onSelected?.Invoke(_listing));
        }
    }
}
