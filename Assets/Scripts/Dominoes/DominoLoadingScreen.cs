using System.Collections;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Controls the initial Dominoes loading screen and transitions to the HomeScreen after a configurable duration.
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
        [SerializeField] private float loadingDuration = 2.5f;

        private void Start()
        {
            // Validate references safely
            if (loadingScreen == null || homeScreen == null)
            {
                if (loadingScreen == null)
                {
                    Debug.LogError("[DominoLoadingScreen] LoadingScreen reference is missing! Please assign it in the Unity Inspector.", this);
                }

                if (homeScreen == null)
                {
                    Debug.LogError("[DominoLoadingScreen] HomeScreen reference is missing! Please assign it in the Unity Inspector.", this);
                }

                return;
            }

            StartCoroutine(LoadingSequence());
        }

        private IEnumerator LoadingSequence()
        {
            // Ensure initial UI state: LoadingScreen visible, HomeScreen hidden
            loadingScreen.SetActive(true);
            homeScreen.SetActive(false);

            // Wait for the configured duration
            if (loadingDuration > 0f)
            {
                yield return new WaitForSeconds(loadingDuration);
            }

            // Transition UI: Hide LoadingScreen, Show HomeScreen
            loadingScreen.SetActive(false);
            homeScreen.SetActive(true);
        }
    }
}
