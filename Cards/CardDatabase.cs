using System.Collections.Generic;
using UnityEngine;

namespace Dystopia.Cards
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Dystopia/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        public CardData[] cards;

        private Dictionary<string, CardData> _lookup;

        public void Init()
        {
            _lookup = new Dictionary<string, CardData>(cards.Length);
            foreach (var c in cards)
                if (!string.IsNullOrEmpty(c.cardId))
                    _lookup[c.cardId] = c;
        }

        public CardData GetByCardId(string cardId)
        {
            if (_lookup == null) Init();
            return _lookup.TryGetValue(cardId, out var data) ? data : null;
        }
    }
}
