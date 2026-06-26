// Option A: PlayFab backing added in-place. ConnectPlayFab() is optional — without it the
// service runs in local-only mode and all existing callers compile unchanged.
//
// FUTURE — Atomic multi-resource transactions must go through a single Cloud Script.
// Example: TierUpCard(cardInstanceId) → Cloud Script validates ownership, deducts
// gold + fragments + materials atomically, upgrades card, returns new balances.
// Client-side multi-step TrySpend chains are NOT safe for production — they are
// development scaffolding only.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Dystopia.Core;
using Dystopia.Economy.Data;
using Dystopia.Networking;

namespace Dystopia.Economy.Services
{
    public class WalletService
    {
        private readonly PlayerWallet _wallet;

        private MonoBehaviour         _runner;
        private PlayFabAuthService    _auth;
        private PlayFabOperationQueue _queue;
        private bool IsPlayFabConnected => _queue != null && _auth != null && _auth.IsLoggedIn;

        // ── PlayFab currency codes / data keys (single source of truth) ──────
        private const string GoldCode         = "GD";
        private const string FragmentsCode    = "FR";
        private const string DiamondsCode     = "DM";
        private const string RaidTokensCode   = "RT";
        private const string MaterialsDataKey = "ClassMaterials";

        // ── Events (UI subscribes to these) ───────────────────────────────────
        public event Action<string, int> OnCurrencyChanged;  // currencyName, newBalance
        public event Action              OnDataLoaded;        // fires once after initial FetchAll completes

        private int _pendingFetches;

        // ── Constructor ───────────────────────────────────────────────────────
        public WalletService(PlayerWallet wallet)
        {
            _wallet = wallet;
        }

        // ── PlayFab wiring ────────────────────────────────────────────────────
        public void ConnectPlayFab(PlayFabAuthService auth, MonoBehaviour runner, PlayFabOperationQueue queue)
        {
            _auth   = auth;
            _runner = runner;
            _queue  = queue;

            if (auth.IsLoggedIn) FetchAll();
            else auth.OnLoginSuccess += FetchAll;
        }

        // ── Read-only accessors ───────────────────────────────────────────────
        public int Gold       => _wallet.Gold;
        public int Diamonds   => _wallet.Diamonds;
        public int Fragments  => _wallet.Fragments;
        public int RaidTokens => _wallet.RaidTokens;

        public int GetMaterial(CardClass cardClass)
        {
            return _wallet.ClassMaterials[cardClass];
        }

        // ── Validation ────────────────────────────────────────────────────────
        public bool CanAfford(int goldCost, int fragmentCost)
        {
            return _wallet.Gold >= goldCost && _wallet.Fragments >= fragmentCost;
        }

        public bool CanAffordTierUpgrade(int goldCost, int fragmentCost, CardClass cardClass, int materialCost)
        {
            return CanAfford(goldCost, fragmentCost)
                && _wallet.ClassMaterials[cardClass] >= materialCost;
        }

        // ── Spend (returns false if insufficient — never goes negative) ────────
        public bool TrySpend(int goldCost, int fragmentCost)
        {
            if (!CanAfford(goldCost, fragmentCost)) return false;

            int prevGold = _wallet.Gold, prevFrags = _wallet.Fragments;
            _wallet.Gold      -= goldCost;
            _wallet.Fragments -= fragmentCost;
            OnCurrencyChanged?.Invoke("Gold",      _wallet.Gold);
            OnCurrencyChanged?.Invoke("Fragments", _wallet.Fragments);

            if (IsPlayFabConnected)
            {
                bool goldFailed = false;
                _queue.Enqueue(done =>
                    PlayFabClientAPI.SubtractUserVirtualCurrency(
                        new SubtractUserVirtualCurrencyRequest { VirtualCurrency = GoldCode, Amount = goldCost },
                        r =>
                        {
                            _wallet.Gold = r.Balance;
                            OnCurrencyChanged?.Invoke("Gold", r.Balance);
                            done();
                        },
                        e =>
                        {
                            Debug.LogError($"[WalletService] Subtract {GoldCode} failed: {e.GenerateErrorReport()}");
                            goldFailed = true;
                            _wallet.Gold      = prevGold; OnCurrencyChanged?.Invoke("Gold",      prevGold);
                            _wallet.Fragments = prevFrags; OnCurrencyChanged?.Invoke("Fragments", prevFrags);
                            done();
                        }));
                _queue.Enqueue(done =>
                {
                    if (goldFailed) { done(); return; }
                    PlayFabClientAPI.SubtractUserVirtualCurrency(
                        new SubtractUserVirtualCurrencyRequest { VirtualCurrency = FragmentsCode, Amount = fragmentCost },
                        r =>
                        {
                            _wallet.Fragments = r.Balance;
                            OnCurrencyChanged?.Invoke("Fragments", r.Balance);
                            done();
                        },
                        e =>
                        {
                            Debug.LogError($"[WalletService] Subtract {FragmentsCode} failed: {e.GenerateErrorReport()}");
                            _wallet.Fragments = prevFrags; OnCurrencyChanged?.Invoke("Fragments", prevFrags);
                            done();
                        });
                });
            }
            return true;
        }

