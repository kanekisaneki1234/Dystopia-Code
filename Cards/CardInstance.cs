using System;
using UnityEngine;
using Dystopia.Core;

namespace Dystopia.Cards
{
    [Serializable]
    public class CardInstance : IProgressable
    {
        // ── Immutable reference ───────────────────────────────────────────
        public CardData data;

        // ── Progression state ─────────────────────────────────────────────
        public int            CurrentLevel   { get; private set; } = 1;
        public int            CurrentTier    { get; private set; } = 1;
        public ResonanceLevel resonanceLevel = ResonanceLevel.None;
        public int            duplicateCount = 0;
        public string         PlayFabItemInstanceId;
        public string         MarketCooldownUntil { get; set; }

        // ── IProgressable ─────────────────────────────────────────────────
        public TierLevel      Tier      => (TierLevel)CurrentTier;
        public ResonanceLevel Resonance => resonanceLevel;

        // ── Computed stats (read-only, set by Recalculate()) ──────────────
        public int   HP                  { get; private set; }
        public int   Attack              { get; private set; }
        public int   Defence             { get; private set; }
        public int   Speed               { get; private set; }
        public float ManaCostMultiplier  { get; private set; } = 1f;

        // ── Constructor ───────────────────────────────────────────────────
        public CardInstance(CardData cardData)
        {
            data = cardData;
            Recalculate();
        }

        // ── Recalculate computed stats ────────────────────────────────────
        // Call this after ANY progression change:
        // level up, tier unlock, or resonance increase.
        public void Recalculate()
        {
            if (data == null) return;

            // Tier multiplier — each Tier adds 25% to base stats
            float tierMult  = 1f + (CurrentTier  - 1) * 0.25f;

            // Level multiplier — each level adds 2% to base stats
            float levelMult = 1f + (CurrentLevel - 1) * 0.02f;

            // Resonance stat bonus — R1 adds 5%, R4 adds another 5%
            float resonanceMult = 1f;
            if (resonanceLevel >= ResonanceLevel.R1) resonanceMult += 0.05f;
            if (resonanceLevel >= ResonanceLevel.R4) resonanceMult += 0.05f;

            // Apply all multipliers to base stats
            HP      = Mathf.RoundToInt(data.baseHP      * tierMult * levelMult * resonanceMult);
            Attack  = Mathf.RoundToInt(data.baseAttack  * tierMult * levelMult * resonanceMult);
            Defence = Mathf.RoundToInt(data.baseDefence * tierMult * levelMult * resonanceMult);
            Speed   = Mathf.RoundToInt(data.baseSpeed   * tierMult * levelMult * resonanceMult);

            // Resonance 3 reduces mana cost by 20%
            ManaCostMultiplier = resonanceLevel >= ResonanceLevel.R3 ? 0.85f : 1f;
        }

        // -------Level Up----------
        public bool LevelUp()
        {
            int levelCap = data.MaxLevelForTier(CurrentTier);

            if (CurrentLevel >= levelCap)
            {
                Debug.LogWarning(
                    $"[CardInstance] {data.cardName} is at max level ({levelCap}) " +
                    $"for Tier {CurrentTier}. Upgrade tier first.");
                return false;
            }

            CurrentLevel++;
            Recalculate();
            return true;
        }

        public bool LevelUpBy(int levels)
        {
            if (levels <= 0) return false;

            int levelCap        = data.MaxLevelForTier(CurrentTier);
            int levelsAvailable = levelCap - CurrentLevel;
            int levelsToApply   = Mathf.Min(levels, levelsAvailable);

            if (levelsToApply <= 0)
            {
                Debug.LogWarning(
                    $"[CardInstance] {data.cardName} is already at max level " +
                    $"({levelCap}) for Tier {CurrentTier}.");
                return false;
            }

            CurrentLevel += levelsToApply;
            Recalculate();
            return true;
        }

        public bool LevelUpToMax()
        {
            int levelCap = data.MaxLevelForTier(CurrentTier);
            return LevelUpBy(levelCap - CurrentLevel);
        }

        public int FragmentCostForLevels(int targetLevel)
        {
            int cap   = data.MaxLevelForTier(CurrentTier);
            int to    = Mathf.Min(targetLevel, cap);
            int total = 0;

            for (int lvl = CurrentLevel; lvl < to; lvl++)
                total += data.FragmentCostForLevel(CurrentTier, lvl);

            return total;
        }

        public int GoldCostForLevels(int targetLevel)
        {
            int cap   = data.MaxLevelForTier(CurrentTier);
            int to    = Mathf.Min(targetLevel, cap);
            int total = 0;

            for (int lvl = CurrentLevel; lvl < to; lvl++)
                total += data.GoldCostForLevel(CurrentTier, lvl);

            return total;
        }

        // -------Tier Upgrade----------
        public bool TierUpgrade()
        {
            if (CurrentTier >= 3)
            {
                Debug.LogWarning(
                    $"[CardInstance] {data.cardName} is already at max tier.");
                return false;
            }

            int requiredLevel = data.MaxLevelForTier(CurrentTier);
            if (CurrentLevel < requiredLevel)
            {
                Debug.LogWarning(
                    $"[CardInstance] {data.cardName} must reach Level {requiredLevel} " +
                    $"before upgrading to Tier {CurrentTier + 1}. " +
                    $"Current level: {CurrentLevel}.");
                return false;
            }

            CurrentTier++;
            Recalculate();
            return true;
        }

        public bool SetResonance(ResonanceLevel level)
        {
            if (level == resonanceLevel) return false;

            resonanceLevel = level;
            Recalculate();

            // R2 triggers the ability boost
            if (resonanceLevel >= ResonanceLevel.R2 && data.ability != null)
                data.ability.ApplyResonanceBoost();

            return true;
        }

        public void AddDuplicate()
        {
            duplicateCount++;
        }

        public void LevelDown()
        {
            if (CurrentLevel > 1) { CurrentLevel--; Recalculate(); }
        }

        public void TierDown()
        {
            if (CurrentTier > 1) { CurrentTier--; Recalculate(); }
        }

        public void UndoResonance(ResonanceLevel previous, int dupesRestored)
        {
            resonanceLevel  = previous;
            duplicateCount += dupesRestored;
            Recalculate();
        }

        // ── Restore persisted state (called by CollectionService on login) ─
        public void RestoreState(int level, int tier, ResonanceLevel resonance, int duplicates, string itemInstanceId)
        {
            CurrentLevel          = level;
            CurrentTier           = tier;
            resonanceLevel        = resonance;
            duplicateCount        = duplicates;
            PlayFabItemInstanceId = itemInstanceId;

            if (resonanceLevel >= ResonanceLevel.R2 && data.ability != null)
                data.ability.ApplyResonanceBoost();

            Recalculate();
        }
    }
}