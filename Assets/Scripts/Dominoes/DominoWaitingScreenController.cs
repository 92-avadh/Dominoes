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
            // Ensure we're not parented to a disabled object (e.g., Canvas)
            // which would prevent coroutines from running
            if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy)
            {
                transform.SetParent(null, false);
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            ValidateReferences();
            matchManager ??= new DominoMatchManager(waitingCountdownDuration);
            matchManager.Initialize();

            // Isolate legacy uGUI GraphicRaycaster so it cannot intercept UI Toolkit touch events
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = false;
            }
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
        private void ValidateReferences()
        {
            // UI Toolkit is the primary modern presentation layer; uGUI fields are optional legacy fallback
            if (waitingScreen == null)
            {
                Debug.Log("[DominoWaitingScreenController] Pure UI Toolkit mode active (uGUI WaitingScreen omitted).", this);
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
            // Ensure this GameObject is active and not parented to a disabled object
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy)
            {
                transform.SetParent(null, false);
            }

            if (homeScreen != null) homeScreen.SetActive(false);
            if (waitingScreen != null) waitingScreen.SetActive(true);

            StartWaitingFlow();
        }

        /// <summary>
        /// Starts the waiting state based on the globally active DominoGameModeContext.
        /// </summary>
        public void StartWaitingFlow()
        {
            EnsureActive();

            if (DominoGameModeContext.CurrentMode == GameModeType.VsComputer)
            {
                StartDirectVsComputerMatch(DominoGameModeContext.Difficulty);
                return;
            }
            else if (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom)
            {
                StartFriendRoomFlow(DominoGameModeContext.RoomCode, DominoGameModeContext.IsHost);
                return;
            }
            else
            {
                StartOnlineMatchmakingFlow(DominoGameModeContext.OnlineRule);
                return;
            }
        }

        /// <summary>
        /// Starts a direct 1v1 match against the Computer AI with no waiting countdown.
        /// </summary>
        public void StartDirectVsComputerMatch(ComputerDifficulty difficulty, string humanName = "Player 1 (You)")
        {
            EnsureActive();
            StopBotSimulation();
            matchManager.ResetMatch();

            string botName = $"Computer ({difficulty})";
            var humanPlayer = new DominoPlayer(1, string.IsNullOrEmpty(humanName) ? "Player 1 (You)" : humanName, isHuman: true);
            var botPlayer = new DominoPlayer(2, botName, isHuman: false);

            matchManager.AddPlayer(humanPlayer);
            matchManager.AddPlayer(botPlayer);

            if (waitingScreen != null) waitingScreen.SetActive(false);
            if (homeScreen != null) homeScreen.SetActive(false);

            bool started = matchManager.TryStartMatch(out string err);
            if (!started)
            {
                Debug.LogError($"[DominoWaitingScreenController] Failed to start direct computer match: {err}");
            }
        }

        /// <summary>
        /// Starts a 4-player online matchmaking lobby with international players and a snappy 5s countdown.
        /// </summary>
        public void StartOnlineMatchmakingFlow(string ruleName, string humanName = "Player 1 (You)")
        {
            EnsureActive();
            StopBotSimulation();
            matchManager.ResetMatch();

            float onlineCountdown = 5f;
            matchManager.WaitingManager.CountdownDuration = onlineCountdown;

            var humanPlayer = new DominoPlayer(1, string.IsNullOrEmpty(humanName) ? "Player 1 (You)" : humanName, isHuman: true);
            matchManager.AddPlayer(humanPlayer);
            matchManager.StartWaitingPhase(onlineCountdown);

            UpdatePlayerCountUI(matchManager.WaitingManager.PlayerCount);
            UpdateCountdownUI(onlineCountdown);
            UpdateWaitingMessageUI(matchManager.WaitingManager.PlayerCount);

            botSimulationCoroutine = StartCoroutine(SimulateOnlinePlayersCoroutine());
        }

        /// <summary>
        /// Starts a 2-player private friend room lounge with room code and 4s connection countdown.
        /// </summary>
        public void StartFriendRoomFlow(string roomCode, bool isHost, string humanName = "Player 1 (You)")
        {
            EnsureActive();
            StopBotSimulation();
            matchManager.ResetMatch();

            float friendCountdown = 4f;
            matchManager.WaitingManager.CountdownDuration = friendCountdown;

            string myName = string.IsNullOrEmpty(humanName) ? (isHost ? "You (Host)" : "You (Guest)") : humanName;
            var humanPlayer = new DominoPlayer(1, myName, isHuman: true);
            matchManager.AddPlayer(humanPlayer);
            matchManager.StartWaitingPhase(friendCountdown);

            UpdatePlayerCountUI(matchManager.WaitingManager.PlayerCount);
            UpdateCountdownUI(friendCountdown);
            UpdateWaitingMessageUI(matchManager.WaitingManager.PlayerCount);

            botSimulationCoroutine = StartCoroutine(SimulateFriendJoinCoroutine(isHost));
        }

        private void EnsureActive()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy)
            {
                transform.SetParent(null, false);
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

        private IEnumerator SimulateOnlinePlayersCoroutine()
        {
            string[] onlineOpponents = new[] { "Lucas 🇧🇷", "Aoi 🇯🇵", "Mateo 🇪🇸" };
            for (int i = 0; i < onlineOpponents.Length; i++)
            {
                yield return new WaitForSeconds(1.1f);

                if (matchManager.CurrentState != MatchState.Waiting || !matchManager.WaitingManager.IsWaitingActive)
                {
                    yield break;
                }

                if (matchManager.WaitingManager.PlayerCount >= DominoWaitingManager.MaxPlayers)
                {
                    yield break;
                }

                var botPlayer = new DominoPlayer(i + 2, onlineOpponents[i], isHuman: false);
                matchManager.AddPlayer(botPlayer);
            }
        }

        private IEnumerator SimulateFriendJoinCoroutine(bool isHost)
        {
            yield return new WaitForSeconds(1.6f);

            if (matchManager.CurrentState != MatchState.Waiting || !matchManager.WaitingManager.IsWaitingActive)
            {
                yield break;
            }

            if (matchManager.WaitingManager.PlayerCount >= 2)
            {
                yield break;
            }

            string friendName = isHost ? "Alex (Friend) 🎮" : "Host Room 👑";
            var friendPlayer = new DominoPlayer(2, friendName, isHuman: false);
            matchManager.AddPlayer(friendPlayer);
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
            int max = (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom) ? 2 : DominoWaitingManager.MaxPlayers;
            string formattedCount = $"Players Ready: {count} / {max}";
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
            string message;
            if (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom)
            {
                message = (playerCount < 2)
                    ? $"Waiting for friend to connect using code {DominoGameModeContext.RoomCode}..."
                    : "Friend connected! Starting private match...";
            }
            else
            {
                message = (playerCount < DominoWaitingManager.MinPlayersToStart)
                    ? "Searching for players worldwide..."
                    : "Players found! Ready to start.";
            }

            SetText(waitingMessageText, waitingMessageLegacyText, message);
        }

        private void ResetWaitingUI()
        {
            int max = (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom) ? 2 : DominoWaitingManager.MaxPlayers;
            float duration = (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom) ? 4f : 5f;
            SetText(playerCountText, playerCountLegacyText, $"Players Ready: 0 / {max}");
            SetText(countdownText, countdownLegacyText, $"Starting in {Mathf.CeilToInt(duration)}");
            SetText(waitingMessageText, waitingMessageLegacyText, "Waiting for players...");
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
