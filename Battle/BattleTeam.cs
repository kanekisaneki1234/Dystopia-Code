using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dystopia.Core;
using Dystopia.Cards;
using Dystopia.Abilities;
using Dystopia.Events;
using Dystopia.Battle.Items;

namespace Dystopia.Battle
{
    public class BattleTeam : IBattleParticipant
    {
        // ── Identity ──────────────────────────────────────────────────────
        public string OwnerName { get; set; }

        // ── Cards ─────────────────────────────────────────────────────────
        public List<CardInstance> Cards = new List<CardInstance>();

        // ── Aggregate stats (IBattleParticipant) ──────────────────────────
        public int AggregateAttack  => Mathf.RoundToInt(Cards.Sum(c => c.Attack)  * AttackModifier);
        public int AggregateDefence => Mathf.RoundToInt(Cards.Sum(c => c.Defence) * DefenceModifier);
        public int AggregateSpeed   => Mathf.RoundToInt(Cards.Sum(c => c.Speed)   * SpeedModifier);

        // ── HP ────────────────────────────────────────────────────────────
        public int MaxHP            => Cards.Sum(c => c.HP);
        public int CurrentHP        { get; private set; }
        public bool IsDefeated      => CurrentHP <= 0;

        // ── Mana ──────────────────────────────────────────────────────────
        public int CurrentMana      { get; private set; }
        public int MaxMana          = 100;

        // ── Type advantage multiplier (set during Setup) ──────────────────
        public float TypeMultiplier = 1f;

        // ── Equipped item ─────────────────────────────────────────────────
        public BattleItem EquippedItem;

        // ── Item support ──────────────────────────────────────────────────
        public bool ForceFirstStrike { get; set; } = false;
        private float _damageReduction = 0f;

        // ── Damage over time ──────────────────────────────────────────────
        private int _dotDamagePerTurn = 0;
        private bool _dotIsTrueDamage   = false;
        public int CurrentDotDamage => _dotDamagePerTurn;

        // ── Shield ────────────────────────────────────────────────────────
        private int _shieldAmount      = 0;
        private int _shieldTurnsRemaining = 0;
        public int ShieldAmount => _shieldAmount;

        // ── Stat modifiers ────────────────────────────────────────────────
        public float AttackModifier  { get; private set; } = 1f;
        public float DefenceModifier { get; private set; } = 1f;
        public float SpeedModifier   { get; private set; } = 1f;

        private int _attackModTurns  = 0;
        private int _defenceModTurns = 0;
        private int _speedModTurns   = 0;

        // ── Ability round-robin ───────────────────────────────────────────
        private int _nextAbilitySlot = 0;

        /// <summary>
        /// Public read accessor for AbilitySelectionUI — shows which slot
        /// is "next up" in the round-robin so the UI can display the default marker.
        /// </summary>
        public int CurrentAbilitySlot => _nextAbilitySlot;

        // ── Ability override (set by IAbilitySelector via GameStateManager) ──
        private int? _abilityOverride = null;

        public void ApplyShield(int amount, int turns)
        {
            _shieldAmount         = Mathf.Max(amount, _shieldAmount);
            _shieldTurnsRemaining = Mathf.Max(turns, _shieldTurnsRemaining);
        }

        public void TickShield()
        {
            if (_shieldTurnsRemaining <= 0) return;
        }

        public void TakeTrueDamage(int amount)
        {
            int final_dmg = Mathf.Max(1, amount);
            CurrentHP = Mathf.Max(0, CurrentHP - final_dmg);
            BattleEvents.OnDamageDealt?.Invoke(OwnerName, final_dmg);
        }

        public void ApplyDot(int damagePerTurn, bool trueDamage = false)
        {
            bool alreadyActive = _dotDamagePerTurn > 0;

            _dotDamagePerTurn = damagePerTurn;
            _dotIsTrueDamage  = trueDamage;

            if (!alreadyActive) TickDot();
        }

        public void TickDot()
        {
            if (_dotDamagePerTurn <= 0) return;

            if (_dotIsTrueDamage)
                TakeTrueDamage(_dotDamagePerTurn);
            else
                TakeDamage(_dotDamagePerTurn);
        }

