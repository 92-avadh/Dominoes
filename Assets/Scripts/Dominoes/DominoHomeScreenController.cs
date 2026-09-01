using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// UI Toolkit controller for the commercial Dominoes HomeScreen (HomeScreen.uxml).
    /// Pure presentation layer managing:
    /// - Header HUD (Left: Menu Button opening Settings/Audio/Haptics, Center: Tappable Profile Avatar/Level)
    /// - 3 Commercial Game Mode buttons (ONLINE, VS COMPUTER, WITH FRIENDS)
    /// - Interactive Game Mode touch states & haptics
    /// - Clean Modals (Player Profile, Settings & Options with Audio/Haptics)
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

        // 3 Game Mode Buttons
        private Button modeOnlineBtn;
        private Button modeComputerBtn;
        private Button modeFriendsBtn;
        private Button homeTutorialBtn;

        // Profile Modal
        private VisualElement profileStatsModal;
        private Label modalPlayerName;
        private Label modalPlayerLevel;
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
            playerName = PlayerPrefs.GetString(PrefPlayerName, "Player 1");
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

            // Hamburger button opens Settings & Game Options
            if (hamburgerButton != null) hamburgerButton.clicked += OnSettingsButtonClicked;
            if (profileButton != null) profileButton.clicked += OnProfileButtonClicked;

            // 2. 3 Game Mode Buttons
            modeOnlineBtn = rootElement.Q<Button>("mode-online-btn");
            modeComputerBtn = rootElement.Q<Button>("mode-computer-btn");
            modeFriendsBtn = rootElement.Q<Button>("mode-friends-btn");
            homeTutorialBtn = rootElement.Q<Button>("home-tutorial-btn");

            if (modeOnlineBtn != null) modeOnlineBtn.clicked += () => LaunchGameMode("Online");
            if (modeComputerBtn != null) modeComputerBtn.clicked += () => LaunchGameMode("Vs Computer");
            if (modeFriendsBtn != null) modeFriendsBtn.clicked += () => LaunchGameMode("With Friends");
            if (homeTutorialBtn != null) homeTutorialBtn.clicked += OnTutorialCalloutClicked;

            // 3. Profile Modal
            profileStatsModal = rootElement.Q<VisualElement>("profile-stats-modal");
            modalPlayerName = rootElement.Q<Label>("modal-player-name");
            modalPlayerLevel = rootElement.Q<Label>("modal-player-level");
            profileCloseBtn = rootElement.Q<Button>("profile-close-btn");

            if (profileCloseBtn != null) profileCloseBtn.clicked += HideAllModals;

            // 4. Settings Modal
            homeSettingsModal = rootElement.Q<VisualElement>("home-settings-modal");
            homeSettingsMusicBtn = rootElement.Q<Button>("home-settings-music-btn");
            homeSettingsMusicTxt = rootElement.Q<Label>("home-settings-music-txt");
            homeSettingsSfxBtn = rootElement.Q<Button>("home-settings-sfx-btn");
            homeSettingsSfxTxt = rootElement.Q<Label>("home-settings-sfx-txt");
            homeSettingsHapticsBtn = rootElement.Q<Button>("home-settings-haptics-btn");
            homeSettingsHapticsTxt = rootElement.Q<Label>("home-settings-haptics-txt");
            homeReplayTutorialBtn = rootElement.Q<Button>("home-replay-tutorial-btn");
            homeSettingsCloseBtn = rootElement.Q<Button>("home-settings-close-btn");

            if (homeSettingsMusicBtn != null) homeSettingsMusicBtn.clicked += OnToggleMusicClicked;
            if (homeSettingsSfxBtn != null) homeSettingsSfxBtn.clicked += OnToggleSfxClicked;
            if (homeSettingsHapticsBtn != null) homeSettingsHapticsBtn.clicked += OnToggleHapticsClicked;
            if (homeReplayTutorialBtn != null) homeReplayTutorialBtn.clicked += OnReplayTutorialClicked;
            if (homeSettingsCloseBtn != null) homeSettingsCloseBtn.clicked += HideAllModals;

            ApplySafeArea();
            RefreshProfileUI();
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

        private void UnregisterUICallbacks()
        {
            if (rootElement != null)
            {
                rootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            }

            if (hamburgerButton != null) hamburgerButton.clicked -= OnSettingsButtonClicked;
            if (profileButton != null) profileButton.clicked -= OnProfileButtonClicked;

            if (profileCloseBtn != null) profileCloseBtn.clicked -= HideAllModals;
            if (homeSettingsCloseBtn != null) homeSettingsCloseBtn.clicked -= HideAllModals;

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
        }

        #region Game Mode Launches

        private void LaunchGameMode(string modeName)
        {
            Debug.Log($"<color=cyan>[DominoHomeScreenController] Launching Mode: {modeName}...</color>");
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();

            HideHomeScreen();

            if (waitingScreenUIToolkitController == null)
            {
#if UNITY_2023_1_OR_NEWER
                waitingScreenUIToolkitController = FindAnyObjectByType<DominoWaitingScreenUIToolkitController>(FindObjectsInactive.Include);
#else
                waitingScreenUIToolkitController = FindObjectOfType<DominoWaitingScreenUIToolkitController>(true);
#endif
            }

            if (waitingScreenUIToolkitController != null)
            {
                waitingScreenUIToolkitController.ShowWaitingScreen();
            }

            if (waitingScreenController != null)
            {
                waitingScreenController.OnPlayDominoesClicked();
            }
            else
            {
                Debug.LogError("[DominoHomeScreenController] Cannot start match: DominoWaitingScreenController is null!", this);
            }
        }

        private void OnTutorialCalloutClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();
            HideAllModals();

            DominoTutorialController.ResetTutorial();
            LaunchGameMode("Tutorial Practice");
        }

        #endregion

        #region Modals

        public void HideAllModals()
        {
            DominoAudioManager.Instance?.PlayClick();
            if (profileStatsModal != null) profileStatsModal.style.display = DisplayStyle.None;
            if (homeSettingsModal != null) homeSettingsModal.style.display = DisplayStyle.None;
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

        public void ShowHomeScreen()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("home-root") ?? uiDocument.rootVisualElement;
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.Flex;
                HideAllModals();
                RefreshProfileUI();
                ApplySafeArea();
            }

            if (waitingScreenUIToolkitController != null)
            {
                waitingScreenUIToolkitController.HideWaitingScreen();
            }
        }

        public void HideHomeScreen()
        {
            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("home-root") ?? uiDocument.rootVisualElement;
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.None;
            }
        }
    }
}
