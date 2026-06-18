using System;
using System.Collections.Generic;
using System.Linq;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Data;

namespace Dystopia.Economy.Services
{
    public class CollectionService
    {
        private readonly CardCollection _collection;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<CardInstance> OnCardAdded;
        public event Action<CardInstance> OnCardRemoved;

        // ── Constructor ───────────────────────────────────────────────────
        public CollectionService(CardCollection collection)
        {
            _collection = collection;
        }

        // ── Read-only accessors ───────────────────────────────────────────
        public IReadOnlyList<CardInstance> OwnedCards => _collection.OwnedCards;

        public int CardCount => _collection.OwnedCards.Count;

        // ── Add ───────────────────────────────────────────────────────────
        public CardInstance AddCard(CardData cardData)
        {
            var instance = new CardInstance(cardData);

            if (cardData.rank == CardRank.S) instance.SetResonance(ResonanceLevel.R1);

            _collection.OwnedCards.Add(instance);
            OnCardAdded?.Invoke(instance);
            return instance;
        }

        // ── Remove (market sales, disenchanting, etc.) ────────────────────
        public bool RemoveCard(CardInstance card)
        {
            if (!_collection.OwnedCards.Contains(card)) return false;

            _collection.OwnedCards.Remove(card);
            OnCardRemoved?.Invoke(card);
            return true;
        }

        // ── Queries ───────────────────────────────────────────────────────
        public List<CardInstance> GetCardsByClass(CardClass cardClass)
        {
            return _collection.OwnedCards
                .Where(c => c.data.cardClass == cardClass)
                .ToList();
        }

        public List<CardInstance> GetCardsByType(CardType cardType)
        {
            return _collection.OwnedCards
                .Where(c => c.data.cardType == cardType)
                .ToList();
        }

        public List<CardInstance> GetCardsByName(string cardName)
        {
            return _collection.OwnedCards
                .Where(c => c.data.cardName == cardName)
                .ToList();
        }

        // ── Duplicate counting (for Resonance) ───────────────────────────
        public int CountDuplicates(CardData cardData)
        {
            return _collection.OwnedCards
                .Count(c => c.data == cardData);
        }

        // ── Ownership check ───────────────────────────────────────────────
        public bool OwnsCard(CardInstance card)
        {
            return _collection.OwnedCards.Contains(card);
        }

        public bool OwnsCardOfType(CardData cardData)
        {
            return _collection.OwnedCards.Any(c => c.data == cardData);
        }

        public CardInstance GetOwnedCard(CardData cardData)
        {
            return _collection.OwnedCards.FirstOrDefault(c => c.data == cardData);
        }
    }
}