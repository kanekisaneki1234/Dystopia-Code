using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Dystopia.Cards;
using Dystopia.Core;

namespace Dystopia.UI
{
    public class BattleModeUI : MonoBehaviour
    {
        [Header("Deck Preview")]
        [SerializeField] private GameObject deckPreviewRoot;
        [SerializeField] private TMP_Text   deckIndexLabel;
        [SerializeField] private TMP_Text[] cardNameLabels;   // 3 elements
        [SerializeField] private TMP_Text[] cardStatsLabels;  // 3 elements

        [Header("Actions")]
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Button testBattleButton;
        [SerializeField] private Button   raidBossButton;
        [SerializeField] private TMP_Text comingSoonText;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        [Header("Scene Names")]
        [SerializeField] private string battleSceneName = "BattleTest";
        [SerializeField] private string mainMenuScene   = "MainMenuScene";

        private void Awake()
        {
            startBattleButton?.onClick.AddListener(OnStartBattle);
            testBattleButton?.onClick.AddListener(OnTestBattle);
            raidBossButton?.onClick.AddListener(OnRaidBoss);
            backButton?.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));

            SetStartButtonEnabled(false);
            if (deckPreviewRoot != null) deckPreviewRoot.SetActive(false);
            if (comingSoonText  != null) comingSoonText.gameObject.SetActive(false);
        }

        public void ShowLoading(string message)
        {
            SetStartButtonEnabled(false);
        }

        public void ShowDeckReady(int deckNumber, List<CardInstance> cards)
        {
            if (deckPreviewRoot != null) deckPreviewRoot.SetActive(true);
            if (deckIndexLabel  != null) deckIndexLabel.text  = $"Deck {deckNumber} selected";

            for (int i = 0; i < 3; i++)
            {
                var card = i < cards.Count ? cards[i] : null;
                if (cardNameLabels  != null && i < cardNameLabels.Length && cardNameLabels[i]  != null)
                    cardNameLabels[i].text  = card != null ? card.data.cardName : "—";
                if (cardStatsLabels != null && i < cardStatsLabels.Length && cardStatsLabels[i] != null)
                    cardStatsLabels[i].text = card != null
                        ? $"ATK {card.Attack}  DEF {card.Defence}  SPD {card.Speed}  HP {card.HP}"
                        : "";
            }

            SetStartButtonEnabled(true);
        }

        public void ShowNoDeckError()
        {
            if (deckPreviewRoot != null) deckPreviewRoot.SetActive(false);
            SetStartButtonEnabled(false);
        }

        private void OnStartBattle()
        {
            GameSession.IsTestBattle = false;
            SceneManager.LoadScene(battleSceneName);
        }

        private void OnTestBattle()
        {
            GameSession.IsTestBattle = true;
            SceneManager.LoadScene(battleSceneName);
        }

        private void OnRaidBoss()
        {
            if (comingSoonText == null) return;
            StopAllCoroutines();
            StartCoroutine(ShowComingSoon());
        }

        private IEnumerator ShowComingSoon()
        {
            comingSoonText.gameObject.SetActive(true);
            var color = comingSoonText.color;

            // Fade in
            color.a = 0f;
            comingSoonText.color = color;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                color.a = t / 0.3f;
                comingSoonText.color = color;
                yield return null;
            }

            // Hold
            color.a = 1f;
            comingSoonText.color = color;
            yield return new WaitForSeconds(2f);

            // Fade out
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                color.a = 1f - (t / 0.5f);
                comingSoonText.color = color;
                yield return null;
            }

            color.a = 0f;
            comingSoonText.color = color;
            comingSoonText.gameObject.SetActive(false);
        }

        private void SetStartButtonEnabled(bool enabled)
        {
            if (startBattleButton != null) startBattleButton.interactable = enabled;
        }
    }
}
