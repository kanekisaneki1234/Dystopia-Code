using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Dystopia.Cards;
using Dystopia.Networking;

namespace Dystopia.Economy.Services
{
    public class DeckService
    {
        public const int DeckCount = 3;
        public const int SlotCount = 3;

        private readonly CardInstance[][] _decks = new CardInstance[DeckCount][];

        private PlayFabAuthService    _auth;
        private MonoBehaviour         _runner;
        private CloudScriptService    _cloudSvc;
        private PlayFabOperationQueue _queue;

        public int  ActiveBattleDeckIndex { get; private set; }
        public bool IsDecksLoaded         { get; private set; }

        public event Action OnDecksLoaded;
        public event Action OnDeckChanged;

        public DeckService()
        {
            for (int i = 0; i < DeckCount; i++)
                _decks[i] = new CardInstance[SlotCount];
        }

        // ── Public API ────────────────────────────────────────────────────────

        public CardInstance[] GetDeck(int index) => _decks[index];

        public void SetCard(int deckIdx, int slotIdx, CardInstance card)
        {
            _decks[deckIdx][slotIdx] = card;
            OnDeckChanged?.Invoke();
        }

        public void RemoveCard(int deckIdx, int slotIdx)
        {
            _decks[deckIdx][slotIdx] = null;
            OnDeckChanged?.Invoke();
        }

        public void SelectForBattle(int deckIdx)
        {
            ActiveBattleDeckIndex = deckIdx;
            OnDeckChanged?.Invoke();
        }

        public bool IsDuplicate(int deckIdx, CardInstance card)
            => Array.Exists(_decks[deckIdx], s => s != null && s == card);

        public string[][] GetDeckIds()
        {
            var result = new string[DeckCount][];
            for (int d = 0; d < DeckCount; d++)
            {
                result[d] = new string[SlotCount];
                for (int s = 0; s < SlotCount; s++)
                    result[d][s] = _decks[d][s]?.PlayFabItemInstanceId ?? "";
            }
            return result;
        }

        // ── PlayFab connection ────────────────────────────────────────────────

        public void ConnectPlayFab(PlayFabAuthService auth, MonoBehaviour runner,
            CloudScriptService cloudSvc, PlayFabOperationQueue queue)
        {
            _auth     = auth;
            _runner   = runner;
            _cloudSvc = cloudSvc;
            _queue    = queue;
        }

        public void FetchDecks(CollectionService collection)
        {
            if (_queue == null) { IsDecksLoaded = true; OnDecksLoaded?.Invoke(); return; }

            _queue.Enqueue(done => PlayFabClientAPI.GetUserData(
                new GetUserDataRequest { Keys = new List<string> { "PlayerDecks" } },
                result =>
                {
                    if (result.Data != null && result.Data.TryGetValue("PlayerDecks", out var entry))
                        ApplyDecks(entry.Value, collection);
                    else
                        Debug.Log("[DeckService] No saved decks found — using empty defaults.");
                    IsDecksLoaded = true;
                    OnDecksLoaded?.Invoke();
                    done();
                },
                error =>
                {
                    Debug.LogError($"[DeckService] FetchDecks failed: {error.GenerateErrorReport()}");
                    IsDecksLoaded = true;
                    OnDecksLoaded?.Invoke();
                    done();
                }));
        }

        public void SaveDecks(Action onSuccess = null, Action<string> onFailed = null)
        {
            if (_cloudSvc == null) { onSuccess?.Invoke(); return; }
            _cloudSvc.SaveDecks(GetDeckIds(), ActiveBattleDeckIndex, onSuccess, onFailed);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void ApplyDecks(string json, CollectionService collection)
        {
            var wrapper = UnityEngine.JsonUtility.FromJson<DeckJson>(json);
            if (wrapper?.rows == null) return;

            ActiveBattleDeckIndex = Mathf.Clamp(wrapper.activeDeckIndex, 0, DeckCount - 1);

            for (int d = 0; d < Mathf.Min(wrapper.rows.Length, DeckCount); d++)
            {
                var row = wrapper.rows[d];
                if (row?.slots == null) continue;
                for (int s = 0; s < Mathf.Min(row.slots.Length, SlotCount); s++)
                {
                    var id = row.slots[s];
                    if (!string.IsNullOrEmpty(id))
                        _decks[d][s] = collection.OwnedCards
                            .FirstOrDefault(c => c.PlayFabItemInstanceId == id);
                }
            }
        }

        // ── JSON wrappers (JsonUtility-compatible) ────────────────────────────

        [Serializable] private class DeckJson { public DeckRow[] rows; public int activeDeckIndex; }
        [Serializable] private class DeckRow  { public string[]  slots; }
    }
}