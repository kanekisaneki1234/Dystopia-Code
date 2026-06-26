using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Dystopia.Networking;

namespace Dystopia.UI
{
    public class TitleController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private CanvasGroup logoPanelGroup;
        [SerializeField] private CanvasGroup authPanelGroup;

        [Header("Tabs")]
        [SerializeField] private Button loginTabButton;
        [SerializeField] private Button signUpTabButton;

        [Header("Login View")]
        [SerializeField] private GameObject     loginView;
        [SerializeField] private TMP_InputField loginEmailField;
        [SerializeField] private TMP_InputField loginPasswordField;
        [SerializeField] private Button         loginButton;
        [SerializeField] private TMP_Text       loginErrorText;

        [Header("Sign Up View")]
        [SerializeField] private GameObject     signUpView;
        [SerializeField] private TMP_InputField signUpEmailField;
        [SerializeField] private TMP_InputField signUpPasswordField;
        [SerializeField] private TMP_InputField signUpConfirmField;
        [SerializeField] private Button         signUpButton;
        [SerializeField] private TMP_Text       signUpErrorText;

        [Header("Guest")]
        [SerializeField] private Button guestButton;

        [Header("Loading")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TMP_Text   loadingText;

        [Header("Scene")]
        [SerializeField] private string mainMenuScene = "MainMenuScene";

        private static readonly Color TabActive   = Color.white;
        private static readonly Color TabInactive = new Color(0.5f, 0.5f, 0.5f);

        private void Start()
        {
            logoPanelGroup.alpha = 0f;
            authPanelGroup.alpha = 0f;
            authPanelGroup.gameObject.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);

            loginTabButton?.onClick.AddListener(() => ShowTab(login: true));
            signUpTabButton?.onClick.AddListener(() => ShowTab(login: false));
            loginButton?.onClick.AddListener(OnLogin);
            signUpButton?.onClick.AddListener(OnSignUp);
            guestButton?.onClick.AddListener(OnGuest);

            ShowTab(login: true);
            HideErrors();

            StartCoroutine(LogoSequence());
        }

        // ── Logo splash ───────────────────────────────────────────────────────

        private IEnumerator LogoSequence()
        {
            logoPanelGroup.gameObject.SetActive(true);
            yield return Fade(logoPanelGroup, 0f, 1f, 1.0f);
            yield return new WaitForSeconds(1.5f);

            authPanelGroup.alpha = 1f;
            authPanelGroup.gameObject.SetActive(true);
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed    += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        // ── Tab switching ─────────────────────────────────────────────────────

        private void ShowTab(bool login)
        {
            if (loginView  != null) loginView.SetActive(login);
            if (signUpView != null) signUpView.SetActive(!login);
            if (loginTabButton  != null && loginTabButton.image  != null)
                loginTabButton.image.color  = login  ? TabActive : TabInactive;
            if (signUpTabButton != null && signUpTabButton.image != null)
                signUpTabButton.image.color = !login ? TabActive : TabInactive;
            HideErrors();
        }

        // ── Login ─────────────────────────────────────────────────────────────

        private void OnLogin()
        {
            string email = loginEmailField != null ? loginEmailField.text.Trim() : "";
            string pass  = loginPasswordField != null ? loginPasswordField.text : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                ShowLoginError("Please enter your email and password.");
                return;
            }

            SetInteractable(false);
            NetworkBootstrapper.Instance.Auth.LoginWithEmail(email, pass,
                onSuccess: OnAuthSuccess,
                onFailed:  err => { ShowLoginError(err); SetInteractable(true); });
        }

        // ── Sign Up ───────────────────────────────────────────────────────────

        private void OnSignUp()
        {
            string email   = signUpEmailField   != null ? signUpEmailField.text.Trim() : "";
            string pass    = signUpPasswordField != null ? signUpPasswordField.text    : "";
            string confirm = signUpConfirmField  != null ? signUpConfirmField.text     : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                ShowSignUpError("Please fill in all fields.");
                return;
            }
            if (pass.Length < 6)
            {
                ShowSignUpError("Password must be at least 6 characters.");
                return;
            }
            if (pass != confirm)
            {
                ShowSignUpError("Passwords do not match.");
                return;
            }

            SetInteractable(false);
            string username = email.Split('@')[0];
            NetworkBootstrapper.Instance.Auth.Register(email, pass, username,
                onSuccess: OnAuthSuccess,
                onFailed:  err => { ShowSignUpError(err); SetInteractable(true); });
        }

        // ── Guest ─────────────────────────────────────────────────────────────

        private void OnGuest()
        {
            SetInteractable(false);
            NetworkBootstrapper.Instance.Auth.LoginAsGuest(
                onSuccess: OnAuthSuccess,
                onFailed:  err => { ShowLoginError(err); SetInteractable(true); });
        }

        // ── Post-auth loading ─────────────────────────────────────────────────

        private void OnAuthSuccess()
        {
            authPanelGroup.gameObject.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (loadingText  != null) loadingText.text = "Syncing your data...";

            NetworkBootstrapper.Instance.Wallet.OnDataLoaded += () =>
                SceneManager.LoadScene(mainMenuScene);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetInteractable(bool on)
        {
            if (loginButton  != null) loginButton.interactable  = on;
            if (signUpButton != null) signUpButton.interactable = on;
            if (guestButton  != null) guestButton.interactable  = on;
        }

        private void ShowLoginError(string msg)
        {
            if (loginErrorText == null) return;
            loginErrorText.text = msg;
            loginErrorText.gameObject.SetActive(true);
        }

        private void ShowSignUpError(string msg)
        {
            if (signUpErrorText == null) return;
            signUpErrorText.text = msg;
            signUpErrorText.gameObject.SetActive(true);
        }

        private void HideErrors()
        {
            if (loginErrorText  != null) loginErrorText.gameObject.SetActive(false);
            if (signUpErrorText != null) signUpErrorText.gameObject.SetActive(false);
        }
    }
}
