using System;
using System.Collections;
using UnityEngine;
using Dystopia.Core;
using Dystopia.Cards;
using Dystopia.Events;

namespace Dystopia.Battle
{
    public class GameStateManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static GameStateManager Instance { get; private set; }

        // ── Config ────────────────────────────────────────────────────────
        [Header("Battle Config")]
        public float abilityWindowSeconds = 1f;
        public int   maxTurns             = 20;

        // ── Runtime state ─────────────────────────────────────────────────
        private BattleState  _state;
        private BattleTeam   _playerTeam;
        private BattleTeam   _opponentTeam;
        private int          _currentTurn;

        // Set by UI during AbilityWindow — null means use default slot order
        private CardInstance _playerAbilityOverride;

        // ── Ability selectors (player UI + opponent AI) ───────────────────
        private IAbilitySelector _playerSelector;
        private IAbilitySelector _opponentSelector;
        private bool             _bothReady;

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            // Singleton pattern — only one GameStateManager can exist at a time
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            // Clear battle-scoped listeners when this scene unloads
            BattleEvents.ClearAllListeners();
            Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────
        public void StartBattle(BattleTeam player, BattleTeam opponent)
        {
            _playerTeam   = player;
            _opponentTeam = opponent;
            _currentTurn  = 0;

            _playerTeam.Initialise();
            _opponentTeam.Initialise();

            TypeResolver.ApplyMultipliers(_playerTeam, _opponentTeam);

            TransitionTo(BattleState.Setup);
        }

        // Called by UI when player selects an ability during the window
        public void SetAbilityOverride(CardInstance card)
        {
            _playerAbilityOverride = card;
        }

        public void SetSelectors(IAbilitySelector player, IAbilitySelector opponent)
        {
            _playerSelector   = player;
            _opponentSelector = opponent;
        }

        // ── State machine ─────────────────────────────────────────────────
        private void TransitionTo(BattleState next)
        {
            _state = next;
            BattleEvents.OnStateChanged?.Invoke(_state);

            switch (_state)
            {
                case BattleState.Setup:
                    TransitionTo(BattleState.RoundStart);
                    break;

                case BattleState.RoundStart:
                    StartCoroutine(DoRoundStart());
                    break;

                case BattleState.AbilityWindow:
                    StartCoroutine(DoAbilityWindow());
                    break;

                case BattleState.Resolution:
                    StartCoroutine(DoResolution());
                    break;

                case BattleState.RoundEnd:
                    DoRoundEnd();
                    break;

                case BattleState.Victory:
                case BattleState.Defeat:
                case BattleState.Draw:
                    BattleEvents.OnBattleEnd?.Invoke(_state.ToString());
                    BattleEvents.OnBattleEnded?.Invoke(BuildBattleResult());
                    break;
            }
        }

        // ── Coroutines ────────────────────────────────────────────────────
        private IEnumerator DoRoundStart()
        {
            _currentTurn++;
            _playerAbilityOverride = null;

            BattleEvents.OnTurnStart?.Invoke(_currentTurn);
            
            // Stat mod ability
            _playerTeam.TickModifiers();
            _opponentTeam.TickModifiers();
            // DoT ability
            _playerTeam.TickDot();
            _opponentTeam.TickDot();
            // Shield ability
            _playerTeam.TickShield();
            _opponentTeam.TickShield();

            if (_playerTeam.IsDefeated || _opponentTeam.IsDefeated)
            {
                DoRoundEnd();
                yield break;
            }

            // Regenerate mana for both teams
            _playerTeam.RegenMana(_currentTurn, _opponentTeam.AggregateSpeed);
            _opponentTeam.RegenMana(_currentTurn, _playerTeam.AggregateSpeed);

            Debug.Log($"[Turn {_currentTurn}] Mana → Player: {_playerTeam.CurrentMana} | Opponent: {_opponentTeam.CurrentMana}");

            // Trigger all passive abilities
            BattleSimulator.TriggerPassives(_playerTeam, _opponentTeam);

            yield return new WaitForSeconds(0.5f);
            TransitionTo(BattleState.AbilityWindow);
        }

        private IEnumerator DoAbilityWindow()
        {
            if (_playerSelector != null && _opponentSelector != null)
                yield return StartCoroutine(RunAbilityWindow());
            else
                yield return new WaitForSeconds(abilityWindowSeconds);

            TransitionTo(BattleState.Resolution);
        }

