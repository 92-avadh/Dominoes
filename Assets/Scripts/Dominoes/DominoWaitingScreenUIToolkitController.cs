using System;
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

        // Visual Elements
        private VisualElement rootElement;
        private VisualElement safeContent;
        private Label countdownLabel;
        private Label waitingMessageLabel;
        private Label playerCountLabel;
        private Label waitingTitleLabel;
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
            }

            if (homeScreenController == null)
            {
#if UNITY_2023_1_OR_NEWER
                homeScreenController = FindAnyObjectByType<DominoHomeScreenController>(FindObjectsInactive.Include);
#else
                homeScreenController = FindObjectOfType<DominoHomeScreenController>(true);
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

            if (leaveModalCancelBtn != null) leaveModalCancelBtn.clicked += HideLeaveConfirmation;
            if (leaveModalConfirmBtn != null) leaveModalConfirmBtn.clicked += OnConfirmLeaveClicked;
            if (alertCloseXBtn != null) alertCloseXBtn.clicked += HideLeaveConfirmation;

            if (leaveButton != null && !isLeaveCallbackRegistered)
            {
                leaveButton.clicked += OnLeaveButtonClicked;
                isLeaveCallbackRegistered = true;
            }

            ApplySafeArea();
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (safeContent == null) return;

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width;
            float screenH = Screen.height;

            if (screenW <= 0 || screenH <= 0) return;

            float leftPercent = (safeArea.xMin / screenW) * 100f;
            float rightPercent = ((screenW - safeArea.xMax) / screenW) * 100f;
            float topPercent = ((screenH - safeArea.yMax) / screenH) * 100f;
            float bottomPercent = (safeArea.yMin / screenH) * 100f;

            safeContent.style.paddingLeft = Length.Percent(Mathf.Max(3f, leftPercent));
            safeContent.style.paddingRight = Length.Percent(Mathf.Max(3f, rightPercent));
            safeContent.style.paddingTop = Length.Percent(Mathf.Max(3.5f, topPercent));
            safeContent.style.paddingBottom = Length.Percent(Mathf.Max(2.5f, bottomPercent));
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
            Debug.Log("<color=green>[DominoWaitingScreenUIToolkitController] Match successfully started! Hiding WaitingScreen.</color>");
            HideWaitingScreen();
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
        /// Updates the 'Players Ready: X / 4' label.
        /// </summary>
        public void UpdatePlayerCountUI(int count)
        {
            SetLabelText(playerCountLabel, $"Players Ready: {count} / {DominoWaitingManager.MaxPlayers}");
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
        /// Updates the waiting status message.
        /// </summary>
        public void UpdateWaitingMessageUI(int playerCount)
        {
            string message = (playerCount < DominoWaitingManager.MinPlayersToStart)
                ? "Waiting for more players..."
                : "Players found! Ready to start.";

            SetLabelText(waitingMessageLabel, message);
        }

        /// <summary>
        /// Synchronizes the 4 player slots with active players in the match manager.
        /// </summary>
        public void RefreshPlayerSlots()
        {
            if (waitingScreenController == null || waitingScreenController.MatchManager == null) return;

            var match = waitingScreenController.MatchManager;
            IReadOnlyList<DominoPlayer> players = (match.WaitingManager != null && match.WaitingManager.WaitingPlayers.Count > 0)
                ? match.WaitingManager.WaitingPlayers
                : match.GameState.Players;

            int activeCount = players != null ? players.Count : 0;

            for (int i = 0; i < 4; i++)
            {
                var slotElement = playerSlots[i];
                var nameLabel = playerSlotNames[i];
                var statusLabel = playerSlotStatuses[i];
                var avatarElement = playerSlotAvatars[i];

                if (slotElement == null) continue;

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

                    if (nameLabel != null) nameLabel.text = "Searching...";
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
        /// Resets the UI elements to default waiting state.
        /// </summary>
        public void ResetWaitingUI()
        {
            float duration = waitingScreenController != null ? waitingScreenController.WaitingCountdownDuration : 15f;
            int count = (waitingScreenController != null && waitingScreenController.MatchManager != null && waitingScreenController.MatchManager.WaitingManager != null)
                ? waitingScreenController.MatchManager.WaitingManager.PlayerCount
                : 1;

            SetLabelText(playerCountLabel, $"Players Ready: {count} / {DominoWaitingManager.MaxPlayers}");
            SetLabelText(countdownLabel, $"Starting in {Mathf.CeilToInt(duration)}");
            UpdateWaitingMessageUI(count);

            RefreshPlayerSlots();
        }

        /// <summary>
        /// Displays the UI Toolkit WaitingScreen and ensures HomeScreen is hidden.
        /// </summary>
        public void ShowWaitingScreen()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("waiting-root") ?? uiDocument.rootVisualElement;
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.Flex;
            }

            // Ensure HomeScreen is hidden when WaitingScreen appears
            if (homeScreenController != null)
            {
                homeScreenController.HideHomeScreen();
            }

            RefreshPlayerSlots();
        }

        /// <summary>
        /// Hides the UI Toolkit WaitingScreen.
        /// </summary>
        public void HideWaitingScreen()
        {
            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("waiting-root") ?? uiDocument.rootVisualElement;
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.None;
            }

            if (leaveConfirmModal != null)
            {
                leaveConfirmModal.style.display = DisplayStyle.None;
            }
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
