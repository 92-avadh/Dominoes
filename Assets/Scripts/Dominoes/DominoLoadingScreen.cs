using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Controls the initial Dominoes loading screen with smooth progress animation,
    /// contextual status messages, and transitions to the HomeScreen.
    /// </summary>
    public class DominoLoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The LoadingScreen GameObject to display initially.")]
        [SerializeField] private GameObject loadingScreen;

        [Tooltip("The HomeScreen GameObject to display after loading completes.")]
        [SerializeField] private GameObject homeScreen;

        [Header("Settings")]
        [Tooltip("Duration in seconds to display the loading screen before transitioning to the HomeScreen.")]
        [SerializeField] private float loadingDuration = 2.0f;

        private VisualElement progressBar;
        private Label statusLabel;
        private Label percentLabel;

        private readonly string[] statusMessages = new string[]
        {
            "Preparing your table...",
            "Finding your opponents...",
            "Shuffling dominoes...",
            "Ready to play!"
        };

        private void Start()
        {
            if (loadingScreen != null)
            {
                var uiDoc = loadingScreen.GetComponent<UIDocument>();
                if (uiDoc != null && uiDoc.rootVisualElement != null)
                {
                    progressBar = uiDoc.rootVisualElement.Q<VisualElement>("loading-progress-bar");
                    statusLabel = uiDoc.rootVisualElement.Q<Label>("loading-status-text");
                    percentLabel = uiDoc.rootVisualElement.Q<Label>("loading-percent-text");
                }
            }

            if (loadingScreen == null || homeScreen == null)
            {
                if (loadingScreen == null)
                {
                    Debug.LogError("[DominoLoadingScreen] LoadingScreen reference is missing!", this);
                }

                if (homeScreen == null)
                {
                    Debug.LogError("[DominoLoadingScreen] HomeScreen reference is missing!", this);
                }

                return;
            }

            StartCoroutine(LoadingSequence());
        }

        private IEnumerator LoadingSequence()
        {
            loadingScreen.SetActive(true);
            homeScreen.SetActive(false);

            float elapsed = 0f;
            while (elapsed < loadingDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / loadingDuration);
                int percent = Mathf.RoundToInt(progress * 100f);

                if (progressBar != null)
                {
                    progressBar.style.width = Length.Percent(percent);
                }

                if (percentLabel != null)
                {
                    percentLabel.text = $"{percent}%";
                }

                if (statusLabel != null)
                {
                    int msgIdx = Mathf.Clamp((int)(progress * statusMessages.Length), 0, statusMessages.Length - 1);
                    statusLabel.text = statusMessages[msgIdx];
                }

                yield return null;
            }

            if (progressBar != null) progressBar.style.width = Length.Percent(100);
            if (percentLabel != null) percentLabel.text = "100%";

            yield return new WaitForSeconds(0.25f);

            loadingScreen.SetActive(false);
            homeScreen.SetActive(true);
        }
    }
}
