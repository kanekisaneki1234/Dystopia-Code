using System.Collections.Generic;
using UnityEngine;
using Dystopia.Battle;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.UI;

namespace Dystopia.UI
{
    public class BattleTestBootstrapper : MonoBehaviour
    {
        [Header("Player Cards (3)")]
        public CardData playerCard1;
        public CardData playerCard2;
        public CardData playerCard3;

        [Header("Opponent Cards (3)")]
        public CardData opponentCard1;
        public CardData opponentCard2;
        public CardData opponentCard3;

        [Header("UI")]
        public BattleUIController  ui;
        public AbilitySelectionUI  abilitySelectionUI;

        // ── Accessible by ability selection UI ────────────────────────
        public BattleTeam PlayerTeam  { get; private set; }
        public BattleTeam OpponentTeam { get; private set; }

        private void MaxOutCard(CardInstance card)
        {
            card.LevelUpToMax();
            card.TierUpgrade();
            card.LevelUpToMax();
            card.TierUpgrade();
            card.LevelUpToMax();

            for (int i = 0; i < 10; i++)
                card.AddDuplicate();
            card.SetResonance(ResonanceLevel.R4);
        }

        private void Start()
        {
            PlayerTeam = new BattleTeam
            {
                OwnerName = "Player",
                Cards = new List<CardInstance>
                {
                    new CardInstance(playerCard1),
                    new CardInstance(playerCard2),
                    new CardInstance(playerCard3)
                }
            };

            OpponentTeam = new BattleTeam
            {
                OwnerName = "Opponent",
                Cards = new List<CardInstance>
                {
                    new CardInstance(opponentCard1),
                    new CardInstance(opponentCard2),
                    new CardInstance(opponentCard3)
                }
            };

            foreach (var card in PlayerTeam.Cards)
                MaxOutCard(card);
            foreach (var card in OpponentTeam.Cards)
                MaxOutCard(card);

            Debug.Log("═══ Player Team ═══");
            foreach (var card in PlayerTeam.Cards)
                Debug.Log($"  {card.data.cardName} [{card.data.cardClass}] " +
                          $"ATK:{card.Attack} DEF:{card.Defence} SPD:{card.Speed} " +
                          $"Ability:{card.data.ability?.abilityName ?? "None"}");
            Debug.Log($"  Team Totals → ATK:{PlayerTeam.AggregateAttack} " +
                      $"DEF:{PlayerTeam.AggregateDefence} SPD:{PlayerTeam.AggregateSpeed} " +
                      $"HP:{PlayerTeam.MaxHP}");

            Debug.Log("═══ Opponent Team ═══");
            foreach (var card in OpponentTeam.Cards)
                Debug.Log($"  {card.data.cardName} [{card.data.cardClass}] " +
                          $"ATK:{card.Attack} DEF:{card.Defence} SPD:{card.Speed} " +
                          $"Ability:{card.data.ability?.abilityName ?? "None"}");
            Debug.Log($"  Team Totals → ATK:{OpponentTeam.AggregateAttack} " +
                      $"DEF:{OpponentTeam.AggregateDefence} SPD:{OpponentTeam.AggregateSpeed} " +
                      $"HP:{OpponentTeam.MaxHP}");

            ui.SetTeamReferences(
                PlayerTeam.MaxHP,    OpponentTeam.MaxHP,
                () => PlayerTeam.CurrentHP,
                () => PlayerTeam.CurrentMana,
                () => OpponentTeam.CurrentHP,
                () => OpponentTeam.CurrentMana,
                PlayerTeam.MaxMana);

            var aiSelector = gameObject.AddComponent<AIAbilitySelector>();
            if (abilitySelectionUI != null)
                GameStateManager.Instance.SetSelectors(abilitySelectionUI, aiSelector);

            GameStateManager.Instance.StartBattle(PlayerTeam, OpponentTeam);
        }
    }
}