        public bool TrySpendTierUpgrade(int goldCost, int fragmentCost, CardClass cardClass, int materialCost)
        {
            if (!CanAffordTierUpgrade(goldCost, fragmentCost, cardClass, materialCost))
                return false;

            int prevGold = _wallet.Gold, prevFrags = _wallet.Fragments;
            _wallet.Gold      -= goldCost;
            _wallet.Fragments -= fragmentCost;
            _wallet.ClassMaterials[cardClass] -= materialCost;   // local-only until Cloud Script tier-up

            OnCurrencyChanged?.Invoke("Gold",                  _wallet.Gold);
            OnCurrencyChanged?.Invoke("Fragments",             _wallet.Fragments);
            OnCurrencyChanged?.Invoke($"Material_{cardClass}", _wallet.ClassMaterials[cardClass]);

            if (IsPlayFabConnected)
            {
                bool goldFailed = false;
                _queue.Enqueue(done =>
                    PlayFabClientAPI.SubtractUserVirtualCurrency(
                        new SubtractUserVirtualCurrencyRequest { VirtualCurrency = GoldCode, Amount = goldCost },
                        r =>
                        {
                            _wallet.Gold = r.Balance;
                            OnCurrencyChanged?.Invoke("Gold", r.Balance);
                            done();
                        },
                        e =>
                        {
                            Debug.LogError($"[WalletService] Subtract {GoldCode} failed: {e.GenerateErrorReport()}");
                            goldFailed = true;
                            _wallet.Gold      = prevGold; OnCurrencyChanged?.Invoke("Gold",      prevGold);
                            _wallet.Fragments = prevFrags; OnCurrencyChanged?.Invoke("Fragments", prevFrags);
                            done();
                        }));
                _queue.Enqueue(done =>
                {
                    if (goldFailed) { done(); return; }
                    PlayFabClientAPI.SubtractUserVirtualCurrency(
                        new SubtractUserVirtualCurrencyRequest { VirtualCurrency = FragmentsCode, Amount = fragmentCost },
                        r =>
                        {
                            _wallet.Fragments = r.Balance;
                            OnCurrencyChanged?.Invoke("Fragments", r.Balance);
                            done();
                        },
                        e =>
                        {
                            Debug.LogError($"[WalletService] Subtract {FragmentsCode} failed: {e.GenerateErrorReport()}");
                            _wallet.Fragments = prevFrags; OnCurrencyChanged?.Invoke("Fragments", prevFrags);
                            done();
                        });
                });
            }
            return true;
        }

        public bool TrySpendDiamonds(int amount)
        {
            if (_wallet.Diamonds < amount) return false;

            int prev = _wallet.Diamonds;
            _wallet.Diamonds -= amount;
            OnCurrencyChanged?.Invoke("Diamonds", _wallet.Diamonds);

            // TODO: Diamond spends must be Cloud Script validated before release.
            if (IsPlayFabConnected)
                _queue.Enqueue(done =>
                    PlayFabClientAPI.SubtractUserVirtualCurrency(
                        new SubtractUserVirtualCurrencyRequest { VirtualCurrency = DiamondsCode, Amount = amount },
                        r =>
                        {
                            _wallet.Diamonds = r.Balance;
                            OnCurrencyChanged?.Invoke("Diamonds", r.Balance);
                            done();
                        },
                        e =>
                        {
                            Debug.LogError($"[WalletService] Subtract {DiamondsCode} failed: {e.GenerateErrorReport()}");
                            _wallet.Diamonds = prev; OnCurrencyChanged?.Invoke("Diamonds", prev);
                            done();
                        }));
            return true;
        }