        public void ApplyStatMod(StatType stat, float multiplier, int turns)
        {
            switch (stat)
            {
                case StatType.Attack:
                    AttackModifier  = multiplier;
                    _attackModTurns = turns;
                    break;
                case StatType.Defence:
                    DefenceModifier  = multiplier;
                    _defenceModTurns = turns;
                    break;
                case StatType.Speed:
                    SpeedModifier   = multiplier;
                    _speedModTurns  = turns;
                    break;
            }
        }

        public void TickModifiers()
        {
            if (_attackModTurns > 0  && --_attackModTurns  == 0) AttackModifier  = 1f;
            if (_defenceModTurns > 0 && --_defenceModTurns == 0) DefenceModifier = 1f;
            if (_speedModTurns > 0   && --_speedModTurns   == 0) SpeedModifier   = 1f;
        }

        // ── Initialise at battle start ────────────────────────────────────
        public void Initialise()
        {
            CurrentHP   = MaxHP;
            CurrentMana = 0;
            ForceFirstStrike = false;
            _damageReduction = 0f;
            _dotDamagePerTurn  = 0;
            _dotIsTrueDamage   = false;
            _shieldAmount         = 0;
            _shieldTurnsRemaining = 0;
            _nextAbilitySlot = 0;
            _abilityOverride = null;

            if (EquippedItem != null) EquippedItem.ResetForBattle();
        }

        // ── Mana ──────────────────────────────────────────────────────────
        public void RegenMana(int currentTurn, int opponentSpeed)
        {
            int regenRate = 8;

            float hpMult = CurrentHP <= MaxHP
                ? 2f - ((float)CurrentHP / MaxHP)
                : 1f;

            float speedMult = Mathf.Clamp(
                (float)AggregateSpeed / Mathf.Max(1, opponentSpeed),
                1f, 1.2f);

            int decayBonus  = Mathf.FloorToInt((2f * regenRate) / currentTurn);
            int totalRegen  = Mathf.RoundToInt(regenRate * hpMult + decayBonus * speedMult);

            CurrentMana = Mathf.Min(CurrentMana + totalRegen, MaxMana);
            BattleEvents.OnManaChanged?.Invoke(OwnerName, CurrentMana);
        }

        public void SpendMana(int amount)
        {
            CurrentMana = Mathf.Max(0, CurrentMana - amount);
            BattleEvents.OnManaChanged?.Invoke(OwnerName, CurrentMana);
        }

        public void AddMana(int amount)
        {
            CurrentMana = Mathf.Min(CurrentMana + amount, MaxMana);
            BattleEvents.OnManaChanged?.Invoke(OwnerName, CurrentMana);
        }

        // ── Damage ────────────────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            int reduced   = Mathf.RoundToInt(amount * (1f - _damageReduction));
            int final_dmg = Mathf.Max(1, reduced);
            _damageReduction = 0f;

            if (_shieldAmount > 0)
            {
                int absorbed = Mathf.Min(final_dmg, _shieldAmount);
                _shieldAmount -= absorbed;
                final_dmg     -= absorbed;

                if (absorbed > 0) BattleEvents.OnShieldAbsorbed?.Invoke(OwnerName, absorbed);
            }

            if (final_dmg > 0)
            {
                CurrentHP = Mathf.Max(0, CurrentHP - final_dmg);
                BattleEvents.OnDamageDealt?.Invoke(OwnerName, final_dmg);
            }
        }

        // ── Healing ───────────────────────────────────────────────────────
        public void Heal(int amount)
        {
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
            BattleEvents.OnHealApplied?.Invoke(OwnerName, amount);
        }

        // ── Item support methods ──────────────────────────────────────────
        public void SetDamageReduction(float amount)
        {
            _damageReduction = Mathf.Clamp01(amount);
        }

        // ── Ability override (set by GameStateManager after AbilityWindow) ──

        /// <summary>
        /// Called by GameStateManager after the AbilityWindow closes,
        /// if the IAbilitySelector for this team chose a specific slot.
        /// </summary>
        public void SetAbilityOverride(int slotIndex)
        {
            _abilityOverride = slotIndex;
        }

        /// <summary>
        /// Clears any pending override. Called at the start of each round
        /// as a safety measure to prevent stale overrides carrying over.
        /// </summary>
        public void ClearAbilityOverride()
        {
            _abilityOverride = null;
        }