        private IEnumerator RunAbilityWindow()
        {
            _playerTeam.ClearAbilityOverride();
            _opponentTeam.ClearAbilityOverride();

            _bothReady = false;
            Action onReady = () =>
            {
                if (_playerSelector.IsReady && _opponentSelector.IsReady)
                    _bothReady = true;
            };

            _playerSelector.OnReady   += onReady;
            _opponentSelector.OnReady += onReady;

            _playerSelector.BeginSelection(_playerTeam, abilityWindowSeconds);
            _opponentSelector.BeginSelection(_opponentTeam, abilityWindowSeconds);

            float elapsed = 0f;
            while (!_bothReady && elapsed < abilityWindowSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _playerSelector.OnReady   -= onReady;
            _opponentSelector.OnReady -= onReady;

            _playerSelector.ForceEnd();
            _opponentSelector.ForceEnd();

            if (_playerSelector.SelectedSlot.HasValue)
                _playerTeam.SetAbilityOverride(_playerSelector.SelectedSlot.Value);

            if (_opponentSelector.SelectedSlot.HasValue)
                _opponentTeam.SetAbilityOverride(_opponentSelector.SelectedSlot.Value);
        }

        private IEnumerator DoResolution()
        {
            BattleSimulator.ResolveRound(
                _playerTeam,
                _opponentTeam,
                _playerAbilityOverride);

            // Pause briefly so animations have time to play
            yield return new WaitForSeconds(1f);
            TransitionTo(BattleState.RoundEnd);
        }

        // ── Battle result builder ─────────────────────────────────────────
        private BattleResult BuildBattleResult()
        {
            int pStats = _playerTeam.AggregateAttack + _playerTeam.AggregateDefence + _playerTeam.AggregateSpeed;
            int oStats = _opponentTeam.AggregateAttack + _opponentTeam.AggregateDefence + _opponentTeam.AggregateSpeed;
            float mult = Mathf.Clamp((float)oStats / Mathf.Max(1, pStats), 0.5f, 2.0f);

            int gold, frags;
            if (_state == BattleState.Victory)
            {
                gold  = Mathf.FloorToInt(100 * mult) + (_currentTurn * 5);
                frags = Mathf.FloorToInt(20  * mult) + Mathf.FloorToInt(_currentTurn * 1.5f);
            }
            else if (_state == BattleState.Draw)
            {
                gold  = Mathf.FloorToInt(50 * mult) + (_currentTurn * 4);
                frags = Mathf.FloorToInt(10 * mult) + _currentTurn;
            }
            else  // Defeat
            {
                gold  = Mathf.FloorToInt(25 * mult) + (_currentTurn * 3);
                frags = Mathf.FloorToInt(5  * mult) + _currentTurn;
            }

            return new BattleResult
            {
                Outcome              = _state.ToString(),
                PlayerTeam           = _playerTeam,
                OpponentTeam         = _opponentTeam,
                RoundsPlayed         = _currentTurn,
                PlayerStatTotal      = pStats,
                OpponentStatTotal    = oStats,
                DifficultyMultiplier = mult,
                GoldReward           = gold,
                FragmentReward       = frags
            };
        }

        // ── Round end logic ───────────────────────────────────────────────
        private void DoRoundEnd()
        {
            // Check for knockout
            if (_playerTeam.IsDefeated && _opponentTeam.IsDefeated)
            {
                TransitionTo(BattleState.Draw);
                return;
            }
            if (_playerTeam.IsDefeated)
            {
                TransitionTo(BattleState.Defeat);
                return;
            }
            if (_opponentTeam.IsDefeated)
            {
                TransitionTo(BattleState.Victory);
                return;
            }

            // Check turn limit
            if (_currentTurn >= maxTurns)
            {
                if (_playerTeam.CurrentHP > _opponentTeam.CurrentHP)
                    TransitionTo(BattleState.Victory);
                else if (_playerTeam.CurrentHP < _opponentTeam.CurrentHP)
                    TransitionTo(BattleState.Defeat);
                else
                    TransitionTo(BattleState.Draw);
                return;
            }

            // No winner yet — next round
            TransitionTo(BattleState.RoundStart);
        }
    }
}