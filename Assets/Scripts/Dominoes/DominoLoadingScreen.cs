using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Controls the initial Dominoes loading screen with smooth progress animation,
    /// contextual status messages, and transitions to the HomeScreen.
    /// Works with UI Toolkit UIDocuments.
    /// </summary>
    public class DominoLoadingScreen : MonoBehaviour
    {
        [Header("UI Document References")]
        [Tooltip("The UIDocument component hosting LoadingScreen.uxml.")]
        [SerializeField] private UIDocument loadingScreenDocument;

        [Tooltip("The UIDocument component hosting HomeScreen.uxml.")]
        [SerializeField] private UIDocument homeScreenDocument;

        [Header("Settings")]
        [Tooltip("Duration in seconds to display the loading screen before transitioning to the HomeScreen.")]
        [SerializeField] private float loadingDuration = 2.0f;

        private VisualElement progressBar;
        private Label statusLabel;
        private Label percentLabel;
        private VisualElement loadingRootElement;

        private readonly string[] statusMessages = new string[]
        {
            "Preparing your table...",
            "Finding your opponents...",
            "Shuffling dominoes...",
            "Ready to play!"
        };

        private void Awake()
        {
            // Auto-find documents if not assigned
            if (loadingScreenDocument == null)
            {
                var loadingGO = GameObject.Find("UI_LoadingScreen_UIToolkit");
                if (loadingGO != null) loadingScreenDocument = loadingGO.GetComponent<UIDocument>();
            }
            
            if (homeScreenDocument == null)
            {
                var homeGO = GameObject.Find("UI_HomeScreen_UIToolkit");
                if (homeGO != null) homeScreenDocument = homeGO.GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            if (loadingScreenDocument == null)
            {
                Debug.LogError("[DominoLoadingScreen] LoadingScreen UIDocument reference is missing!", this);
                // Try to continue anyway
                return;
            }

            if (loadingScreenDocument.rootVisualElement != null)
            {
                loadingRootElement = loadingScreenDocument.rootVisualElement.Q<VisualElement>("loading-root") 
                                    ?? loadingScreenDocument.rootVisualElement;
                
                var safeContent = loadingRootElement.Q<VisualElement>("loading-safe-content") ?? loadingRootElement;
                DominoSafeAreaHandler.ApplySafeArea(safeContent, baseLeft: 24f, baseRight: 24f, baseTop: 32f, baseBottom: 32f);

                loadingRootElement.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    DominoSafeAreaHandler.ApplySafeArea(safeContent, baseLeft: 24f, baseRight: 24f, baseTop: 32f, baseBottom: 32f);
                });

                progressBar = loadingRootElement.Q<VisualElement>("loading-progress-bar");
                statusLabel = loadingRootElement.Q<Label>("loading-status-text");
                percentLabel = loadingRootElement.Q<Label>("loading-percent-text");
            }

            // Hide HomeScreen initially
            if (homeScreenDocument != null && homeScreenDocument.rootVisualElement != null)
            {
                var homeRoot = homeScreenDocument.rootVisualElement.Q<VisualElement>("home-root") 
                              ?? homeScreenDocument.rootVisualElement;
                homeRoot.style.display = DisplayStyle.None;
            }

            StartCoroutine(LoadingSequence());
        }

        private IEnumerator LoadingSequence()
        {
            // Show loading screen
            if (loadingRootElement != null)
            {
                loadingRootElement.style.display = DisplayStyle.Flex;
            }

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

            // Transition to HomeScreen
            if (loadingRootElement != null)
            {
                loadingRootElement.style.display = DisplayStyle.None;
            }

            if (homeScreenDocument != null && homeScreenDocument.rootVisualElement != null)
            {
                var homeRoot = homeScreenDocument.rootVisualElement.Q<VisualElement>("home-root") 
                              ?? homeScreenDocument.rootVisualElement;
                homeRoot.style.display = DisplayStyle.Flex;
                
                // Initialize ResponsiveUIManager if present
                var responsiveManager = FindFirstObjectByType<ResponsiveUIManager>();
                if (responsiveManager != null)
                {
                    responsiveManager.Initialize(homeScreenDocument.rootVisualElement);
                }
            }
            
            // Notify HomeScreenController to show
            var homeController = FindFirstObjectByType<DominoHomeScreenController>();
            if (homeController != null)
            {
                homeController.ShowHomeScreen();
            }
        }
    }
}