using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dystopia.Core;
using Dystopia.Events;

namespace Dystopia.UI
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("Player")]
        public Slider   playerHPBar;
        public Slider   playerManaBar;
        public TMP_Text playerHPText;

        [Header("Opponent")]
        public Slider   opponentHPBar;
        public Slider   opponentManaBar;
        public TMP_Text opponentHPText;

        [Header("Battle Info")]
        public TMP_Text turnText;
        public TMP_Text battleLog;

        private int _playerMaxHP;
        private int _opponentMaxHP;

        private void OnEnable()
        {
            BattleEvents.OnStateChanged   += HandleStateChanged;
            BattleEvents.OnTurnStart      += HandleTurnStart;
            BattleEvents.OnDamageDealt    += HandleDamage;
            BattleEvents.OnHealApplied    += HandleHeal;
            BattleEvents.OnAbilityFired   += HandleAbility;
            BattleEvents.OnManaChanged    += HandleMana;
            BattleEvents.OnBattleEnd      += HandleBattleEnd;
            BattleEvents.OnShieldAbsorbed += HandleShieldAbsorbed;
        }

        private void OnDisable()
        {
            BattleEvents.OnStateChanged   -= HandleStateChanged;
            BattleEvents.OnTurnStart      -= HandleTurnStart;
            BattleEvents.OnDamageDealt    -= HandleDamage;
            BattleEvents.OnHealApplied    -= HandleHeal;
            BattleEvents.OnAbilityFired   -= HandleAbility;
            BattleEvents.OnManaChanged    -= HandleMana;
            BattleEvents.OnBattleEnd      -= HandleBattleEnd;
            BattleEvents.OnShieldAbsorbed -= HandleShieldAbsorbed;
        }

        private void HandleStateChanged(BattleState state)
        {
            Log($"[State] {state}");
        }

        private void HandleTurnStart(int turn)
        {
            turnText.text = $"Turn {turn}";
            Log($"── Turn {turn} ──");
        }

        private void HandleDamage(string teamName, int amount)
        {
            Log($"{teamName} took {amount} damage");
            RefreshBars();
        }

        private void HandleHeal(string teamName, int amount)
        {
            Log($"{teamName} healed {amount} HP");
            RefreshBars();
        }

        private void HandleAbility(string abilityName, int value)
        {
            Log($"Ability: {abilityName} ({value})");
        }

        private void HandleShieldAbsorbed(string teamName, int amount)
        {
            Log($"{teamName} shield absorbed {amount} damage");
        }

        private void HandleMana(string teamName, int newMana)
        {
        }

        private void HandleBattleEnd(string result)
        {
            Log($"=== {result} ===");
        }

        private void RefreshBars()
        {
        }

        public void SetTeamReferences(
            int playerMaxHP,   int opponentMaxHP,
            System.Func<int>   getPlayerHP,
            System.Func<int>   getPlayerMana,
            System.Func<int>   getOpponentHP,
            System.Func<int>   getOpponentMana,
            int maxMana)
        {
            _playerMaxHP   = playerMaxHP;
            _opponentMaxHP = opponentMaxHP;

            playerHPBar.maxValue     = playerMaxHP;
            opponentHPBar.maxValue   = opponentMaxHP;
            playerManaBar.maxValue   = maxMana;
            opponentManaBar.maxValue = maxMana;

            BattleEvents.OnDamageDealt += (_, __) =>
            {
                playerHPBar.value   = getPlayerHP();
                opponentHPBar.value = getOpponentHP();
                playerHPText.text   = $"{getPlayerHP()} / {playerMaxHP}";
                opponentHPText.text = $"{getOpponentHP()} / {opponentMaxHP}";
            };

            BattleEvents.OnManaChanged += (_, __) =>
            {
                playerManaBar.value   = getPlayerMana();
                opponentManaBar.value = getOpponentMana();
            };
        }

        private void Log(string message)
        {
            Debug.Log($"[Battle] {message}");
            battleLog.text += message + "\n";
        }
    }
}