        /// <summary>
        /// Primary method for BattleSimulator.AttackPhase to get the ability
        /// card for this turn. Handles both manual override and round-robin.
        ///
        /// Override path:
        ///   - Uses the player-selected slot if it's valid and affordable.
        ///   - Advances the round-robin pointer to the slot AFTER the override
        ///     (Section 3.8 rule: pointer jumps past the manually-picked slot).
        ///   - Consumes the override so it doesn't persist to next turn.
        ///
        /// Round-robin path (no override, or override was invalid):
        ///   - Falls through to GetNextAutoAbilityCard() which enforces the
        ///     strict queue rule (can't afford → no ability fires at all).
        ///
        /// In both cases, AdvanceAbilitySlot() is still called separately
        /// by AttackPhase after the ability fires. For the override path,
        /// the pointer is already set correctly so AdvanceAbilitySlot()
        /// will confirm the same position (idempotent for the override case).
        /// </summary>
        public CardInstance GetAbilityCardForTurn()
        {
            if (_abilityOverride.HasValue)
            {
                int slot = _abilityOverride.Value;
                _abilityOverride = null;    // consume regardless of validity

                var activeCards = GetActiveAbilityCards();

                if (slot >= 0 && slot < activeCards.Count && activeCards[slot] != null)
                {
                    var card    = activeCards[slot];
                    var ability = card.data.ability;

                    // Verify mana at resolution time (player could afford at selection
                    // time, but a passive/tick might have changed mana since then)
                    int cost = Mathf.RoundToInt(ability.manaCost * card.ManaCostMultiplier);
                    if (CurrentMana >= cost)
                    {
                        // Advance pointer to the slot AFTER the override
                        _nextAbilitySlot = (slot + 1) % activeCards.Count;

                        Debug.Log($"[BattleTeam] Manual override: slot {slot} " +
                                  $"({card.data.cardName} — {ability.abilityName})");
                        return card;
                    }

                    Debug.LogWarning($"[BattleTeam] Override slot {slot} " +
                                     $"({card.data.cardName}) can no longer afford " +
                                     $"ability (need {cost}, have {CurrentMana}). " +
                                     $"Falling back to round-robin.");
                }
                else
                {
                    Debug.LogWarning($"[BattleTeam] Override slot {slot} invalid " +
                                     $"(activeCards count: {GetActiveAbilityCards().Count}). " +
                                     $"Falling back to round-robin.");
                }
            }

            // No override or override failed — use standard round-robin
            return GetNextAutoAbilityCard();
        }

        // ── Ability selection (round-robin) ───────────────────────────────

        /// <summary>
        /// Round-robin auto-selection with the strict queue rule:
        /// if the next ability in queue can't afford its mana cost,
        /// NO ability fires — even if cheaper abilities exist later.
        /// </summary>
        public CardInstance GetNextAutoAbilityCard()
        {
            var activeCards = GetActiveAbilityCards();
            if (activeCards.Count == 0) return null;

            _nextAbilitySlot = _nextAbilitySlot % activeCards.Count;
            var card = activeCards[_nextAbilitySlot];

            int cost = Mathf.RoundToInt(
                card.data.ability.manaCost * card.ManaCostMultiplier);

            if (CurrentMana < cost) return null;

            return card;
        }

        public void AdvanceAbilitySlot(CardInstance usedCard)
        {
            var activeCards = GetActiveAbilityCards();
            if (activeCards.Count == 0) return;

            int usedIndex = activeCards.IndexOf(usedCard);
            if (usedIndex >= 0)
                _nextAbilitySlot = (usedIndex + 1) % activeCards.Count;
        }

        public List<CardInstance> GetActiveAbilityCards()
        {
            var result = new List<CardInstance>();
            foreach (var card in Cards)
            {
                if (card.data.ability != null && card.data.ability.kind == AbilityKind.Active)
                    result.Add(card);
            }
            return result;
        }

        // ── Passive ability check ─────────────────────────────────────────
        public List<CardInstance> GetPassiveCards()
        {
            return Cards.Where(c =>
                c.data.ability != null &&
                c.data.ability.kind == AbilityKind.Passive).ToList();
        }
    }
}