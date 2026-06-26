using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Dystopia.Economy.Data;
using Dystopia.Economy.Services;

namespace Dystopia.Networking
{
    public class CloudScriptService
    {
        private readonly PlayFabAuthService    _auth;
        private readonly WalletService         _wallet;
        private readonly PlayFabOperationQueue _queue;

        public CloudScriptService(PlayFabAuthService auth, WalletService wallet, PlayFabOperationQueue queue)
        {
            _auth   = auth;
            _wallet = wallet;
            _queue  = queue;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void GrantCurrency(string currencyCode, int amount,
            Action onSuccess = null, Action<string> onFailed = null)
        {
            Execute("GrantCurrency",
                new { currencyCode, amount },
                onSuccess, onFailed);
        }

        public void GrantMaterial(string className, int amount,
            Action onSuccess = null, Action<string> onFailed = null)
        {
            Execute("GrantMaterial",
                new { className, amount },
                onSuccess, onFailed);
        }

        public void GrantTestResources(
            Action onSuccess = null, Action<string> onFailed = null)
        {
            // Split into 5 separate queue entries (1 Cloud Script call each) to avoid
            // the 6-server-call limit inside the old monolithic GrantTestResources handler.
            GrantCurrency("GD", 1000, null, onFailed);
            GrantCurrency("FR",  500, null, onFailed);
            GrantCurrency("DM",  100, null, onFailed);
            GrantCurrency("RT",   10, null, onFailed);
            Execute("GrantTestMaterials", null, onSuccess, onFailed);
        }

        public void ClaimBattlePayout(int goldReward, int fragmentReward,
            Action onSuccess = null, Action<string> onFailed = null)
        {
            Execute("BattlePayout",
                new { goldReward, fragmentReward },
                onSuccess, onFailed);
        }

        public void SpendMultipleCurrencies(List<CurrencyCost> costs,
            MaterialCost materialCost = null,
            Action onSuccess = null, Action<string> onFailed = null)
        {
            Execute("SpendMultipleCurrencies",
                new { costs, materialCost },
                onSuccess, onFailed);
        }

        // onSuccess receives the PlayFab ItemInstanceId of the newly granted card
        public void GrantCard(string cardId,
            Action<string> onSuccess = null, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn)
            {
                Debug.LogWarning("[CloudScript] Cannot GrantCard — not logged in.");
                onFailed?.Invoke("Not logged in");
                return;
            }

            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "GrantCard",
                FunctionParameter       = new { cardId },
                GeneratePlayStreamEvent = true
            };

            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleGrantCardResult(result, onSuccess, onFailed); done(); },
                    error  =>
                    {
                        var msg = error.GenerateErrorReport();
                        Debug.LogError($"[CloudScript] GrantCard request failed: {msg}");
                        onFailed?.Invoke(msg);
                        done();
                    }));
        }

        public void AddDuplicate(string itemInstanceId,
            Action onSuccess = null, Action<string> onFailed = null)
        {
            Execute("AddDuplicate", new { itemInstanceId }, onSuccess, onFailed);
        }

        public void UpdateCardState(string itemInstanceId, int level, int tier,
            int resonance, int duplicateCount,
            Action onSuccess = null, Action<string> onFailed = null)
        {
            Execute("UpdateCardState",
                new { itemInstanceId, level, tier, resonance, duplicateCount },
                onSuccess, onFailed);
        }

        public void SaveDecks(string[][] deckIds, int activeDeckIndex,
            Action onSuccess = null, Action<string> onFailed = null)
        {
            var args = new SaveDecksArgs
            {
                rows            = new DeckRowArg[deckIds.Length],
                activeDeckIndex = activeDeckIndex
            };
            for (int i = 0; i < deckIds.Length; i++)
                args.rows[i] = new DeckRowArg { slots = deckIds[i] };
            Execute("SaveDecks", args, onSuccess, onFailed);
        }

        public void ClaimDailyReward(
            Action<DailyRewardResult> onSuccess, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn)
            {
                Debug.LogWarning("[CloudScript] Cannot call ClaimDailyReward — not logged in.");
                onFailed?.Invoke("Not logged in");
                return;
            }

            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "ClaimDailyReward",
                FunctionParameter       = null,
                GeneratePlayStreamEvent = true
            };

            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleDailyRewardResult(result, onSuccess, onFailed); done(); },
                    error  =>
                    {
                        var msg = error.GenerateErrorReport();
                        Debug.LogError($"[CloudScript] ClaimDailyReward request failed: {msg}");
                        onFailed?.Invoke(msg); done();
                    }));
        }

        public void GetDailyRewardStatus(
            Action<DailyRewardStatus> onSuccess, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn)
            {
                Debug.LogWarning("[CloudScript] Cannot call GetDailyRewardStatus — not logged in.");
                onFailed?.Invoke("Not logged in");
                return;
            }

            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "GetDailyRewardStatus",
                FunctionParameter       = null,
                GeneratePlayStreamEvent = true
            };

            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleDailyRewardStatusResult(result, onSuccess, onFailed); done(); },
                    error  =>
                    {
                        var msg = error.GenerateErrorReport();
                        Debug.LogError($"[CloudScript] GetDailyRewardStatus request failed: {msg}");
                        onFailed?.Invoke(msg); done();
                    }));
        }

        public void GetMarketListings(
            Action<List<MarketListing>> onSuccess, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn) { onFailed?.Invoke("Not logged in"); return; }
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "GetMarketListings",
                FunctionParameter       = null,
                GeneratePlayStreamEvent = true
            };
            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleMarketListingsResult(result, onSuccess, onFailed); done(); },
                    error  => { onFailed?.Invoke(error.GenerateErrorReport()); done(); }));
        }

        public void GetMyListings(
            Action<List<MarketListing>> onSuccess, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn) { onFailed?.Invoke("Not logged in"); return; }
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "GetMyListings",
                FunctionParameter       = null,
                GeneratePlayStreamEvent = true
            };
            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleMarketListingsResult(result, onSuccess, onFailed); done(); },
                    error  => { onFailed?.Invoke(error.GenerateErrorReport()); done(); }));
        }

        public void ListCard(string itemInstanceId, int price, string listingType,
            string sellerDisplayName,
            Action<ListCardResult> onSuccess, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn) { onFailed?.Invoke("Not logged in"); return; }
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "ListCard",
                FunctionParameter       = new { itemInstanceId, price, listingType, sellerDisplayName },
                GeneratePlayStreamEvent = true
            };
            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleListCardResult(result, onSuccess, onFailed); done(); },
                    error  => { onFailed?.Invoke(error.GenerateErrorReport()); done(); }));
        }

        public void DelistCard(string listingId,
            Action<DelistCardResult> onSuccess, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn) { onFailed?.Invoke("Not logged in"); return; }
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "DelistCard",
                FunctionParameter       = new { listingId },
                GeneratePlayStreamEvent = true
            };
            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleDelistCardResult(result, onSuccess, onFailed); done(); },
                    error  => { onFailed?.Invoke(error.GenerateErrorReport()); done(); }));
        }

        public void BuyCard(string listingId,
            Action<BuyCardResult> onSuccess, Action<string> onFailed = null)
        {
            if (!_auth.IsLoggedIn) { onFailed?.Invoke("Not logged in"); return; }
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = "BuyCard",
                FunctionParameter       = new { listingId },
                GeneratePlayStreamEvent = true
            };
            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleBuyCardResult(result, onSuccess, onFailed); done(); },
                    error  => { onFailed?.Invoke(error.GenerateErrorReport()); done(); }));
        }

        // ── Core dispatch ─────────────────────────────────────────────────────

        private void Execute(string functionName, object parameters,
            Action onSuccess, Action<string> onFailed)
        {
            if (!_auth.IsLoggedIn)
            {
                Debug.LogWarning($"[CloudScript] Cannot call {functionName} — not logged in.");
                onFailed?.Invoke("Not logged in");
                return;
            }

            var request = new ExecuteCloudScriptRequest
            {
                FunctionName            = functionName,
                FunctionParameter       = parameters,
                GeneratePlayStreamEvent = true
            };

            _queue.Enqueue(done =>
                PlayFabClientAPI.ExecuteCloudScript(request,
                    result => { HandleResult(result, onSuccess, onFailed); done(); },
                    error  =>
                    {
                        var msg = error.GenerateErrorReport();
                        Debug.LogError($"[CloudScript] Request failed for {functionName}: {msg}");
                        onFailed?.Invoke(msg);
                        done();
                    }));
        }

        // ── Result handling ───────────────────────────────────────────────────

        private void HandleResult(ExecuteCloudScriptResult result,
            Action onSuccess, Action<string> onFailed)
        {
            // 1. Pipe JS-side logs to Unity console
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            // 2. Runtime error (JS threw or crashed)
            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError(
                    $"[CloudScript] Runtime error in {result.FunctionName}: {msg}\n{result.Error.StackTrace}");
                onFailed?.Invoke(msg);
                return;
            }

            // 3. Application-level error (makeError() returned from JS)
            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);
            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool b && b)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                var appErr = errMsg?.ToString() ?? "Unknown app error";
                Debug.LogError($"[CloudScript] App error in {result.FunctionName}: {appErr}");
                onFailed?.Invoke(appErr);
                return;
            }

            // 4. Success — sync local cache then notify caller
            Debug.Log($"[CloudScript] {result.FunctionName} succeeded.");
            _wallet.RefreshAll();
            onSuccess?.Invoke();
        }

        private void HandleGrantCardResult(ExecuteCloudScriptResult result,
            Action<string> onSuccess, Action<string> onFailed)
        {
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError($"[CloudScript] GrantCard runtime error: {msg}\n{result.Error.StackTrace}");
                onFailed?.Invoke(msg);
                return;
            }

            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool b && b)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                var appErr = errMsg?.ToString() ?? "Unknown error";
                Debug.LogError($"[CloudScript] GrantCard app error: {appErr}");
                onFailed?.Invoke(appErr);
                return;
            }

            string itemInstanceId = null;
            if (dict != null && dict.TryGetValue("data", out var dataObj))
            {
                var dataJson = PlayFab.Json.PlayFabSimpleJson.SerializeObject(dataObj);
                var dataDict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(dataJson);
                if (dataDict != null && dataDict.TryGetValue("itemInstanceId", out var iid))
                    itemInstanceId = iid?.ToString();
            }

            Debug.Log($"[CloudScript] GrantCard succeeded → {itemInstanceId}");
            onSuccess?.Invoke(itemInstanceId);
        }

        private void HandleDailyRewardResult(ExecuteCloudScriptResult result,
            Action<DailyRewardResult> onSuccess, Action<string> onFailed)
        {
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError($"[CloudScript] ClaimDailyReward runtime error: {msg}\n{result.Error.StackTrace}");
                onFailed?.Invoke(msg); return;
            }

            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool b && b)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                onFailed?.Invoke(errMsg?.ToString() ?? "Unknown error"); return;
            }

            DailyRewardResult reward = null;
            if (dict != null && dict.TryGetValue("data", out var dataObj))
            {
                var dataJson = PlayFab.Json.PlayFabSimpleJson.SerializeObject(dataObj);
                reward = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<DailyRewardResult>(dataJson);
            }

            if (reward == null) { onFailed?.Invoke("Failed to parse reward result"); return; }

            Debug.Log($"[CloudScript] ClaimDailyReward: streak={reward.streak} +{reward.reward} DM");
            _wallet.RefreshAll();
            onSuccess?.Invoke(reward);
        }

        private void HandleDailyRewardStatusResult(ExecuteCloudScriptResult result,
            Action<DailyRewardStatus> onSuccess, Action<string> onFailed)
        {
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError($"[CloudScript] GetDailyRewardStatus runtime error: {msg}\n{result.Error.StackTrace}");
                onFailed?.Invoke(msg); return;
            }

            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool b && b)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                onFailed?.Invoke(errMsg?.ToString() ?? "Unknown error"); return;
            }

            DailyRewardStatus status = null;
            if (dict != null && dict.TryGetValue("data", out var dataObj))
            {
                var dataJson = PlayFab.Json.PlayFabSimpleJson.SerializeObject(dataObj);
                status = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<DailyRewardStatus>(dataJson);
            }

            if (status == null) { onFailed?.Invoke("Failed to parse status result"); return; }

            Debug.Log($"[CloudScript] GetDailyRewardStatus: canClaim={status.canClaim} streak={status.currentStreak}");
            onSuccess?.Invoke(status);
        }

        private void HandleMarketListingsResult(ExecuteCloudScriptResult result,
            Action<List<MarketListing>> onSuccess, Action<string> onFailed)
        {
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError($"[CloudScript] {result.FunctionName} runtime error: {msg}");
                onFailed?.Invoke(msg); return;
            }

            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool bErr && bErr)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                onFailed?.Invoke(errMsg?.ToString() ?? "Unknown error"); return;
            }

            List<MarketListing> listings = new List<MarketListing>();
            if (dict != null && dict.TryGetValue("data", out var dataObj))
            {
                var dataJson = PlayFab.Json.PlayFabSimpleJson.SerializeObject(dataObj);
                var wrapper  = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<MarketListingsWrapper>(dataJson);
                if (wrapper?.listings != null) listings = new List<MarketListing>(wrapper.listings);
            }

            onSuccess?.Invoke(listings);
        }

        private void HandleListCardResult(ExecuteCloudScriptResult result,
            Action<ListCardResult> onSuccess, Action<string> onFailed)
        {
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError($"[CloudScript] ListCard runtime error: {msg}");
                onFailed?.Invoke(msg); return;
            }

            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool bErr && bErr)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                onFailed?.Invoke(errMsg?.ToString() ?? "Unknown error"); return;
            }

            ListCardResult listResult = null;
            if (dict != null && dict.TryGetValue("data", out var dataObj))
            {
                var dataJson = PlayFab.Json.PlayFabSimpleJson.SerializeObject(dataObj);
                listResult = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<ListCardResult>(dataJson);
            }

            if (listResult == null) { onFailed?.Invoke("Failed to parse ListCard result"); return; }

            Debug.Log($"[CloudScript] ListCard: listingId={listResult.listingId} fee={listResult.fee}");
            _wallet.RefreshAll();
            onSuccess?.Invoke(listResult);
        }

        private void HandleDelistCardResult(ExecuteCloudScriptResult result,
            Action<DelistCardResult> onSuccess, Action<string> onFailed)
        {
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError($"[CloudScript] DelistCard runtime error: {msg}");
                onFailed?.Invoke(msg); return;
            }

            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool bErr && bErr)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                onFailed?.Invoke(errMsg?.ToString() ?? "Unknown error"); return;
            }

            DelistCardResult delistResult = null;
            if (dict != null && dict.TryGetValue("data", out var dataObj))
            {
                var dataJson = PlayFab.Json.PlayFabSimpleJson.SerializeObject(dataObj);
                delistResult = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<DelistCardResult>(dataJson);
            }

            if (delistResult == null) { onFailed?.Invoke("Failed to parse DelistCard result"); return; }

            Debug.Log($"[CloudScript] DelistCard: cardId={delistResult.cardId}");
            onSuccess?.Invoke(delistResult);
        }

        private void HandleBuyCardResult(ExecuteCloudScriptResult result,
            Action<BuyCardResult> onSuccess, Action<string> onFailed)
        {
            if (result.Logs != null)
                foreach (var entry in result.Logs)
                    Debug.Log($"[CloudScript] [{entry.Level}] {entry.Message}");

            if (result.Error != null)
            {
                var msg = $"{result.Error.Error}: {result.Error.Message}";
                Debug.LogError($"[CloudScript] BuyCard runtime error: {msg}");
                onFailed?.Invoke(msg); return;
            }

            var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
            var dict = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue("error", out var errFlag) && errFlag is bool bErr && bErr)
            {
                dict.TryGetValue("errorMessage", out var errMsg);
                onFailed?.Invoke(errMsg?.ToString() ?? "Unknown error"); return;
            }

            BuyCardResult buyResult = null;
            if (dict != null && dict.TryGetValue("data", out var dataObj))
            {
                var dataJson = PlayFab.Json.PlayFabSimpleJson.SerializeObject(dataObj);
                buyResult = PlayFab.Json.PlayFabSimpleJson.DeserializeObject<BuyCardResult>(dataJson);
            }

            if (buyResult == null) { onFailed?.Invoke("Failed to parse BuyCard result"); return; }

            Debug.Log($"[CloudScript] BuyCard: cardId={buyResult.cardId} isNew={buyResult.isNewCard}");
            _wallet.RefreshAll();
            onSuccess?.Invoke(buyResult);
        }
    }

    // ── Shared data models ────────────────────────────────────────────────────

    [Serializable]
    internal class SaveDecksArgs { public DeckRowArg[] rows; public int activeDeckIndex; }

    [Serializable]
    internal class DeckRowArg { public string[] slots; }

    [Serializable]
    public class CurrencyCost
    {
        public string currencyCode;
        public int    amount;
    }

    [Serializable]
    public class MaterialCost
    {
        public string className;
        public int    amount;
    }
}
