using System;
using Dystopia.Battle;
using Dystopia.Core;

namespace Dystopia.Events
{
    public static class BattleEvents
    {
        // ── Battle lifecycle ──────────────────────────────────────────────
        public static Action<BattleState>            OnStateChanged;
        public static Action<int>                    OnTurnStart;
        public static Action<string>                 OnBattleEnd;
        public static Action<BattleResult>           OnBattleEnded;  // rich payload for result panel

        // ── Combat ────────────────────────────────────────────────────────
        public static Action<string, int>            OnDamageDealt;
        public static Action<string, int>            OnHealApplied;
        public static Action<string, int>            OnAbilityFired;
        public static Action<string, int>            OnManaChanged;
        public static Action<string, int>            OnShieldAbsorbed; // teamName, amount absorbed

        // ── Progression ───────────────────────────────────────────────────
        public static Action<string, TierLevel>      OnTierUnlocked;
        public static Action<string, ResonanceLevel> OnResonanceUp;

        // ── Economy ───────────────────────────────────────────────────────
        // public static Action<int>                    OnGoldChanged;
        // public static Action<int>                    OnDiamondsChanged;
        // public static Action<int>                    OnFragmentsChanged;

        // ── Cleanup ───────────────────────────────────────────────────────
        // Call this when a battle scene unloads to prevent stale listeners
        // from a previous scene firing into a new one.
        public static void ClearAllListeners()
        {
            OnStateChanged     = null;
            OnTurnStart        = null;
            OnBattleEnd        = null;
            OnBattleEnded      = null;
            OnDamageDealt      = null;
            OnHealApplied      = null;
            OnAbilityFired     = null;
            OnManaChanged      = null;
            OnTierUnlocked     = null;
            OnResonanceUp      = null;
            OnShieldAbsorbed   = null;
            // OnGoldChanged      = null;
            // OnDiamondsChanged  = null;
            // OnFragmentsChanged = null;
        }
    }
}