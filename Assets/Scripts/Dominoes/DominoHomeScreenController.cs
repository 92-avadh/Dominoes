using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// UI Toolkit controller for the commercial Dominoes HomeScreen (HomeScreen.uxml).
    /// Pure presentation layer managing:
    /// - Header HUD (Left: Menu Button, Center: Profile Avatar/Level, Right: Coins +100)
    /// - 3 Primary Game Modes: ONLINE, VS COMPUTER, FRIEND
    /// - Interactive Sub-Modals:
    ///     * VS Computer: Easy, Medium, Hard difficulty selector
    ///     * Online: Classic, All Fives, Block, Draw rule sets
    ///     * Friend: Create Room (dynamic code generation) & Join Room (code entry)
    /// - Modals (Player Profile, Settings & Audio/Haptics)
    /// - Device Safe-Area responsive padding
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class DominoHomeScreenController : MonoBehaviour
    {
        private const string PrefPlayerName = "Dominoes_PlayerName";

        [Header("UI Document")]
        [Tooltip("The UIDocument component hosting HomeScreen.uxml.")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Controllers Integration")]
        [Tooltip("Reference to the existing DominoWaitingScreenController.")]
        [SerializeField] private DominoWaitingScreenController waitingScreenController;

        [Tooltip("Reference to the UI Toolkit DominoWaitingScreenUIToolkitController.")]
        [SerializeField] private DominoWaitingScreenUIToolkitController waitingScreenUIToolkitController;

        [Tooltip("Reference to the UI Toolkit DominoGameScreenUIToolkitController.")]
        [SerializeField] private DominoGameScreenUIToolkitController gameScreenUIToolkitController;

        private VisualElement rootElement;
        private VisualElement safeContent;

        // Header Elements
        private Button hamburgerButton;
        private Button profileButton;
        private Label profileNameLabel;
        private Label profileLevelText;

        // 3 Main Mode Buttons
        private Button modeOnlineBtn;
        private Button modeComputerBtn;
        private Button modeFriendsBtn;

        // 1. VS Computer Difficulty Modal
        private VisualElement computerDifficultyModal;
        private Button diffEasyBtn;
        private Button diffMediumBtn;
        private Button diffHardBtn;
        private Button computerModalCloseBtn;

        // 2. Online Rules Modal
        private VisualElement onlineRulesModal;
        private Button ruleClassicBtn;
        private Button ruleFivesBtn;
        private Button ruleBlockBtn;
        private Button ruleDrawBtn;
        private Button onlineFindMatchBtn;
        private Button onlineModalCloseBtn;
        private string selectedOnlineRule = "Classic";

        // 3. Friend Room Modal
        private VisualElement friendRoomModal;
        private Button friendTabCreateBtn;
        private Button friendTabJoinBtn;
        private VisualElement friendCreatePanel;
        private VisualElement friendJoinPanel;
        private Label friendGeneratedCode;
        private Button friendCopyCodeBtn;
        private Button friendStartRoomBtn;
        private TextField friendCodeInput;
        private Label friendJoinStatus;
        private Button friendJoinRoomBtn;
        private Button friendModalCloseBtn;
        private string activeRoomCode = "DOM-5829";

        // Profile Modal
        private VisualElement profileStatsModal;
        private Label modalPlayerName;
        private Button profileCloseBtn;

        // Settings Modal
        private VisualElement homeSettingsModal;
        private Button homeSettingsMusicBtn;
        private Label homeSettingsMusicTxt;
        private Button homeSettingsSfxBtn;
        private Label homeSettingsSfxTxt;
        private Button homeSettingsHapticsBtn;
        private Label homeSettingsHapticsTxt;
        private Button homeReplayTutorialBtn;
        private Button homeSettingsCloseBtn;

        // Loading Screen Overlay
        private VisualElement gameLoadingScreen;
        private VisualElement gameLoadingProgressBar;
        private Label gameLoadingStatusText;
        private Label gameLoadingPercentText;
        private static bool hasShownInitialLoading = false;
        private Coroutine loadingCoroutine;

        // Runtime Profile State
        private string playerName = "Player 1";

        public UIDocument UIDocument => uiDocument;
        public DominoWaitingScreenController WaitingScreenController
        {
            get => waitingScreenController;
            set => waitingScreenController = value;
        }

        public DominoWaitingScreenUIToolkitController WaitingScreenUIToolkitController
        {
            get => waitingScreenUIToolkitController;
            set => waitingScreenUIToolkitController = value;
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

            if (waitingScreenUIToolkitController == null)
            {
#if UNITY_2023_1_OR_NEWER
                waitingScreenUIToolkitController = FindAnyObjectByType<DominoWaitingScreenUIToolkitController>(FindObjectsInactive.Include);
#else
                waitingScreenUIToolkitController = FindObjectOfType<DominoWaitingScreenUIToolkitController>(true);
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

            LoadProfile();
        }

        private void OnEnable()
        {
            RegisterUICallbacks();
            SubscribeWaitingEvents();
            ShowHomeScreen();
            RefreshProfileUI();
        }

        private IEnumerator Start()
        {
            if (rootElement == null)
            {
                RegisterUICallbacks();
                SubscribeWaitingEvents();
            }
            ShowHomeScreen();
            RefreshProfileUI();

            yield return null;
            if (rootElement == null)
            {
                RegisterUICallbacks();
                SubscribeWaitingEvents();
                ShowHomeScreen();
                RefreshProfileUI();
            }
        }

        private void OnDisable()
        {
            UnregisterUICallbacks();
            UnsubscribeWaitingEvents();
        }

        private void OnDestroy()
        {
            UnregisterUICallbacks();
            UnsubscribeWaitingEvents();
        }

        private void LoadProfile()
        {
            playerName = PlayerPrefs.GetString(PrefPlayerName, "NewPlayer");
        }

        private void RegisterUICallbacks()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null) return;
            }

            var panelRoot = uiDocument.rootVisualElement;
            if (panelRoot == null) return;

            rootElement = panelRoot.Q<VisualElement>("home-root") ?? panelRoot;
            safeContent = rootElement.Q<VisualElement>("safe-content") ?? rootElement;
            rootElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            // 1. Top Header Elements
            hamburgerButton = rootElement.Q<Button>("hamburger-button");
            profileButton = rootElement.Q<Button>("profile-button");
            profileNameLabel = rootElement.Q<Label>("profile-name-label");
            profileLevelText = rootElement.Q<Label>("profile-level-text");

            SetupButton(hamburgerButton, OnSettingsButtonClicked);
            SetupButton(profileButton, OnProfileButtonClicked);

            // 2. 3 Main Mode Buttons
            modeOnlineBtn = rootElement.Q<Button>("mode-online-btn");
            modeComputerBtn = rootElement.Q<Button>("mode-computer-btn");
            modeFriendsBtn = rootElement.Q<Button>("mode-friends-btn");

            SetupButton(modeOnlineBtn, OnOnlineButtonClicked);
            SetupButton(modeComputerBtn, OnComputerButtonClicked);
            SetupButton(modeFriendsBtn, OnFriendsButtonClicked);

            // 3. Computer Difficulty Modal
            computerDifficultyModal = rootElement.Q<VisualElement>("computer-difficulty-modal");
            diffEasyBtn = rootElement.Q<Button>("diff-easy-btn");
            diffMediumBtn = rootElement.Q<Button>("diff-medium-btn");
            diffHardBtn = rootElement.Q<Button>("diff-hard-btn");
            computerModalCloseBtn = rootElement.Q<Button>("computer-modal-close-btn");

            SetupButton(diffEasyBtn, () => LaunchComputerMatch("Easy"));
            SetupButton(diffMediumBtn, () => LaunchComputerMatch("Medium"));
            SetupButton(diffHardBtn, () => LaunchComputerMatch("Hard"));
            SetupButton(computerModalCloseBtn, HideAllModals);

            // 4. Online Rules Modal
            onlineRulesModal = rootElement.Q<VisualElement>("online-rules-modal");
            ruleClassicBtn = rootElement.Q<Button>("rule-classic-btn");
            ruleFivesBtn = rootElement.Q<Button>("rule-fives-btn");
            ruleBlockBtn = rootElement.Q<Button>("rule-block-btn");
            ruleDrawBtn = rootElement.Q<Button>("rule-draw-btn");
            onlineFindMatchBtn = rootElement.Q<Button>("online-find-match-btn");
            onlineModalCloseBtn = rootElement.Q<Button>("online-modal-close-btn");

            SetupButton(ruleClassicBtn, () => SelectOnlineRule("Classic", ruleClassicBtn));
            SetupButton(ruleFivesBtn, () => SelectOnlineRule("All Fives", ruleFivesBtn));
            SetupButton(ruleBlockBtn, () => SelectOnlineRule("Block", ruleBlockBtn));
            SetupButton(ruleDrawBtn, () => SelectOnlineRule("Draw", ruleDrawBtn));
            SetupButton(onlineFindMatchBtn, LaunchOnlineMatch);
            SetupButton(onlineModalCloseBtn, HideAllModals);

            // 5. Friend Room Modal
            friendRoomModal = rootElement.Q<VisualElement>("friend-room-modal");
            friendTabCreateBtn = rootElement.Q<Button>("friend-tab-create-btn");
            friendTabJoinBtn = rootElement.Q<Button>("friend-tab-join-btn");
            friendCreatePanel = rootElement.Q<VisualElement>("friend-create-panel");
            friendJoinPanel = rootElement.Q<VisualElement>("friend-join-panel");
            friendGeneratedCode = rootElement.Q<Label>("friend-generated-code");
            friendCopyCodeBtn = rootElement.Q<Button>("friend-copy-code-btn");
            friendStartRoomBtn = rootElement.Q<Button>("friend-start-room-btn");
            friendCodeInput = rootElement.Q<TextField>("friend-code-input");
            friendJoinStatus = rootElement.Q<Label>("friend-join-status");
            friendJoinRoomBtn = rootElement.Q<Button>("friend-join-room-btn");
            friendModalCloseBtn = rootElement.Q<Button>("friend-modal-close-btn");

            SetupButton(friendTabCreateBtn, () => SwitchFriendTab(true));
            SetupButton(friendTabJoinBtn, () => SwitchFriendTab(false));
            SetupButton(friendCopyCodeBtn, OnCopyRoomCodeClicked);
            SetupButton(friendStartRoomBtn, OnStartFriendMatchAsHost);
            SetupButton(friendJoinRoomBtn, OnJoinFriendMatch);
            SetupButton(friendModalCloseBtn, HideAllModals);

            // 6. Profile Modal
            profileStatsModal = rootElement.Q<VisualElement>("profile-stats-modal");
            modalPlayerName = rootElement.Q<Label>("modal-player-name");
            profileCloseBtn = rootElement.Q<Button>("profile-close-btn");
            SetupButton(profileCloseBtn, HideAllModals);

            // 7. Settings Modal
            homeSettingsModal = rootElement.Q<VisualElement>("home-settings-modal");
            homeSettingsMusicBtn = rootElement.Q<Button>("home-settings-music-btn");
            homeSettingsMusicTxt = rootElement.Q<Label>("home-settings-music-txt");
            homeSettingsSfxBtn = rootElement.Q<Button>("home-settings-sfx-btn");
            homeSettingsSfxTxt = rootElement.Q<Label>("home-settings-sfx-txt");
            homeSettingsHapticsBtn = rootElement.Q<Button>("home-settings-haptics-btn");
            homeSettingsHapticsTxt = rootElement.Q<Label>("home-settings-haptics-txt");
            homeReplayTutorialBtn = rootElement.Q<Button>("home-replay-tutorial-btn");
            homeSettingsCloseBtn = rootElement.Q<Button>("home-settings-close-btn");

            SetupButton(homeSettingsMusicBtn, OnToggleMusicClicked);
            SetupButton(homeSettingsSfxBtn, OnToggleSfxClicked);
            SetupButton(homeSettingsHapticsBtn, OnToggleHapticsClicked);
            SetupButton(homeReplayTutorialBtn, OnReplayTutorialClicked);
            SetupButton(homeSettingsCloseBtn, HideAllModals);

            // 6. Dismiss any active modal when clicking its dark backdrop
            rootElement.Query<VisualElement>(className: "modal-backdrop").ForEach(backdrop =>
            {
                backdrop.RegisterCallback<ClickEvent>(_ => HideAllModals());
            });

            // 6. First-time / App Launch Loading Screen
            gameLoadingScreen = rootElement.Q<VisualElement>("game-loading-screen");
            gameLoadingProgressBar = rootElement.Q<VisualElement>("game-loading-progress-bar");
            gameLoadingStatusText = rootElement.Q<Label>("game-loading-status-text");
            gameLoadingPercentText = rootElement.Q<Label>("game-loading-percent-text");

            if (gameLoadingScreen != null)
            {
                if (hasShownInitialLoading)
                {
                    gameLoadingScreen.style.display = DisplayStyle.None;
                }
                else
                {
                    gameLoadingScreen.style.display = DisplayStyle.Flex;
                    gameLoadingScreen.style.opacity = 1f;
                    if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
                    loadingCoroutine = StartCoroutine(RunFirstTimeLoadingSequence());
                }
            }

            ApplySafeArea();
            if (rootElement != null)
            {
                rootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                rootElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            }
            RefreshProfileUI();
        }

        private void SetupButton(Button btn, Action onClick)
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

        private IEnumerator DeferredApplySafeArea()
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

        private void UnregisterUICallbacks()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
            gameLoadingScreen = null;
            gameLoadingProgressBar = null;
            gameLoadingStatusText = null;
            gameLoadingPercentText = null;

            if (rootElement != null)
            {
                rootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            }

            safeContent = null;
            rootElement = null;
        }

        private void SubscribeWaitingEvents()
        {
            if (waitingScreenController != null)
            {
                waitingScreenController.OnLeftWaiting -= HandleLeftWaiting;
                waitingScreenController.OnLeftWaiting += HandleLeftWaiting;
            }
        }

        private void UnsubscribeWaitingEvents()
        {
            if (waitingScreenController != null)
            {
                waitingScreenController.OnLeftWaiting -= HandleLeftWaiting;
            }
        }

        public void RefreshProfileUI()
        {
            if (profileNameLabel != null) profileNameLabel.text = playerName;
            if (modalPlayerName != null) modalPlayerName.text = playerName;

            var avatarImg = rootElement?.Q<VisualElement>(className: "hud-avatar-img");
            if (avatarImg != null)
            {
                var avatarTex = Resources.Load<Texture2D>("Textures/Avatars/avatar_you");
                if (avatarTex != null)
                {
                    avatarImg.style.backgroundImage = new StyleBackground(avatarTex);
                    avatarImg.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
                }
            }
        }

        #region Game Mode Triggers & Modals

        private void OnComputerButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            HideAllModals();
            if (computerDifficultyModal != null)
            {
                computerDifficultyModal.style.display = DisplayStyle.Flex;
            }
        }

        private void OnOnlineButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            HideAllModals();
            if (onlineRulesModal != null)
            {
                onlineRulesModal.style.display = DisplayStyle.Flex;
                SelectOnlineRule(selectedOnlineRule, ruleClassicBtn);
            }
        }

        private void OnFriendsButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            HideAllModals();
            GenerateNewRoomCode();
            SwitchFriendTab(true);
            if (friendRoomModal != null)
            {
                friendRoomModal.style.display = DisplayStyle.Flex;
            }
        }

        private void LaunchComputerMatch(string difficulty)
        {
            Debug.Log($"<color=green>[DominoHomeScreenController] Launching VS Computer Match (Difficulty: {difficulty})...</color>");
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerMediumPulse();
            HideAllModals();

            DominoGameModeContext.CurrentMode = GameModeType.VsComputer;
            if (System.Enum.TryParse<ComputerDifficulty>(difficulty, true, out var parsedDiff))
            {
                DominoGameModeContext.Difficulty = parsedDiff;
            }
            else
            {
                DominoGameModeContext.Difficulty = ComputerDifficulty.Medium;
            }

            HideHomeScreen();
            EnsureWaitingControllers();

            if (waitingScreenController != null)
            {
                waitingScreenController.StartDirectVsComputerMatch(DominoGameModeContext.Difficulty, playerName);
            }
        }

        private void SelectOnlineRule(string ruleName, Button targetBtn)
        {
            selectedOnlineRule = ruleName;
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();

            Button[] ruleBtns = { ruleClassicBtn, ruleFivesBtn, ruleBlockBtn, ruleDrawBtn };
            foreach (var btn in ruleBtns)
            {
                if (btn != null) btn.RemoveFromClassList("rule-card--active");
            }

            if (targetBtn != null)
            {
                targetBtn.AddToClassList("rule-card--active");
            }
        }

        private void LaunchOnlineMatch()
        {
            Debug.Log($"<color=cyan>[DominoHomeScreenController] Launching Online Match (Rule: {selectedOnlineRule})...</color>");
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerMediumPulse();
            HideAllModals();

            DominoGameModeContext.CurrentMode = GameModeType.OnlineMatchmaking;
            DominoGameModeContext.OnlineRule = selectedOnlineRule;

            HideHomeScreen();
            EnsureWaitingControllers();

            if (waitingScreenUIToolkitController != null)
            {
                waitingScreenUIToolkitController.ShowWaitingScreen();
            }

            if (waitingScreenController != null)
            {
                waitingScreenController.StartOnlineMatchmakingFlow(selectedOnlineRule, playerName);
            }
        }

        private void SwitchFriendTab(bool isCreate)
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();

            if (friendTabCreateBtn != null)
            {
                if (isCreate) friendTabCreateBtn.AddToClassList("friend-tab-btn--active");
                else friendTabCreateBtn.RemoveFromClassList("friend-tab-btn--active");
            }

            if (friendTabJoinBtn != null)
            {
                if (!isCreate) friendTabJoinBtn.AddToClassList("friend-tab-btn--active");
                else friendTabJoinBtn.RemoveFromClassList("friend-tab-btn--active");
            }

            if (friendCreatePanel != null) friendCreatePanel.style.display = isCreate ? DisplayStyle.Flex : DisplayStyle.None;
            if (friendJoinPanel != null) friendJoinPanel.style.display = !isCreate ? DisplayStyle.Flex : DisplayStyle.None;
            if (friendJoinStatus != null) friendJoinStatus.text = "";
        }

        private void GenerateNewRoomCode()
        {
            int randCode = UnityEngine.Random.Range(1000, 9999);
            activeRoomCode = $"DOM-{randCode}";
            if (friendGeneratedCode != null)
            {
                friendGeneratedCode.text = activeRoomCode;
            }
            if (friendCopyCodeBtn != null)
            {
                friendCopyCodeBtn.text = "📋 COPY ROOM CODE";
            }
        }

        private void OnCopyRoomCodeClicked()
        {
            GUIUtility.systemCopyBuffer = activeRoomCode;
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            if (friendCopyCodeBtn != null)
            {
                friendCopyCodeBtn.text = "✅ COPIED TO CLIPBOARD!";
            }
        }

        private void OnStartFriendMatchAsHost()
        {
            Debug.Log($"<color=magenta>[DominoHomeScreenController] Hosting Private Room: {activeRoomCode}...</color>");
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerMediumPulse();
            HideAllModals();

            DominoGameModeContext.CurrentMode = GameModeType.FriendRoom;
            DominoGameModeContext.RoomCode = activeRoomCode;
            DominoGameModeContext.IsHost = true;

            HideHomeScreen();
            EnsureWaitingControllers();

            if (waitingScreenUIToolkitController != null)
            {
                waitingScreenUIToolkitController.ShowWaitingScreen();
            }

            if (waitingScreenController != null)
            {
                waitingScreenController.StartFriendRoomFlow(activeRoomCode, isHost: true, playerName);
            }
        }

        private void OnJoinFriendMatch()
        {
            string enteredCode = friendCodeInput != null ? friendCodeInput.value.Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(enteredCode))
            {
                if (friendJoinStatus != null) friendJoinStatus.text = "Please enter a valid room code.";
                return;
            }

            Debug.Log($"<color=magenta>[DominoHomeScreenController] Joining Private Room: {enteredCode}...</color>");
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerMediumPulse();
            HideAllModals();

            DominoGameModeContext.CurrentMode = GameModeType.FriendRoom;
            DominoGameModeContext.RoomCode = enteredCode;
            DominoGameModeContext.IsHost = false;

            HideHomeScreen();
            EnsureWaitingControllers();

            if (waitingScreenUIToolkitController != null)
            {
                waitingScreenUIToolkitController.ShowWaitingScreen();
            }

            if (waitingScreenController != null)
            {
                waitingScreenController.StartFriendRoomFlow(enteredCode, isHost: false, playerName);
            }
        }

        private void EnsureWaitingControllers()
        {
            if (waitingScreenUIToolkitController == null)
            {
#if UNITY_2023_1_OR_NEWER
                waitingScreenUIToolkitController = FindAnyObjectByType<DominoWaitingScreenUIToolkitController>(FindObjectsInactive.Include);
#else
                waitingScreenUIToolkitController = FindObjectOfType<DominoWaitingScreenUIToolkitController>(true);
#endif
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

            if (waitingScreenController != null)
            {
                if (!waitingScreenController.gameObject.activeSelf)
                {
                    waitingScreenController.gameObject.SetActive(true);
                }
                if (waitingScreenController.transform.parent != null &&
                    !waitingScreenController.transform.parent.gameObject.activeInHierarchy)
                {
                    waitingScreenController.transform.SetParent(null, false);
                }
            }
        }

        private void LaunchGameMode(string modeName)
        {
            HideHomeScreen();
            EnsureWaitingControllers();

            if (waitingScreenUIToolkitController != null)
            {
                waitingScreenUIToolkitController.ShowWaitingScreen();
            }

            if (waitingScreenController != null)
            {
                waitingScreenController.OnPlayDominoesClicked();
            }
        }

        #endregion

        #region Profile & Settings Modals

        public void HideAllModals()
        {
            DominoAudioManager.Instance?.PlayClick();
            if (profileStatsModal != null) profileStatsModal.style.display = DisplayStyle.None;
            if (homeSettingsModal != null) homeSettingsModal.style.display = DisplayStyle.None;
            if (computerDifficultyModal != null) computerDifficultyModal.style.display = DisplayStyle.None;
            if (onlineRulesModal != null) onlineRulesModal.style.display = DisplayStyle.None;
            if (friendRoomModal != null) friendRoomModal.style.display = DisplayStyle.None;
        }

        private void OnProfileButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            if (profileStatsModal != null) profileStatsModal.style.display = DisplayStyle.Flex;
        }

        private void OnSettingsButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            UpdateSettingsUI();
            if (homeSettingsModal != null) homeSettingsModal.style.display = DisplayStyle.Flex;
        }

        private void UpdateSettingsUI()
        {
            bool musicOn = DominoAudioManager.Instance == null || !DominoAudioManager.Instance.IsMusicMuted;
            bool sfxOn = DominoAudioManager.Instance == null || !DominoAudioManager.Instance.IsSfxMuted;
            bool hapticsOn = DominoHapticsManager.IsHapticsEnabled;

            if (homeSettingsMusicBtn != null)
            {
                if (musicOn) { homeSettingsMusicBtn.AddToClassList("settings-toggle-btn--on"); homeSettingsMusicBtn.RemoveFromClassList("settings-toggle-btn--off"); }
                else { homeSettingsMusicBtn.RemoveFromClassList("settings-toggle-btn--on"); homeSettingsMusicBtn.AddToClassList("settings-toggle-btn--off"); }
            }
            if (homeSettingsMusicTxt != null) homeSettingsMusicTxt.text = musicOn ? "ON" : "OFF";

            if (homeSettingsSfxBtn != null)
            {
                if (sfxOn) { homeSettingsSfxBtn.AddToClassList("settings-toggle-btn--on"); homeSettingsSfxBtn.RemoveFromClassList("settings-toggle-btn--off"); }
                else { homeSettingsSfxBtn.RemoveFromClassList("settings-toggle-btn--on"); homeSettingsSfxBtn.AddToClassList("settings-toggle-btn--off"); }
            }
            if (homeSettingsSfxTxt != null) homeSettingsSfxTxt.text = sfxOn ? "ON" : "OFF";

            if (homeSettingsHapticsBtn != null)
            {
                if (hapticsOn) { homeSettingsHapticsBtn.AddToClassList("settings-toggle-btn--on"); homeSettingsHapticsBtn.RemoveFromClassList("settings-toggle-btn--off"); }
                else { homeSettingsHapticsBtn.RemoveFromClassList("settings-toggle-btn--on"); homeSettingsHapticsBtn.AddToClassList("settings-toggle-btn--off"); }
            }
            if (homeSettingsHapticsTxt != null) homeSettingsHapticsTxt.text = hapticsOn ? "ON" : "OFF";
        }

        private void OnToggleMusicClicked()
        {
            DominoAudioManager.Instance?.ToggleMusic();
            DominoAudioManager.Instance?.PlayClick();
            UpdateSettingsUI();
        }

        private void OnToggleSfxClicked()
        {
            DominoAudioManager.Instance?.ToggleSfx();
            DominoAudioManager.Instance?.PlayClick();
            UpdateSettingsUI();
        }

        private void OnToggleHapticsClicked()
        {
            DominoHapticsManager.ToggleHaptics();
            DominoAudioManager.Instance?.PlayClick();
            if (DominoHapticsManager.IsHapticsEnabled) DominoHapticsManager.TriggerLightTap();
            UpdateSettingsUI();
        }

        private void OnReplayTutorialClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            DominoTutorialController.ResetTutorial();
            LaunchGameMode("Tutorial Practice");
        }

        #endregion

        private void HandleLeftWaiting()
        {
            Debug.Log("[DominoHomeScreenController] Player left waiting screen. Restoring UI Toolkit HomeScreen.");
            ShowHomeScreen();
        }

        private Coroutine screenFadeCoroutine;

        public void ShowHomeScreen(bool animate = true)
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
                RegisterUICallbacks();
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.Flex;
                rootElement.pickingMode = PickingMode.Position;
                HideAllModals();
                RefreshProfileUI();
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

            if (waitingScreenUIToolkitController != null)
            {
                waitingScreenUIToolkitController.HideWaitingScreen(animate);
            }
        }

        public void HideHomeScreen(bool animate = true, Action onComplete = null)
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
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("home-root") ?? uiDocument.rootVisualElement;
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

        private IEnumerator FadeOutScreen(VisualElement element, Action onComplete = null, float duration = 0.35f)
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

        #region Loading Screen Animation
        private IEnumerator RunFirstTimeLoadingSequence()
        {
            if (gameLoadingScreen == null) yield break;

            gameLoadingScreen.style.display = DisplayStyle.Flex;
            gameLoadingScreen.style.opacity = 1f;

            string[] stages = new string[]
            {
                "Initializing table...",
                "Loading luxury domino set...",
                "Connecting to casual table...",
                "Shuffling bone yard...",
                "Ready to play!"
            };

            float duration = 2.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                int pct = Mathf.RoundToInt(smoothT * 100f);

                if (gameLoadingProgressBar != null)
                {
                    gameLoadingProgressBar.style.width = Length.Percent(pct);
                }

                if (gameLoadingPercentText != null)
                {
                    gameLoadingPercentText.text = $"{pct}%";
                }

                if (gameLoadingStatusText != null)
                {
                    int stageIdx = Mathf.Clamp((int)(smoothT * (stages.Length - 1)), 0, stages.Length - 1);
                    gameLoadingStatusText.text = stages[stageIdx];
                }

                yield return null;
            }

            if (gameLoadingProgressBar != null)
            {
                gameLoadingProgressBar.style.width = Length.Percent(100f);
            }
            if (gameLoadingPercentText != null)
            {
                gameLoadingPercentText.text = "100%";
            }
            if (gameLoadingStatusText != null)
            {
                gameLoadingStatusText.text = "Ready to play!";
            }

            yield return new WaitForSeconds(0.35f);

            // Smooth fadeout
            float fadeDur = 0.4f;
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeDur)
            {
                fadeElapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDur);
                if (gameLoadingScreen != null)
                {
                    gameLoadingScreen.style.opacity = alpha;
                }
                yield return null;
            }

            if (gameLoadingScreen != null)
            {
                gameLoadingScreen.style.display = DisplayStyle.None;
            }

            hasShownInitialLoading = true;
            loadingCoroutine = null;
        }

        public void TriggerLoadingScreen()
        {
            hasShownInitialLoading = false;
            if (gameLoadingScreen != null)
            {
                if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
                loadingCoroutine = StartCoroutine(RunFirstTimeLoadingSequence());
            }
        }
        #endregion
    }
}
