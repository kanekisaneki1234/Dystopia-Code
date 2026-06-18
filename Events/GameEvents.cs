using System;

namespace Dystopia.Events
{
    public static class GameEvents
    {
        // Economy — persist across all scenes forever
        public static Action<int> OnGoldChanged;
        public static Action<int> OnDiamondsChanged;
        public static Action<int> OnFragmentsChanged;

        // Navigation
        public static Action<string> OnSceneChangeRequested;

        // Player state
        public static Action OnInventoryUpdated;
        public static Action OnPlayerDataLoaded;
    }
}