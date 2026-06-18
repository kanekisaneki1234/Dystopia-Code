using System;
using UnityEngine;
using Dystopia.Core;

namespace Dystopia.Economy.Data
{
    [Serializable]
    public class PackRankEntry
    {
        public CardRank rank;
        public int minCards;
        public int maxCards;
        [Range(0f, 1f)]
        public float appearanceChance = 1f;
    }

    [CreateAssetMenu(menuName = "Dystopia/Economy/Pack")]
    public class PackData : ScriptableObject
    {
        public string packName;
        public int    diamondCost;
        public PackRankEntry[] rankEntries;
    }
}