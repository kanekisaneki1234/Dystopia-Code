using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Cards;
using Dystopia.Economy.Data;

namespace Dystopia.UI
{
    public class MarketMyListingUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private Image    artworkImage;
        [SerializeField] private TMP_Text typeLabel;
        [SerializeField] private TMP_Text levelTierText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private Button   delistButton;

        private MarketListing _listing;

        public void Configure(MarketListing listing, CardData cardData, Action<MarketListing> onDelist)
        {
            _listing = listing;

            if (cardNameText)  cardNameText.text  = cardData != null ? cardData.cardName : listing.cardId;
            if (artworkImage && cardData?.artwork) artworkImage.sprite = cardData.artwork;
            if (typeLabel)     typeLabel.text      = listing.listingType == "original" ? "Original" : "Duplicate";
            if (levelTierText && listing.cardData != null)
                levelTierText.text = $"Lv.{listing.cardData.level}  T{listing.cardData.tier}  R{listing.cardData.resonance}";
            if (priceText)     priceText.text      = $"{listing.price} GD";

            if (cooldownText)
            {
                var cooldownEnd = System.DateTime.Parse(listing.cooldownEndsAt, null,
                    System.Globalization.DateTimeStyles.RoundtripKind);
                var remaining = cooldownEnd - System.DateTime.UtcNow;
                cooldownText.text = remaining.TotalSeconds > 0
                    ? (remaining.TotalHours >= 1
                        ? $"Cooldown: {(int)remaining.TotalHours}h {remaining.Minutes}m"
                        : $"Cooldown: {remaining.Minutes}m {remaining.Seconds}s")
                    : "Listed";
            }

            delistButton?.onClick.RemoveAllListeners();
            delistButton?.onClick.AddListener(() => onDelist?.Invoke(_listing));
        }
    }
}