        public bool TrySpendRaidTokens(int amount)
        {
            if (_wallet.RaidTokens < amount) return false;

            int prev = _wallet.RaidTokens;
            _wallet.RaidTokens -= amount;
            OnCurrencyChanged?.Invoke("RaidTokens", _wallet.RaidTokens);

            if (IsPlayFabConnected)
                _queue.Enqueue(done =>
                    PlayFabClientAPI.SubtractUserVirtualCurrency(
                        new SubtractUserVirtualCurrencyRequest { VirtualCurrency = RaidTokensCode, Amount = amount },
                        r =>
                        {
                            _wallet.RaidTokens = r.Balance;
                            OnCurrencyChanged?.Invoke("RaidTokens", r.Balance);
                            done();
                        },
                        e =>
                        {
                            Debug.LogError($"[WalletService] Subtract {RaidTokensCode} failed: {e.GenerateErrorReport()}");
                            _wallet.RaidTokens = prev; OnCurrencyChanged?.Invoke("RaidTokens", prev);
                            done();
                        }));
            return true;
        }

        // ── Earn — disabled on client; grants go through Cloud Scripts ────────
        public void AddGold(int amount)
            => Debug.LogWarning("[WalletService] AddGold ignored — use Cloud Scripts to grant currency server-side.");

        public void AddFragments(int amount)
            => Debug.LogWarning("[WalletService] AddFragments ignored — use Cloud Scripts to grant currency server-side.");

        public void AddDiamonds(int amount)
            // TODO: Diamond grants must be Cloud Script validated before release.
            => Debug.LogWarning("[WalletService] AddDiamonds ignored — use Cloud Scripts to grant currency server-side.");

        public void AddRaidTokens(int amount)
            => Debug.LogWarning("[WalletService] AddRaidTokens ignored — use Cloud Scripts to grant currency server-side.");

        public void AddMaterial(CardClass cardClass, int amount)
            => Debug.LogWarning("[WalletService] AddMaterial ignored — use Cloud Scripts to grant materials server-side.");

        // ── Refresh ───────────────────────────────────────────────────────────
        public void RefreshBalances()
        {
            if (!IsPlayFabConnected) { Debug.LogWarning("[WalletService] RefreshBalances: not connected."); return; }
            _queue.Enqueue(done => PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                result => { ApplyCurrencies(result); done(); },
                err    => { Debug.LogError($"[WalletService] RefreshBalances failed: {err.GenerateErrorReport()}"); done(); }));
        }

        public void RefreshMaterials()
        {
            if (!IsPlayFabConnected) { Debug.LogWarning("[WalletService] RefreshMaterials: not connected."); return; }
            _queue.Enqueue(done => PlayFabClientAPI.GetUserData(
                new GetUserDataRequest { Keys = new List<string> { MaterialsDataKey } },
                result => { ApplyMaterials(result); done(); },
                err    => { Debug.LogError($"[WalletService] RefreshMaterials failed: {err.GenerateErrorReport()}"); done(); }));
        }

        public void RefreshAll()
        {
            if (!IsPlayFabConnected) { Debug.LogWarning("[WalletService] RefreshAll: not connected."); return; }
            RefreshBalances();
            RefreshMaterials();
        }

