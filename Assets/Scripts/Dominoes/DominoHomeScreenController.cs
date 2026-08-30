using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// UI Toolkit controller for the commercial Dominoes HomeScreen (HomeScreen.uxml).
    /// Pure presentation layer managing:
    /// - Interactive Game Mode cards (Classic Draw, All Fives, Block, 2-Player Duel)
    /// - Live Currencies (Coins, Gems) & Player Profile HUD
    /// - Full Modals (Career Stats, Global Leaderboard, Daily Rewards Claimer, Themes & Shop, Settings)
    /// - Audio clicks and haptic feedback on all interactions
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class DominoHomeScreenController : MonoBehaviour
    {
        private const string PrefCoins = "Dominoes_Coins";
        private const string PrefGems = "Dominoes_Gems";
        private const string PrefPlayerName = "Dominoes_PlayerName";
        private const string PrefDailyClaimed = "Dominoes_DailyBonusClaimedDate";
        private const string PrefTileTheme = "Dominoes_TileTheme";
        private const string PrefFeltTheme = "Dominoes_FeltTheme";

        [Header("UI Document")]
        [Tooltip("The UIDocument component hosting HomeScreen.uxml.")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Controllers Integration")]
        [Tooltip("Reference to the existing DominoWaitingScreenController.")]
        [SerializeField] private DominoWaitingScreenController waitingScreenController;

        [Tooltip("Reference to the UI Toolkit DominoWaitingScreenUIToolkitController.")]
        [SerializeField] private DominoWaitingScreenUIToolkitController waitingScreenUIToolkitController;

        private VisualElement rootElement;
        private VisualElement safeContent;

        // Top Bar Elements
        private Button profileButton;
        private Label profileNameLabel;
        private Label profileLevelText;
        private Button coinsBadgeBtn;
        private Label coinsAmountLabel;
        private Button gemsBadgeBtn;
        private Label gemsAmountLabel;
        private Button homeSettingsButton;

        // Banner & Game Mode Cards
        private Button dailyBonusBannerBtn;
        private Label dailyBannerSubtitle;
        private Button dominoesCardButton;
        private Button modeAllFivesBtn;
        private Button modeBlockBtn;
        private Button modeDuelBtn;

        // Bottom Navigation Bar
        private Button navPlayBtn;
        private Button navLeaderboardBtn;
        private Button navRewardsBtn;
        private Button navShopBtn;
        private Button navStatsBtn;

        // Modals
        private VisualElement profileStatsModal;
        private Button profileCloseBtn;

        private VisualElement leaderboardModal;
        private Button leaderboardCloseBtn;

        private VisualElement dailyRewardsModal;
        private Button claimDailyRewardBtn;
        private Button dailyRewardsCloseBtn;

        private VisualElement shopModal;
        private Button themeTileIvoryBtn;
        private Button themeTileObsidianBtn;
        private Button themeTileGoldBtn;
        private Button themeFeltGreenBtn;
        private Button themeFeltBlueBtn;
        private Button themeFeltRubyBtn;
        private Button shopCloseBtn;

        private VisualElement homeSettingsModal;
        private Button homeSettingsMusicBtn;
        private Label homeSettingsMusicTxt;
        private Button homeSettingsSfxBtn;
        private Label homeSettingsSfxTxt;
        private Button homeSettingsHapticsBtn;
        private Label homeSettingsHapticsTxt;
        private Button homeReplayTutorialBtn;
        private Button homeSettingsCloseBtn;

        // Runtime Currency & Profile State
        private int coins = 12500;
        private int gems = 50;
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
                waitingScreenController = FindAnyObjectByType<DominoWaitingScreenController>();
#else
                waitingScreenController = FindObjectOfType<DominoWaitingScreenController>();
#endif
            }

            if (waitingScreenUIToolkitController == null)
            {
#if UNITY_2023_1_OR_NEWER
                waitingScreenUIToolkitController = FindAnyObjectByType<DominoWaitingScreenUIToolkitController>();
#else
                waitingScreenUIToolkitController = FindObjectOfType<DominoWaitingScreenUIToolkitController>();
#endif
            }

            LoadProfileAndCurrencies();
        }

        private void OnEnable()
        {
            RegisterUICallbacks();
            SubscribeWaitingEvents();
            ShowHomeScreen();
            RefreshCurrenciesUI();
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

        private void LoadProfileAndCurrencies()
        {
            coins = PlayerPrefs.GetInt(PrefCoins, 12500);
            gems = PlayerPrefs.GetInt(PrefGems, 50);
            playerName = PlayerPrefs.GetString(PrefPlayerName, "Player 1");
        }

        private void SaveCurrencies()
        {
            PlayerPrefs.SetInt(PrefCoins, coins);
            PlayerPrefs.SetInt(PrefGems, gems);
            PlayerPrefs.Save();
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

            // 1. Top Bar Elements
            profileButton = rootElement.Q<Button>("profile-button");
            profileNameLabel = rootElement.Q<Label>("profile-name-label");
            profileLevelText = rootElement.Q<Label>("profile-level-text");
            coinsBadgeBtn = rootElement.Q<Button>("coins-badge-btn");
            coinsAmountLabel = rootElement.Q<Label>("coins-amount-label");
            gemsBadgeBtn = rootElement.Q<Button>("gems-badge-btn");
            gemsAmountLabel = rootElement.Q<Label>("gems-amount-label");
            homeSettingsButton = rootElement.Q<Button>("home-settings-button");

            if (profileButton != null) profileButton.clicked += OnProfileButtonClicked;
            if (coinsBadgeBtn != null) coinsBadgeBtn.clicked += OnShopButtonClicked;
            if (gemsBadgeBtn != null) gemsBadgeBtn.clicked += OnShopButtonClicked;
            if (homeSettingsButton != null) homeSettingsButton.clicked += OnSettingsButtonClicked;

            // 2. Banner & Game Modes
            dailyBonusBannerBtn = rootElement.Q<Button>("daily-bonus-banner-btn");
            dailyBannerSubtitle = rootElement.Q<Label>("daily-banner-subtitle");
            dominoesCardButton = rootElement.Q<Button>("dominoes-card");
            modeAllFivesBtn = rootElement.Q<Button>("mode-allfives-btn");
            modeBlockBtn = rootElement.Q<Button>("mode-block-btn");
            modeDuelBtn = rootElement.Q<Button>("mode-duel-btn");

            if (dailyBonusBannerBtn != null) dailyBonusBannerBtn.clicked += OnDailyRewardsButtonClicked;
            if (dominoesCardButton != null) dominoesCardButton.clicked += () => LaunchGameMode("Classic Draw");
            if (modeAllFivesBtn != null) modeAllFivesBtn.clicked += () => LaunchGameMode("All Fives");
            if (modeBlockBtn != null) modeBlockBtn.clicked += () => LaunchGameMode("Block");
            if (modeDuelBtn != null) modeDuelBtn.clicked += () => LaunchGameMode("Heads-Up Duel");

            // 3. Bottom Nav Tabs
            navPlayBtn = rootElement.Q<Button>("nav-play-btn");
            navLeaderboardBtn = rootElement.Q<Button>("nav-leaderboard-btn");
            navRewardsBtn = rootElement.Q<Button>("nav-rewards-btn");
            navShopBtn = rootElement.Q<Button>("nav-shop-btn");
            navStatsBtn = rootElement.Q<Button>("nav-stats-btn");

            if (navPlayBtn != null) navPlayBtn.clicked += HideAllModals;
            if (navLeaderboardBtn != null) navLeaderboardBtn.clicked += OnLeaderboardButtonClicked;
            if (navRewardsBtn != null) navRewardsBtn.clicked += OnDailyRewardsButtonClicked;
            if (navShopBtn != null) navShopBtn.clicked += OnShopButtonClicked;
            if (navStatsBtn != null) navStatsBtn.clicked += OnProfileButtonClicked;

            // 4. Modals
            profileStatsModal = rootElement.Q<VisualElement>("profile-stats-modal");
            profileCloseBtn = rootElement.Q<Button>("profile-close-btn");
            if (profileCloseBtn != null) profileCloseBtn.clicked += HideAllModals;

            leaderboardModal = rootElement.Q<VisualElement>("leaderboard-modal");
            leaderboardCloseBtn = rootElement.Q<Button>("leaderboard-close-btn");
            if (leaderboardCloseBtn != null) leaderboardCloseBtn.clicked += HideAllModals;

            dailyRewardsModal = rootElement.Q<VisualElement>("daily-rewards-modal");
            claimDailyRewardBtn = rootElement.Q<Button>("claim-daily-reward-btn");
            dailyRewardsCloseBtn = rootElement.Q<Button>("daily-rewards-close-btn");
            if (claimDailyRewardBtn != null) claimDailyRewardBtn.clicked += OnClaimDailyRewardClicked;
            if (dailyRewardsCloseBtn != null) dailyRewardsCloseBtn.clicked += HideAllModals;

            shopModal = rootElement.Q<VisualElement>("shop-modal");
            themeTileIvoryBtn = rootElement.Q<Button>("theme-tile-ivory-btn");
            themeTileObsidianBtn = rootElement.Q<Button>("theme-tile-obsidian-btn");
            themeTileGoldBtn = rootElement.Q<Button>("theme-tile-gold-btn");
            themeFeltGreenBtn = rootElement.Q<Button>("theme-felt-green-btn");
            themeFeltBlueBtn = rootElement.Q<Button>("theme-felt-blue-btn");
            themeFeltRubyBtn = rootElement.Q<Button>("theme-felt-ruby-btn");
            shopCloseBtn = rootElement.Q<Button>("shop-close-btn");

            if (themeTileIvoryBtn != null) themeTileIvoryBtn.clicked += () => EquipTileTheme("Ivory");
            if (themeTileObsidianBtn != null) themeTileObsidianBtn.clicked += () => EquipTileTheme("Obsidian");
            if (themeTileGoldBtn != null) themeTileGoldBtn.clicked += () => EquipTileTheme("Gold");
            if (themeFeltGreenBtn != null) themeFeltGreenBtn.clicked += () => EquipFeltTheme("Green");
            if (themeFeltBlueBtn != null) themeFeltBlueBtn.clicked += () => EquipFeltTheme("Blue");
            if (themeFeltRubyBtn != null) themeFeltRubyBtn.clicked += () => EquipFeltTheme("Ruby");
            if (shopCloseBtn != null) shopCloseBtn.clicked += HideAllModals;

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
            RefreshCurrenciesUI();
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

            safeContent.style.paddingLeft = Length.Percent(Mathf.Max(2.5f, leftPercent));
            safeContent.style.paddingRight = Length.Percent(Mathf.Max(2.5f, rightPercent));
            safeContent.style.paddingTop = Length.Percent(Mathf.Max(3f, topPercent));
            safeContent.style.paddingBottom = Length.Percent(Mathf.Max(2f, bottomPercent));
        }

        private void UnregisterUICallbacks()
        {
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

        public void RefreshCurrenciesUI()
        {
            if (coinsAmountLabel != null) coinsAmountLabel.text = $"{coins:N0}";
            if (gemsAmountLabel != null) gemsAmountLabel.text = $"{gems:N0}";
            if (profileNameLabel != null) profileNameLabel.text = playerName;
        }

        #region Game Mode Launches

        private void LaunchGameMode(string modeName)
        {
            Debug.Log($"<color=cyan>[DominoHomeScreenController] Launching Mode: {modeName}...</color>");
            DominoAudioManager.Instance?.PlayClick();
            DominoHapticsManager.TriggerLightTap();

            HideHomeScreen();

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

        #endregion

        #region Modals & Tabs

        public void HideAllModals()
        {
            DominoAudioManager.Instance?.PlayClick();
            if (profileStatsModal != null) profileStatsModal.style.display = DisplayStyle.None;
            if (leaderboardModal != null) leaderboardModal.style.display = DisplayStyle.None;
            if (dailyRewardsModal != null) dailyRewardsModal.style.display = DisplayStyle.None;
            if (shopModal != null) shopModal.style.display = DisplayStyle.None;
            if (homeSettingsModal != null) homeSettingsModal.style.display = DisplayStyle.None;

            UpdateNavTabs(navPlayBtn);
        }

        private void UpdateNavTabs(Button activeBtn)
        {
            if (navPlayBtn != null) navPlayBtn.RemoveFromClassList("nav-tab-btn--active");
            if (navLeaderboardBtn != null) navLeaderboardBtn.RemoveFromClassList("nav-tab-btn--active");
            if (navRewardsBtn != null) navRewardsBtn.RemoveFromClassList("nav-tab-btn--active");
            if (navShopBtn != null) navShopBtn.RemoveFromClassList("nav-tab-btn--active");
            if (navStatsBtn != null) navStatsBtn.RemoveFromClassList("nav-tab-btn--active");

            if (activeBtn != null) activeBtn.AddToClassList("nav-tab-btn--active");
        }

        private void OnProfileButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            if (profileStatsModal != null) profileStatsModal.style.display = DisplayStyle.Flex;
            UpdateNavTabs(navStatsBtn);
        }

        private void OnLeaderboardButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            if (leaderboardModal != null) leaderboardModal.style.display = DisplayStyle.Flex;
            UpdateNavTabs(navLeaderboardBtn);
        }

        private void OnDailyRewardsButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            if (dailyRewardsModal != null) dailyRewardsModal.style.display = DisplayStyle.Flex;
            UpdateNavTabs(navRewardsBtn);
        }

        private void OnShopButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            if (shopModal != null) shopModal.style.display = DisplayStyle.Flex;
            UpdateNavTabs(navShopBtn);
        }

        private void OnSettingsButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideAllModals();
            UpdateSettingsUI();
            if (homeSettingsModal != null) homeSettingsModal.style.display = DisplayStyle.Flex;
        }

        private void OnClaimDailyRewardClicked()
        {
            coins += 1000;
            SaveCurrencies();
            RefreshCurrenciesUI();

            DominoAudioManager.Instance?.PlayWin();
            DominoHapticsManager.TriggerWinCelebration();

            if (claimDailyRewardBtn != null)
            {
                claimDailyRewardBtn.text = "CLAIMED! ✓";
                claimDailyRewardBtn.SetEnabled(false);
            }

            if (dailyBannerSubtitle != null)
            {
                dailyBannerSubtitle.text = "Reward Claimed! Next in 24h";
            }
        }

        private void EquipTileTheme(string theme)
        {
            DominoAudioManager.Instance?.PlayClick();
            PlayerPrefs.SetString(PrefTileTheme, theme);
            PlayerPrefs.Save();
            Debug.Log($"[DominoHomeScreenController] Equipped Tile Theme: {theme}");
        }

        private void EquipFeltTheme(string theme)
        {
            DominoAudioManager.Instance?.PlayClick();
            PlayerPrefs.SetString(PrefFeltTheme, theme);
            PlayerPrefs.Save();
            Debug.Log($"[DominoHomeScreenController] Equipped Table Felt Theme: {theme}");
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
            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("home-root") ?? uiDocument.rootVisualElement;
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.Flex;
                HideAllModals();
                RefreshCurrenciesUI();
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
