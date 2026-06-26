using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Cards;
using Dystopia.Core;

namespace Dystopia.UI
{
    public class CardSlotUI : MonoBehaviour
    {
        [Header("Display")]
        public Image    artwork;
        public TMP_Text cardNameText;
        public TMP_Text powerLevelText;
        public TMP_Text tierText;
        public TMP_Text resonanceText;


        public void Configure(CardInstance card)
        {
            if (artwork)
            {
                artwork.sprite  = card.data.artwork;
                artwork.enabled = card.data.artwork != null;
            }

            if (cardNameText)
                cardNameText.text = card.data.cardName;

            if (powerLevelText)
                powerLevelText.text = $"Lv.{card.CurrentLevel}";

            if (tierText)
                tierText.text = $"Tier {(TierLevel)card.CurrentTier}";

            if (resonanceText)
                resonanceText.text = ResonanceStars(card.resonanceLevel);
        }

        private static string ResonanceStars(ResonanceLevel level)
        {
            int filled = (int)level;          // None=0, R1=1 … R4=4
            int empty  = 4 - filled;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < filled; i++) sb.Append("<sprite=0>");
            for (int i = 0; i < empty;  i++) sb.Append("<sprite=1>");
            return sb.ToString();
        }
    }
}
