using System;
using System.Collections.Generic;
using Dystopia.Cards;

namespace Dystopia.Economy.Data
{
    [Serializable]
    public class CardCollection
    {
        public List<CardInstance> OwnedCards;

        public CardCollection()
        {
            OwnedCards = new List<CardInstance>();
        }
    }
}