using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// UI Toolkit presentation controller for the Dominoes WaitingScreen (WaitingScreen.uxml).
    /// Bridges the UI Toolkit visual elements to the existing DominoMatchManager and DominoWaitingScreenController.
    /// Features dynamic VS arena layout and Image 3 commercial alert confirmation modal.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class DominoWaitingScreenUIToolkitController : MonoBehaviour
    {
        [Header("UI Document")]
        [Tooltip("The UIDocument component hosting WaitingScreen.uxml. If left empty, will be auto-detected on this GameObject.")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Controllers Integration")]
        [Tooltip("Reference to the existing DominoWaitingScreenController. If left empty, will be auto-located in the active scene.")]
        [SerializeField] private DominoWaitingScreenController waitingScreenController;

        [Tooltip("Reference to the UI Toolkit DominoHomeScreenController. If left empty, will be auto-located in the active scene.")]
        [SerializeField] private DominoHomeScreenController homeScreenController;

        [Tooltip("Reference to the UI Toolkit DominoGameScreenUIToolkitController. If left empty, will be auto-located in the active scene.")]
        [SerializeField] private DominoGameScreenUIToolkitController gameScreenUIToolkitController;

        // Visual Elements
        private VisualElement rootElement;
        private VisualElement safeContent;
        private Label countdownLabel;
        private Label waitingMessageLabel;
        private Label playerCountLabel;
        private Label waitingTitleLabel;
        private Label waitingSubtitleLabel;
        private Button leaveButton;

        // Leave Confirmation Modal (Image 3 Alert Modal)
        private VisualElement leaveConfirmModal;
        private VisualElement leaveModalCard;
        private Button leaveModalCancelBtn;
        private Button leaveModalConfirmBtn;
        private Button alertCloseXBtn;

        private readonly VisualElement[] playerSlots = new VisualElement[4];
        private readonly Label[] playerSlotNames = new Label[4];
        private readonly Label[] playerSlotStatuses = new Label[4];
        private readonly VisualElement[] playerSlotAvatars = new VisualElement[4];

        private bool isLeaveCallbackRegistered;
        private bool isMatchEventsSubscribed;

        public UIDocument UIDocument => uiDocument;
        public DominoWaitingScreenController WaitingScreenController
        {
            get => waitingScreenController;
            set => waitingScreenController = value;
        }

        public DominoHomeScreenController HomeScreenController
        {
            get => homeScreenController;
            set => homeScreenController = value;
        }

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (waitingScreenController == null)
            {
#if UNITY_2023_1_OR_NEWER
                waitingScreenController = FindAnyObjectByType<DominoWaitingScreenController>(FindObjectsInactive.Include);
#else
                waitingScreenController = FindObjectOfType<DominoWaitingScreenController>(true);
#endif
                if (waitingScreenController == null)
                {
                    var matchGO = new GameObject("DominoMatchController");
                    waitingScreenController = matchGO.AddComponent<DominoWaitingScreenController>();
                }
            }

            if (homeScreenController == null)
            {
#if UNITY_2023_1_OR_NEWER
                homeScreenController = FindAnyObjectByType<DominoHomeScreenController>(FindObjectsInactive.Include);
#else
                homeScreenController = FindObjectOfType<DominoHomeScreenController>(true);
#endif
            }

            if (gameScreenUIToolkitController == null)
            {
#if UNITY_2023_1_OR_NEWER
                gameScreenUIToolkitController = FindAnyObjectByType<DominoGameScreenUIToolkitController>(FindObjectsInactive.Include);
#else
                gameScreenUIToolkitController = FindObjectOfType<DominoGameScreenUIToolkitController>(true);
#endif
            }

            // Guarantee that WaitingScreen starts hidden on Awake
            HideWaitingScreen();
        }

        private void OnEnable()
        {
            RegisterUIElements();
            SubscribeMatchEvents();
        }

        private void OnDisable()
        {
            UnregisterUIElements();
            UnsubscribeMatchEvents();
        }

        private void OnDestroy()
        {
            UnregisterUIElements();
            UnsubscribeMatchEvents();
        }

        private void Start()
        {
        }

        /// <summary>
        /// Queries the visual tree for WaitingScreen elements and registers button callbacks.
        /// </summary>
        private void RegisterUIElements()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null) return;
            }

            var panelRoot = uiDocument.rootVisualElement;
            if (panelRoot == null) return;

            rootElement = panelRoot.Q<VisualElement>("waiting-root") ?? panelRoot;

            countdownLabel = rootElement.Q<Label>("countdown-label");
            waitingMessageLabel = rootElement.Q<Label>("waiting-message-label");
            playerCountLabel = rootElement.Q<Label>("player-count-label");
            waitingTitleLabel = rootElement.Q<Label>("waiting-title");
            waitingSubtitleLabel = rootElement.Q<Label>("waiting-subtitle");
            leaveButton = rootElement.Q<Button>("leave-button");

            // Cache player slot rows
            for (int i = 0; i < 4; i++)
            {
                playerSlots[i] = rootElement.Q<VisualElement>($"slot-{i}");
                playerSlotNames[i] = rootElement.Q<Label>($"slot-name-{i}");
                playerSlotStatuses[i] = rootElement.Q<Label>($"slot-status-{i}");

                if (playerSlots[i] != null)
                {
                    playerSlotAvatars[i] = playerSlots[i].Q<VisualElement>($"slot-avatar-{i}") ?? playerSlots[i].Q<VisualElement>(className: "vs-avatar-img");
                }
            }

            // Leave Confirmation Modal (Matching Image 3)
            leaveConfirmModal = rootElement.Q<VisualElement>("leave-confirm-modal");
            leaveModalCard = rootElement.Q<VisualElement>("leave-modal-card");
            leaveModalCancelBtn = rootElement.Q<Button>("leave-modal-cancel-btn");
            leaveModalConfirmBtn = rootElement.Q<Button>("leave-modal-confirm-btn");
            alertCloseXBtn = rootElement.Q<Button>("alert-close-x-btn");

            safeContent = rootElement.Q<VisualElement>("safe-content") ?? rootElement;
            rootElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            SetupButton(leaveModalCancelBtn, HideLeaveConfirmation);
            SetupButton(leaveModalConfirmBtn, OnConfirmLeaveClicked);
            SetupButton(alertCloseXBtn, HideLeaveConfirmation);
            SetupButton(leaveButton, OnLeaveButtonClicked);

            ApplySafeArea();
        }

        private void SetupButton(Button btn, System.Action onClick)
        {
            if (btn == null || onClick == null) return;
            btn.clicked -= onClick;
            btn.clicked += () =>
            {
                DominoAudioManager.Instance?.PlayClick();
                onClick();
            };
        }

        // (SetPickingModeRecursive removed — it was the root cause of unresponsive buttons)

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            ApplySafeArea();
        }

        private System.Collections.IEnumerator DeferredApplySafeArea()
        {
            yield return null;
            ApplySafeArea();
            yield return new WaitForSeconds(0.1f);
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (safeContent == null) return;
            DominoSafeAreaHandler.ApplySafeArea(safeContent, baseLeft: 10f, baseRight: 10f, baseTop: 6f, baseBottom: 6f);
        }

        /// <summary>
        /// Unregisters button callbacks and clears element references.
        /// </summary>
        private void UnregisterUIElements()
        {
            if (leaveButton != null && isLeaveCallbackRegistered)
            {
                leaveButton.clicked -= OnLeaveButtonClicked;
                isLeaveCallbackRegistered = false;
            }

            if (leaveModalCancelBtn != null) leaveModalCancelBtn.clicked -= HideLeaveConfirmation;
            if (leaveModalConfirmBtn != null) leaveModalConfirmBtn.clicked -= OnConfirmLeaveClicked;
            if (alertCloseXBtn != null) alertCloseXBtn.clicked -= HideLeaveConfirmation;

            leaveConfirmModal = null;
            leaveModalCard = null;
            leaveModalCancelBtn = null;
            leaveModalConfirmBtn = null;
            alertCloseXBtn = null;

            countdownLabel = null;
            waitingMessageLabel = null;
            playerCountLabel = null;
            waitingTitleLabel = null;
            waitingSubtitleLabel = null;
            leaveButton = null;
            if (rootElement != null)
            {
                rootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            }

            safeContent = null;
            rootElement = null;
        }

        /// <summary>
        /// Subscribes to the underlying match and waiting managers.
        /// </summary>
        private void SubscribeMatchEvents()
        {
            if (waitingScreenController == null || isMatchEventsSubscribed) return;

            var matchManager = waitingScreenController.MatchManager;
            if (matchManager != null)
            {
                matchManager.OnMatchStarted += HandleMatchStarted;
                matchManager.OnMatchStateChanged += HandleMatchStateChanged;

                if (matchManager.WaitingManager != null)
                {
                    matchManager.WaitingManager.OnPlayerCountChanged += HandlePlayerCountChanged;
                    matchManager.WaitingManager.OnCountdownTick += HandleCountdownTick;
                    matchManager.WaitingManager.OnCountdownExpired += HandleCountdownExpired;
                }

                isMatchEventsSubscribed = true;
            }
        }

        /// <summary>
        /// Unsubscribes from the underlying match and waiting managers.
        /// </summary>
        private void UnsubscribeMatchEvents()
        {
            if (waitingScreenController == null || !isMatchEventsSubscribed) return;

            var matchManager = waitingScreenController.MatchManager;
            if (matchManager != null)
            {
                matchManager.OnMatchStarted -= HandleMatchStarted;
                matchManager.OnMatchStateChanged -= HandleMatchStateChanged;

                if (matchManager.WaitingManager != null)
                {
                    matchManager.WaitingManager.OnPlayerCountChanged -= HandlePlayerCountChanged;
                    matchManager.WaitingManager.OnCountdownTick -= HandleCountdownTick;
                    matchManager.WaitingManager.OnCountdownExpired -= HandleCountdownExpired;
                }
            }

            isMatchEventsSubscribed = false;
        }

        /// <summary>
        /// Handles click on the Leave/Cancel Match button. Shows animated confirmation AlertDialog.
        /// </summary>
        private void OnLeaveButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            ShowLeaveConfirmation();
        }

        public void ShowLeaveConfirmation()
        {
            if (leaveConfirmModal != null)
            {
                leaveConfirmModal.style.display = DisplayStyle.Flex;
            }
        }

        public void HideLeaveConfirmation()
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            if (leaveConfirmModal != null)
            {
                leaveConfirmModal.style.display = DisplayStyle.None;
            }
        }

        private void OnConfirmLeaveClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            HideLeaveConfirmation();
            ExecuteLeave();
        }

        private void ExecuteLeave()
        {
            Debug.Log("<color=yellow>[DominoWaitingScreenUIToolkitController] Leave confirmed. Cancelling match...</color>");

            // 1. Tell existing waiting controller / managers to reset
            if (waitingScreenController != null)
            {
                waitingScreenController.OnLeaveWaitingClicked();
            }

            // 2. Hide WaitingScreen
            HideWaitingScreen();

            // 3. Restore HomeScreen
            if (homeScreenController != null)
            {
                homeScreenController.ShowHomeScreen();
            }
        }

        private void HandlePlayerCountChanged(int count)
        {
            UpdatePlayerCountUI(count);
            UpdateWaitingMessageUI(count);
            RefreshPlayerSlots();
        }

        private void HandleCountdownTick(float remainingTime)
        {
            UpdateCountdownUI(remainingTime);
        }

        private void HandleCountdownExpired()
        {
            if (waitingScreenController != null && waitingScreenController.MatchManager != null)
            {
                int currentCount = waitingScreenController.MatchManager.WaitingManager.PlayerCount;
                if (currentCount < DominoWaitingManager.MinPlayersToStart)
                {
                    SetLabelText(countdownLabel, "Waiting for players...");
                    SetLabelText(waitingMessageLabel, "Waiting for more players...");
                }
            }
        }

        private void HandleMatchStarted()
        {
            Debug.Log("<color=green>[DominoWaitingScreenUIToolkitController] Match successfully started! Hiding WaitingScreen and showing GameScreen.</color>");
            DominoAudioManager.Instance?.PlayTurnChime();
            HideWaitingScreen();

            if (gameScreenUIToolkitController == null)
            {
#if UNITY_2023_1_OR_NEWER
                gameScreenUIToolkitController = FindAnyObjectByType<DominoGameScreenUIToolkitController>(FindObjectsInactive.Include);
#else
                gameScreenUIToolkitController = FindObjectOfType<DominoGameScreenUIToolkitController>(true);
#endif
            }

            if (gameScreenUIToolkitController != null)
            {
                gameScreenUIToolkitController.ShowGameScreen();
            }
        }

        private void HandleMatchStateChanged(MatchState newState)
        {
            if (newState == MatchState.Waiting)
            {
                ShowWaitingScreen();
                ResetWaitingUI();
            }
            else
            {
                HideWaitingScreen();
            }
        }

        /// <summary>
        /// Updates the lobby title and subtitle to reflect the active game mode.
        /// </summary>
        public void RefreshLobbyHeader()
        {
            if (waitingTitleLabel != null)
            {
                waitingTitleLabel.text = DominoGameModeContext.GetLobbyTitle();
            }

            if (waitingSubtitleLabel != null)
            {
                waitingSubtitleLabel.text = DominoGameModeContext.GetLobbySubtitle();
            }
        }

        /// <summary>
        /// Updates the 'Players Ready: X / N' label.
        /// </summary>
        public void UpdatePlayerCountUI(int count)
        {
            int max = (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom) ? 2 : DominoWaitingManager.MaxPlayers;
            SetLabelText(playerCountLabel, $"Players Ready: {count} / {max}");
        }

        /// <summary>
        /// Updates the countdown label with formatted seconds.
        /// </summary>
        public void UpdateCountdownUI(float remainingTime)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
            SetLabelText(countdownLabel, $"Starting in {seconds}");
        }

        /// <summary>
        /// Updates the waiting status message according to game mode.
        /// </summary>
        public void UpdateWaitingMessageUI(int playerCount)
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

            SetLabelText(waitingMessageLabel, message);
        }

        /// <summary>
        /// Synchronizes the player slots with active players in the match manager.
        /// In Friend mode, slots 2 and 3 are hidden to present a clean 1v1 lounge.
        /// </summary>
        public void RefreshPlayerSlots()
        {
            if (waitingScreenController == null || waitingScreenController.MatchManager == null) return;

            var match = waitingScreenController.MatchManager;
            IReadOnlyList<DominoPlayer> players = (match.WaitingManager != null && match.WaitingManager.WaitingPlayers.Count > 0)
                ? match.WaitingManager.WaitingPlayers
                : match.GameState.Players;

            int activeCount = players != null ? players.Count : 0;
            bool isFriendMode = (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom);

            for (int i = 0; i < 4; i++)
            {
                var slotElement = playerSlots[i];
                var nameLabel = playerSlotNames[i];
                var statusLabel = playerSlotStatuses[i];
                var avatarElement = playerSlotAvatars[i];

                if (slotElement == null) continue;

                // In Friend Mode, slots 2 and 3 are hidden for 1v1 match
                if (isFriendMode && (i == 2 || i == 3))
                {
                    slotElement.style.display = DisplayStyle.None;
                    continue;
                }
                else
                {
                    slotElement.style.display = DisplayStyle.Flex;
                }

                if (i < activeCount && players != null)
                {
                    var player = players[i];
                    slotElement.RemoveFromClassList("vs-avatar-slot--searching");

                    string displayName = !string.IsNullOrEmpty(player.PlayerName)
                        ? player.PlayerName
                        : (player.IsHuman ? "Player 1 (You)" : $"Opponent {player.Id}");

                    if (nameLabel != null) nameLabel.text = displayName;
                    if (statusLabel != null)
                    {
                        statusLabel.text = "READY";
                        statusLabel.AddToClassList("vs-status-pill--ready");
                        statusLabel.RemoveFromClassList("vs-status-pill--searching");
                    }

                    if (avatarElement != null)
                    {
                        avatarElement.style.opacity = 1f;
                    }
                }
                else
                {
                    slotElement.AddToClassList("vs-avatar-slot--searching");

                    string searchingText = (isFriendMode && i == 1) ? "Waiting for friend..." : "Searching...";
                    if (nameLabel != null) nameLabel.text = searchingText;
                    if (statusLabel != null)
                    {
                        statusLabel.text = "SEARCHING";
                        statusLabel.RemoveFromClassList("vs-status-pill--ready");
                        statusLabel.AddToClassList("vs-status-pill--searching");
                    }

                    if (avatarElement != null)
                    {
                        avatarElement.style.opacity = 0.45f;
                    }
                }
            }
        }

        /// <summary>
        /// Resets the UI elements to mode-specific waiting state.
        /// </summary>
        public void ResetWaitingUI()
        {
            RefreshLobbyHeader();

            float duration = waitingScreenController != null ? waitingScreenController.WaitingCountdownDuration : 5f;
            int count = (waitingScreenController != null && waitingScreenController.MatchManager != null && waitingScreenController.MatchManager.WaitingManager != null)
                ? waitingScreenController.MatchManager.WaitingManager.PlayerCount
                : 1;

            int max = (DominoGameModeContext.CurrentMode == GameModeType.FriendRoom) ? 2 : DominoWaitingManager.MaxPlayers;
            SetLabelText(playerCountLabel, $"Players Ready: {count} / {max}");
            SetLabelText(countdownLabel, $"Starting in {Mathf.CeilToInt(duration)}");
            UpdateWaitingMessageUI(count);

            RefreshPlayerSlots();
        }

        private Coroutine screenFadeCoroutine;

        /// <summary>
        /// Displays the UI Toolkit WaitingScreen and ensures HomeScreen is hidden.
        /// </summary>
        public void ShowWaitingScreen(bool animate = true)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                uiDocument.sortingOrder = 10;
                if (uiDocument.rootVisualElement != null)
                {
                    uiDocument.rootVisualElement.pickingMode = PickingMode.Position;
                }
            }

            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                RegisterUIElements();
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.Flex;
                rootElement.pickingMode = PickingMode.Position;
                ApplySafeArea();
                StartCoroutine(DeferredApplySafeArea());

                if (animate)
                {
                    if (screenFadeCoroutine != null) StopCoroutine(screenFadeCoroutine);
                    screenFadeCoroutine = StartCoroutine(FadeInScreen(rootElement));
                }
                else
                {
                    rootElement.style.opacity = 1f;
                }
            }

            // Ensure HomeScreen is hidden when WaitingScreen appears
            if (homeScreenController != null)
            {
                homeScreenController.HideHomeScreen(animate);
            }

            RefreshPlayerSlots();
        }

        /// <summary>
        /// Hides the UI Toolkit WaitingScreen.
        /// </summary>
        public void HideWaitingScreen(bool animate = true, System.Action onComplete = null)
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                uiDocument.sortingOrder = 0;
                if (uiDocument.rootVisualElement != null)
                {
                    uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
                }
            }

            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("waiting-root") ?? uiDocument.rootVisualElement;
            }

            if (leaveConfirmModal != null)
            {
                leaveConfirmModal.style.display = DisplayStyle.None;
            }

            if (rootElement != null)
            {
                if (animate)
                {
                    if (screenFadeCoroutine != null) StopCoroutine(screenFadeCoroutine);
                    screenFadeCoroutine = StartCoroutine(FadeOutScreen(rootElement, onComplete));
                }
                else
                {
                    rootElement.style.opacity = 0f;
                    rootElement.style.display = DisplayStyle.None;
                    rootElement.pickingMode = PickingMode.Ignore;
                    onComplete?.Invoke();
                }
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator FadeInScreen(VisualElement element, float duration = 0.35f)
        {
            if (element == null) yield break;
            element.style.display = DisplayStyle.Flex;
            element.style.opacity = 0f;
            element.pickingMode = PickingMode.Position;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                element.style.opacity = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }
            element.style.opacity = 1f;
            screenFadeCoroutine = null;
        }

        private IEnumerator FadeOutScreen(VisualElement element, System.Action onComplete = null, float duration = 0.35f)
        {
            if (element == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                element.style.opacity = Mathf.SmoothStep(1f, 0f, t);
                yield return null;
            }

            element.style.opacity = 0f;
            element.style.display = DisplayStyle.None;
            element.pickingMode = PickingMode.Ignore;
            screenFadeCoroutine = null;
            onComplete?.Invoke();
        }

        private void SetLabelText(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