        // ── Fetch from PlayFab ────────────────────────────────────────────────
        private void FetchAll()
        {
            _pendingFetches = 2;
            _queue.Enqueue(done => PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                result => { ApplyCurrencies(result); CheckAllFetched(); done(); },
                err    =>
                {
                    Debug.LogWarning($"[WalletService] Currency fetch failed: {err.GenerateErrorReport()}");
                    CheckAllFetched();
                    done();
                    _runner.StartCoroutine(RetryCurrencies());
                }));
            _queue.Enqueue(done => PlayFabClientAPI.GetUserData(
                new GetUserDataRequest { Keys = new List<string> { MaterialsDataKey } },
                result => { ApplyMaterials(result); CheckAllFetched(); done(); },
                err    =>
                {
                    Debug.LogWarning($"[WalletService] Materials fetch failed: {err.GenerateErrorReport()}");
                    CheckAllFetched();
                    done();
                    _runner.StartCoroutine(RetryMaterials());
                }));
        }

        private void CheckAllFetched()
        {
            if (--_pendingFetches == 0) OnDataLoaded?.Invoke();
        }

        private IEnumerator RetryCurrencies()
        {
            yield return new WaitForSeconds(2f);
            _queue.Enqueue(done => PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
                result => { ApplyCurrencies(result); done(); },
                err    => { Debug.LogError($"[WalletService] Currency retry failed: {err.GenerateErrorReport()}. Using cached values."); done(); }));
        }

        private IEnumerator RetryMaterials()
        {
            yield return new WaitForSeconds(2f);
            _queue.Enqueue(done => PlayFabClientAPI.GetUserData(
                new GetUserDataRequest { Keys = new List<string> { MaterialsDataKey } },
                result => { ApplyMaterials(result); done(); },
                err    => { Debug.LogError($"[WalletService] Materials retry failed: {err.GenerateErrorReport()}. Using cached values."); done(); }));
        }

        private void ApplyCurrencies(GetUserInventoryResult result)
        {
            var vc = result.VirtualCurrency;
            if (vc.TryGetValue(GoldCode,       out int g))  _wallet.Gold       = g;
            if (vc.TryGetValue(FragmentsCode,  out int f))  _wallet.Fragments  = f;
            if (vc.TryGetValue(DiamondsCode,   out int d))  _wallet.Diamonds   = d;
            if (vc.TryGetValue(RaidTokensCode, out int rt)) _wallet.RaidTokens = rt;

            if (result.VirtualCurrencyRechargeTimes != null &&
                result.VirtualCurrencyRechargeTimes.TryGetValue(RaidTokensCode, out var recharge))
                Debug.Log($"[WalletService] RT recharges in {recharge.SecondsToRecharge}s (next: +1 token)");

            Debug.Log($"[WalletService] Currencies synced — GD:{_wallet.Gold} FR:{_wallet.Fragments} DM:{_wallet.Diamonds} RT:{_wallet.RaidTokens}");
            OnCurrencyChanged?.Invoke("Gold",       _wallet.Gold);
            OnCurrencyChanged?.Invoke("Fragments",  _wallet.Fragments);
            OnCurrencyChanged?.Invoke("Diamonds",   _wallet.Diamonds);
            OnCurrencyChanged?.Invoke("RaidTokens", _wallet.RaidTokens);
        }

        private void ApplyMaterials(GetUserDataResult result)
        {
            if (result.Data == null || !result.Data.TryGetValue(MaterialsDataKey, out var record))
            {
                Debug.Log("[WalletService] ClassMaterials key not found — new player, using zeros.");
                return;
            }

            var json = JsonUtility.FromJson<ClassMaterialsJson>(record.Value);
            // CardClass enum from Core/Enums.cs: Assassin, Tank, Mage, Healer, Support, Guardian
            foreach (CardClass cls in Enum.GetValues(typeof(CardClass)))
            {
                int value = cls switch
                {
                    CardClass.Assassin => json.assassin,
                    CardClass.Tank     => json.tank,
                    CardClass.Mage     => json.mage,
                    CardClass.Healer   => json.healer,
                    CardClass.Support  => json.support,
                    CardClass.Guardian => json.guardian,
                    _                  => 0
                };
                _wallet.ClassMaterials[cls] = value;
                OnCurrencyChanged?.Invoke($"Material_{cls}", value);
            }
            Debug.Log("[WalletService] ClassMaterials synced from Player Data.");
        }

        // ── Inner types ───────────────────────────────────────────────────────
        [Serializable]
        private class ClassMaterialsJson
        {
            // Field names must be lowercase to match PlayFab Player Data JSON keys
            public int assassin, tank, mage, healer, support, guardian;
        }
    }
}
