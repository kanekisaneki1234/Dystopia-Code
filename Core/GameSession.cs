using System.Collections.Generic;
using Dystopia.Cards;

namespace Dystopia.Core
{
    public static class GameSession
    {
        public static IReadOnlyList<CardInstance> ActiveDeck      { get; private set; }
        public static int                         ActiveDeckIndex { get; private set; }
        public static bool                        IsTestBattle    { get; set; }

        public static void SetActiveDeck(int index, List<CardInstance> cards)
        {
            ActiveDeckIndex = index;
            ActiveDeck      = cards.AsReadOnly();
        }

        public static void Clear() { ActiveDeck = null; }
    }
}