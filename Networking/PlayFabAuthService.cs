using System;
using System.Collections;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace Dystopia.Networking
{
    public class PlayFabAuthService
    {
        public bool   IsLoggedIn   { get; private set; }
        public string PlayFabId    { get; private set; }
        public bool   IsNewAccount { get; private set; }

        public event Action         OnLoginSuccess;
        public event Action<string> OnLoginFailed;

        private readonly MonoBehaviour _runner;
        private int _retryCount;
        private const int MaxRetries = 3;

        // Callbacks used by the explicit-callback login paths (LoginAsGuest, etc.)
        private Action         _pendingSuccess;
        private Action<string> _pendingFailed;

        public PlayFabAuthService(MonoBehaviour coroutineRunner)
        {
            _runner = coroutineRunner;
        }

        // ── Anonymous device login (existing — used by autoLogin test scenes) ──

        public void Login()
        {
            _retryCount     = 0;
            _pendingSuccess = null;
            _pendingFailed  = null;
            AttemptLogin();
        }

        // ── New explicit-callback methods for TitleController ─────────────────

        public void LoginAsGuest(Action onSuccess, Action<string> onFailed)
        {
            _retryCount     = 0;
            _pendingSuccess = onSuccess;
            _pendingFailed  = onFailed;
            AttemptLogin();
        }

        public void LoginWithEmail(string email, string password,
            Action onSuccess, Action<string> onFailed)
        {
            PlayFabClientAPI.LoginWithEmailAddress(
                new LoginWithEmailAddressRequest { Email = email, Password = password },
                result => HandleSuccess(result.PlayFabId, result.NewlyCreated, onSuccess),
                error  => onFailed?.Invoke(FriendlyError(error)));
        }

        public void Register(string email, string password, string username,
            Action onSuccess, Action<string> onFailed)
        {
            PlayFabClientAPI.RegisterPlayFabUser(
                new RegisterPlayFabUserRequest
                {
                    Email    = email,
                    Password = password,
                    Username = username,
                    RequireBothUsernameAndEmail = false
                },
                result => HandleSuccess(result.PlayFabId, newlyCreated: true, onSuccess),
                error  => onFailed?.Invoke(FriendlyError(error)));
        }

        // ── Logout ────────────────────────────────────────────────────────────

        public void Logout()
        {
            PlayFabClientAPI.ForgetAllCredentials();
            IsLoggedIn   = false;
            PlayFabId    = null;
            IsNewAccount = false;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void AttemptLogin()
        {
            Debug.Log($"[PlayFabAuth] Login attempt {_retryCount + 1}/{MaxRetries + 1}");

#if UNITY_EDITOR
            PlayFabClientAPI.LoginWithCustomID(
                new LoginWithCustomIDRequest
                {
                    CustomId      = "Editor_" + SystemInfo.deviceUniqueIdentifier,
                    CreateAccount = true
                },
                OnSuccess, OnError);

#elif UNITY_ANDROID
            PlayFabClientAPI.LoginWithAndroidDeviceID(
                new LoginWithAndroidDeviceIDRequest
                {
                    AndroidDeviceId = SystemInfo.deviceUniqueIdentifier,
                    CreateAccount   = true
                },
                OnSuccess, OnError);

#elif UNITY_IOS
            PlayFabClientAPI.LoginWithIOSDeviceID(
                new LoginWithIOSDeviceIDRequest
                {
                    DeviceId      = SystemInfo.deviceUniqueIdentifier,
                    CreateAccount = true
                },
                OnSuccess, OnError);

#else
            PlayFabClientAPI.LoginWithCustomID(
                new LoginWithCustomIDRequest
                {
                    CustomId      = "Device_" + SystemInfo.deviceUniqueIdentifier,
                    CreateAccount = true
                },
                OnSuccess, OnError);
#endif
        }

        private void OnSuccess(LoginResult result)
        {
            HandleSuccess(result.PlayFabId, result.NewlyCreated, _pendingSuccess);
            _pendingSuccess = null;
            _pendingFailed  = null;
        }

        private void HandleSuccess(string playFabId, bool newlyCreated, Action onSuccess)
        {
            IsLoggedIn   = true;
            PlayFabId    = playFabId;
            IsNewAccount = newlyCreated;

            string accountType = newlyCreated ? "new account" : "returning player";
            Debug.Log($"[PlayFabAuth] Login successful — PlayFabId: {playFabId} ({accountType})");

            OnLoginSuccess?.Invoke();
            onSuccess?.Invoke();
        }

        private void OnError(PlayFabError error)
        {
            Debug.LogWarning($"[PlayFabAuth] Attempt {_retryCount + 1} failed: {error.GenerateErrorReport()}");

            if (_retryCount < MaxRetries)
            {
                _retryCount++;
                float delay = Mathf.Pow(2f, _retryCount - 1); // 1s, 2s, 4s
                _runner.StartCoroutine(RetryAfterDelay(delay));
            }
            else
            {
                Debug.LogError($"[PlayFabAuth] All {MaxRetries + 1} login attempts failed.");
                string msg = FriendlyError(error);
                OnLoginFailed?.Invoke(msg);
                _pendingFailed?.Invoke(msg);
                _pendingSuccess = null;
                _pendingFailed  = null;
            }
        }

        private IEnumerator RetryAfterDelay(float seconds)
        {
            Debug.Log($"[PlayFabAuth] Retrying in {seconds}s...");
            yield return new WaitForSeconds(seconds);
            AttemptLogin();
        }

        private static string FriendlyError(PlayFabError error) => error.Error switch
        {
            PlayFabErrorCode.AccountNotFound          => "No account found with that email.",
            PlayFabErrorCode.InvalidEmailOrPassword   => "Incorrect password.",
            PlayFabErrorCode.EmailAddressNotAvailable => "Email already registered — try logging in.",
            PlayFabErrorCode.InvalidPassword          => "Password must be at least 6 characters.",
            _                                         => error.ErrorMessage
        };
    }
}
