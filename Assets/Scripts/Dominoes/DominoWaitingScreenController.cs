using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Dominoes
{
    /// <summary>
    /// Coordinates the transition between HomeScreen and WaitingScreen, connects UI elements to the existing
    /// DominoMatchManager and DominoWaitingManager, updates player count and countdown dynamically, and simulates bot joins.
    /// </summary>
    public class DominoWaitingScreenController : MonoBehaviour
    {
        [Header("UI Screens")]
        [Tooltip("The HomeScreen GameObject to show/hide.")]
        [SerializeField] private GameObject homeScreen;

        [Tooltip("The WaitingScreen GameObject to show/hide.")]
        [SerializeField] private GameObject waitingScreen;

        [Header("Buttons")]
        [Tooltip("Button on HomeScreen to start waiting/matchmaking.")]
        [SerializeField] private Button playDominoesButton;

        [Tooltip("Button on WaitingScreen to cancel waiting and return to HomeScreen.")]
        [SerializeField] private Button leaveWaitingButton;

        [Header("Waiting UI - TextMeshPro")]
        [Tooltip("TextMeshPro display for 'Players Ready: X / 4'.")]
        [SerializeField] private TMP_Text playerCountText;

        [Tooltip("TextMeshPro display for 'Starting in 15'.")]
        [SerializeField] private TMP_Text countdownText;

        [Tooltip("TextMeshPro display for 'Waiting for more players...' or 'Players found!'.")]
        [SerializeField] private TMP_Text waitingMessageText;

        [Tooltip("Optional TextMeshPro display for the Waiting Screen Title.")]
        [SerializeField] private TMP_Text waitingTitleText;

        [Header("Waiting UI - Legacy Text (Fallback)")]
        [Tooltip("Legacy UI Text display for player count (if not using TextMeshPro).")]
        [SerializeField] private Text playerCountLegacyText;

        [Tooltip("Legacy UI Text display for countdown (if not using TextMeshPro).")]
        [SerializeField] private Text countdownLegacyText;

        [Tooltip("Legacy UI Text display for waiting message (if not using TextMeshPro).")]
        [SerializeField] private Text waitingMessageLegacyText;

        [Tooltip("Optional Legacy UI Text display for title.")]
        [SerializeField] private Text waitingTitleLegacyText;

        [Header("Waiting & Countdown Settings")]
        [Tooltip("Configurable waiting countdown duration in seconds (default: 15s).")]
        [SerializeField] private float waitingCountdownDuration = 15f;

        [Header("Simulated Bot Settings")]
        [Tooltip("If enabled, bot players will simulate joining during the countdown.")]
        [SerializeField] private bool simulateBots = true;

        [Tooltip("Number of bot players to simulate joining (0 to 3). Total match players = 1 human + bots.")]
        [Range(0, 3)]
        [SerializeField] private int simulatedBotsCount = 3;

        [Tooltip("Delay in seconds between simulated bot joins.")]
        [SerializeField] private float botJoinInterval = 2.5f;

        [Header("Match Manager Source of Truth")]
        [SerializeField] private DominoMatchManager matchManager = new DominoMatchManager();

        private Coroutine botSimulationCoroutine;

        /// <summary>
        /// Event fired when the player cancels or leaves the waiting screen.
        /// Allows UI Toolkit or outer controllers to restore their home views.
        /// </summary>
        public event Action OnLeftWaiting;

        public DominoMatchManager MatchManager => matchManager;
        public float WaitingCountdownDuration => waitingCountdownDuration;

        private void Awake()
        {
            ValidateReferences();
            matchManager ??= new DominoMatchManager(waitingCountdownDuration);
            matchManager.Initialize();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            StopBotSimulation();
        }

        private void Start()
        {
            // If waiting screen is active at start, ensure it is hidden unless loading screen is handling it
            if (waitingScreen != null && waitingScreen.activeSelf)
            {
                waitingScreen.SetActive(false);
            }
        }

        private void Update()
        {
            // Drive match manager timer while in waiting state
            if (matchManager != null && matchManager.CurrentState == MatchState.Waiting)
            {
                matchManager.Update(Time.deltaTime);
            }
        }

        /// <summary>
        /// Validates assigned inspector references safely and logs clear warnings/errors.
        /// </summary>
        private void ValidateReferences()
        {
            if (homeScreen == null)
            {
                Debug.LogWarning("[DominoWaitingScreenController] 'HomeScreen' reference is not assigned (optional when using UI Toolkit HomeScreen).", this);
            }

            if (waitingScreen == null)
            {
                Debug.LogError("[DominoWaitingScreenController] 'WaitingScreen' reference is missing! Please assign it in the Inspector.", this);
            }

            if (playDominoesButton == null)
            {
                Debug.LogWarning("[DominoWaitingScreenController] 'PlayDominoesButton' reference is not assigned (optional when using UI Toolkit HomeScreen).", this);
            }

            if (leaveWaitingButton == null)
            {
                Debug.LogError("[DominoWaitingScreenController] 'LeaveWaitingButton' reference is missing! Please assign it in the Inspector.", this);
            }

            if (playerCountText == null && playerCountLegacyText == null)
            {
                Debug.LogWarning("[DominoWaitingScreenController] 'PlayerCountText' is not assigned. Assign either TextMeshPro or Legacy Text.", this);
            }

            if (countdownText == null && countdownLegacyText == null)
            {
                Debug.LogWarning("[DominoWaitingScreenController] 'CountdownText' is not assigned. Assign either TextMeshPro or Legacy Text.", this);
            }

            if (waitingMessageText == null && waitingMessageLegacyText == null)
            {
                Debug.LogWarning("[DominoWaitingScreenController] 'WaitingMessageText' is not assigned. Assign either TextMeshPro or Legacy Text.", this);
            }
        }

        private void SubscribeEvents()
        {
            // Prevent duplicate button listeners
            if (playDominoesButton != null)
            {
                playDominoesButton.onClick.RemoveListener(OnPlayDominoesClicked);
                playDominoesButton.onClick.AddListener(OnPlayDominoesClicked);
            }

            if (leaveWaitingButton != null)
            {
                leaveWaitingButton.onClick.RemoveListener(OnLeaveWaitingClicked);
                leaveWaitingButton.onClick.AddListener(OnLeaveWaitingClicked);
            }

            if (matchManager != null)
            {
                matchManager.OnMatchStarted -= HandleMatchStarted;
                matchManager.OnMatchStarted += HandleMatchStarted;

                matchManager.OnMatchStartFailed -= HandleMatchStartFailed;
                matchManager.OnMatchStartFailed += HandleMatchStartFailed;

                matchManager.OnMatchStateChanged -= HandleMatchStateChanged;
                matchManager.OnMatchStateChanged += HandleMatchStateChanged;

                if (matchManager.WaitingManager != null)
                {
                    matchManager.WaitingManager.OnPlayerCountChanged -= HandlePlayerCountChanged;
                    matchManager.WaitingManager.OnPlayerCountChanged += HandlePlayerCountChanged;

                    matchManager.WaitingManager.OnCountdownTick -= HandleCountdownTick;
                    matchManager.WaitingManager.OnCountdownTick += HandleCountdownTick;

                    matchManager.WaitingManager.OnCountdownExpired -= HandleCountdownExpired;
                    matchManager.WaitingManager.OnCountdownExpired += HandleCountdownExpired;
                }
            }
        }

        private void UnsubscribeEvents()
        {
            if (playDominoesButton != null)
            {
                playDominoesButton.onClick.RemoveListener(OnPlayDominoesClicked);
            }

            if (leaveWaitingButton != null)
            {
                leaveWaitingButton.onClick.RemoveListener(OnLeaveWaitingClicked);
            }

            if (matchManager != null)
            {
                matchManager.OnMatchStarted -= HandleMatchStarted;
                matchManager.OnMatchStartFailed -= HandleMatchStartFailed;
                matchManager.OnMatchStateChanged -= HandleMatchStateChanged;

                if (matchManager.WaitingManager != null)
                {
                    matchManager.WaitingManager.OnPlayerCountChanged -= HandlePlayerCountChanged;
                    matchManager.WaitingManager.OnCountdownTick -= HandleCountdownTick;
                    matchManager.WaitingManager.OnCountdownExpired -= HandleCountdownExpired;
                }
            }
        }

        /// <summary>
        /// Triggered when the user clicks Play Dominoes on HomeScreen.
        /// </summary>
        public void OnPlayDominoesClicked()
        {
            if (homeScreen != null) homeScreen.SetActive(false);
            if (waitingScreen != null) waitingScreen.SetActive(true);

            StartWaitingFlow();
        }

        /// <summary>
        /// Starts the waiting state, resets timer, registers the human player, and starts bot simulation.
        /// </summary>
        public void StartWaitingFlow()
        {
            StopBotSimulation();

            matchManager.ResetMatch();
            matchManager.WaitingManager.CountdownDuration = waitingCountdownDuration;

            // Register the human player (1st player)
            var humanPlayer = new DominoPlayer(1, "Player 1 (You)", isHuman: true);
            matchManager.AddPlayer(humanPlayer);

            // Start waiting countdown timer
            matchManager.StartWaitingPhase(waitingCountdownDuration);

            // Initial UI refresh
            UpdatePlayerCountUI(matchManager.WaitingManager.PlayerCount);
            UpdateCountdownUI(waitingCountdownDuration);
            UpdateWaitingMessageUI(matchManager.WaitingManager.PlayerCount);

            // Start bot joins if configured
            if (simulateBots && simulatedBotsCount > 0)
            {
                botSimulationCoroutine = StartCoroutine(SimulateBotJoinsCoroutine());
            }
        }

        /// <summary>
        /// Triggered when the user clicks Leave on WaitingScreen.
        /// </summary>
        public void OnLeaveWaitingClicked()
        {
            StopBotSimulation();
            matchManager.ResetMatch();

            if (waitingScreen != null) waitingScreen.SetActive(false);
            if (homeScreen != null) homeScreen.SetActive(true);

            ResetWaitingUI();

            OnLeftWaiting?.Invoke();
        }

        private void StopBotSimulation()
        {
            if (botSimulationCoroutine != null)
            {
                StopCoroutine(botSimulationCoroutine);
                botSimulationCoroutine = null;
            }
        }

        private IEnumerator SimulateBotJoinsCoroutine()
        {
            for (int i = 1; i <= simulatedBotsCount; i++)
            {
                yield return new WaitForSeconds(botJoinInterval);

                // Ensure we are still in waiting state and waiting is active
                if (matchManager.CurrentState != MatchState.Waiting || !matchManager.WaitingManager.IsWaitingActive)
                {
                    yield break;
                }

                if (matchManager.WaitingManager.PlayerCount >= DominoWaitingManager.MaxPlayers)
                {
                    yield break;
                }

                int botId = i + 1;
                string[] playerNames = new[] { "Sophia", "Marcus", "Elena" };
                string botName = (i - 1 >= 0 && i - 1 < playerNames.Length) ? playerNames[i - 1] : $"Player {i + 1}";
                var botPlayer = new DominoPlayer(botId, botName, isHuman: false);
                matchManager.AddPlayer(botPlayer);
            }
        }

        private void HandlePlayerCountChanged(int count)
        {
            UpdatePlayerCountUI(count);
            UpdateWaitingMessageUI(count);
        }

        private void HandleCountdownTick(float remainingTime)
        {
            UpdateCountdownUI(remainingTime);
        }

        private void HandleCountdownExpired()
        {
            if (matchManager.WaitingManager.PlayerCount < DominoWaitingManager.MinPlayersToStart)
            {
                // Fewer than 2 players -> Keep waiting, do NOT start game
                SetText(countdownText, countdownLegacyText, "Waiting for players...");
                SetText(waitingMessageText, waitingMessageLegacyText, "Waiting for more players...");
            }
        }

        private void HandleMatchStarted()
        {
            StopBotSimulation();
            Debug.Log($"<color=green>[DominoWaitingScreenController] Match started successfully! Players: {matchManager.GameState.PlayerCount}, Tiles Per Player: {matchManager.TilesPerPlayer}</color>");
            // Game is now in Playing state with dealt hands.
        }

        private void HandleMatchStartFailed(string errorMessage)
        {
            Debug.LogWarning($"[DominoWaitingScreenController] Match start failed: {errorMessage}");
        }

        private void HandleMatchStateChanged(MatchState newState)
        {
            Debug.Log($"[DominoWaitingScreenController] Match state changed to: {newState}");
        }

        private void UpdatePlayerCountUI(int count)
        {
            string formattedCount = $"Players Ready: {count} / {DominoWaitingManager.MaxPlayers}";
            SetText(playerCountText, playerCountLegacyText, formattedCount);
        }

        private void UpdateCountdownUI(float remainingTime)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
            string formattedCountdown = $"Starting in {seconds}";
            SetText(countdownText, countdownLegacyText, formattedCountdown);
        }

        private void UpdateWaitingMessageUI(int playerCount)
        {
            string message = (playerCount < DominoWaitingManager.MinPlayersToStart)
                ? "Waiting for more players..."
                : "Players found!";

            SetText(waitingMessageText, waitingMessageLegacyText, message);
        }

        private void ResetWaitingUI()
        {
            SetText(playerCountText, playerCountLegacyText, $"Players Ready: 0 / {DominoWaitingManager.MaxPlayers}");
            SetText(countdownText, countdownLegacyText, $"Starting in {Mathf.CeilToInt(waitingCountdownDuration)}");
            SetText(waitingMessageText, waitingMessageLegacyText, "Waiting for more players...");
        }

        private void SetText(TMP_Text tmp, Text legacy, string content)
        {
            if (tmp != null)
            {
                tmp.text = content;
            }

            if (legacy != null)
            {
                legacy.text = content;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Simulate Test Case A (1 Player - No Start)")]
        public void EditorTestCaseA()
        {
            simulatedBotsCount = 0;
            OnPlayDominoesClicked();
        }

        [ContextMenu("Simulate Test Case B (2 Players - Start on Expiry)")]
        public void EditorTestCaseB()
        {
            simulatedBotsCount = 1;
            OnPlayDominoesClicked();
        }

        [ContextMenu("Simulate Test Case C (3 Players - Start on Expiry)")]
        public void EditorTestCaseC()
        {
            simulatedBotsCount = 2;
            OnPlayDominoesClicked();
        }

        [ContextMenu("Simulate Test Case D (4 Players - Start on Expiry)")]
        public void EditorTestCaseD()
        {
            simulatedBotsCount = 3;
            OnPlayDominoesClicked();
        }

        [ContextMenu("Simulate Test Case E (Leave Waiting)")]
        public void EditorTestCaseE()
        {
            OnLeaveWaitingClicked();
        }
#endif
    }
}
