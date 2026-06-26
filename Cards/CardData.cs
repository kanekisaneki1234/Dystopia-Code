using UnityEngine;
using Dystopia.Core;
using Dystopia.Abilities;

namespace Dystopia.Cards
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Dystopia/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string    cardId;      // Stable unique key — matches PlayFab catalog ItemId. Never change after creation.
        public string    cardName;
        public CardRank  rank;
        public CardClass cardClass;
        public CardType  cardType;
        public Sprite    artwork;

        [Header("Base Stats (Tier I, Level 1, no Resonance)")]
        public int baseHP;
        public int baseAttack;
        public int baseDefence;
        public int baseSpeed;

        [Header("Ability")]
        public AbilityData ability;

        public int MaxLevelForTier(int tier) => tier switch
        {
            1 => 30,
            2 => 40,
            3 => 50,
            _ => 30
        };

        private static readonly float[] TierCostMult = { 1.0f, 1.25f, 1.5f };

        public int FragmentCostForLevel(int tier, int currentLevel)
        {
            return (int)System.Math.Round(
                2f * currentLevel * TierCostMult[tier - 1],
                System.MidpointRounding.AwayFromZero);
        }

        public int GoldCostForLevel(int tier, int currentLevel)
        {
            return 5 * FragmentCostForLevel(tier, currentLevel);
        }
    }
}