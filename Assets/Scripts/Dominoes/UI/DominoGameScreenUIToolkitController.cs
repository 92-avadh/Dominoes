using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Production UI Toolkit controller for the Dominoes GameScreen (GameScreen.uxml).
    /// Professional Visual, Audio, Animation, and Game-Feel Layer:
    /// - Evaluates legal moves via DominoMoveValidator / DominoPlacementManager
    /// - Renders skeuomorphic physical ivory domino tiles with authentic 3x3 pips
    /// - Employs DominoBoardLayoutEngine with smooth position & scale interpolation on board rearrangement
    /// - Intuitive Drag & Drop and Tap-to-Place interactions with landing bounce and clack audio sync
    /// - Interactive Boneyard Stack on top-left of the board table with live tile counter and draw glow
    /// - Floating Circular Lamp Hint Button and contextual Pass Button
    /// - Complete Audio & Haptics integration (click, pickup, clack, draw, turn chime, win fanfare)
    /// - Settings modal for Music, SFX, Haptics toggles and tutorial replay
    /// - Animated victory score count-up
    /// - Safe-area aware for modern Android devices
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class DominoGameScreenUIToolkitController : MonoBehaviour
    {
        [Header("UI Document")]
        [Tooltip("The UIDocument component hosting GameScreen.uxml. If left empty, will be auto-detected on this GameObject.")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Controllers Integration")]
        [Tooltip("Reference to the existing DominoWaitingScreenController.")]
        [SerializeField] private DominoWaitingScreenController waitingScreenController;

        [Tooltip("Reference to the UI Toolkit DominoHomeScreenController.")]
        [SerializeField] private DominoHomeScreenController homeScreenController;

        [Tooltip("Reference to the UI Toolkit DominoWaitingScreenUIToolkitController.")]
        [SerializeField] private DominoWaitingScreenUIToolkitController waitingScreenUIToolkitController;

        // Gameplay sub-managers
        private readonly DominoPlacementManager placementManager = new DominoPlacementManager();
        private readonly DominoPassDrawManager passDrawManager = new DominoPassDrawManager();

        // Tutorial Controller
        private DominoTutorialController tutorialController;

        // UI Visual Elements
        private VisualElement rootElement;
        private VisualElement safeContent;

        // 1. Top Bar
        private Button gameMenuButton;
        private Button settingsButton;
        private Button helpButton;
        private Label roundInfoLabel;

        // 2. Opponents Bar (Up to 3 Opponents)
        private VisualElement[] opponentSlots = new VisualElement[3];
        private Label[] opponentNames = new Label[3];
        private Label[] opponentTileCounts = new Label[3];
        private VisualElement[] opponentActiveIndicators = new VisualElement[3];

        // 3. Board Area & Boneyard Stack
        private VisualElement boardFelt;
        private Button boneyardPileBtn;
        private Label boneyardCountLabel;
        private Label boneyardActionHint;

        private VisualElement boardTilesContainer;
        private Label emptyBoardMessage;
        private VisualElement leftEndTarget;
        private Label leftTargetNum;
        private VisualElement rightEndTarget;
        private Label rightTargetNum;

        // 4. Turn Guidance Bar & Floating Actions
        private VisualElement turnGuidanceBar;
        private VisualElement turnStatusDot;
        private Label turnTitleLabel;
        private Label turnSubtitleLabel;

        private Button passButton;
        private Button hintButton;

        // 5. Player Hand Area
        private Label handTileCountLabel;
        private VisualElement handTilesContainer;

        // 6. Drag Ghost Element
        private VisualElement dragGhostTile;

        // 7. Settings Modal
        private VisualElement settingsModal;
        private Button settingsMusicToggleBtn;
        private Label settingsMusicToggleText;
        private Button settingsSfxToggleBtn;
        private Label settingsSfxToggleText;
        private Button settingsHapticsToggleBtn;
        private Label settingsHapticsToggleText;
        private Button settingsReplayTutorialBtn;
        private Button settingsCloseBtn;

        // 8. Help / Rules Modal
        private VisualElement helpRulesModal;
        private Button replayTutorialBtn;
        private Button helpRulesCloseBtn;

        // 9. Result Modal
        private VisualElement resultModal;
        private Label resultTitle;
        private Label resultWinnerLabel;
        private Label animatedScoreLabel;
        private VisualElement resultScoresList;
        private Button playAgainButton;
        private Button exitHomeButton;

        // 10. Leave Confirmation Modal
        private VisualElement leaveConfirmModal;
        private VisualElement leaveModalCard;
        private Button leaveModalCancelBtn;
        private Button leaveModalConfirmBtn;

        // Runtime State
        private DominoTile selectedTile;
        private DominoTile draggingTile;
        private bool isDragging;
        private Vector2 dragStartPosition;
        private DominoBoardLayoutResult lastLayoutResult;
        private readonly Dictionary<DominoTile, Vector2> previousTilePositions = new Dictionary<DominoTile, Vector2>();
        private Coroutine botTurnCoroutine;
        private Coroutine scoreCountCoroutine;
        private Coroutine boardAnimationCoroutine;
        private bool isEventsSubscribed;
        private DominoPlayer lastActivePlayer;

        public UIDocument UIDocument => uiDocument;
        public DominoTutorialController TutorialController => tutorialController;
        public DominoMatchManager CurrentMatchManager => waitingScreenController != null ? waitingScreenController.MatchManager : null;
        public DominoTile SelectedTile => selectedTile;

        public void SelectTile(DominoTile tile)
        {
            selectedTile = tile;
            RefreshAll();
        }

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            tutorialController = new DominoTutorialController(this);

            if (waitingScreenController == null)
            {
#if UNITY_2023_1_OR_NEWER
                waitingScreenController = FindAnyObjectByType<DominoWaitingScreenController>();
#else
                waitingScreenController = FindObjectOfType<DominoWaitingScreenController>();
#endif
            }

            if (homeScreenController == null)
            {
#if UNITY_2023_1_OR_NEWER
                homeScreenController = FindAnyObjectByType<DominoHomeScreenController>();
#else
                homeScreenController = FindObjectOfType<DominoHomeScreenController>();
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

            // Ensure AudioManager exists in scene
            if (DominoAudioManager.Instance == null)
            {
                var audioGo = new GameObject("DominoAudioManager");
                audioGo.AddComponent<DominoAudioManager>();
            }

            HideGameScreen();
        }

        private void OnEnable()
        {
            RegisterUIElements();
            SubscribeMatchEvents();
            HideGameScreen();
        }

        private void OnDisable()
        {
            UnregisterUIElements();
            UnsubscribeMatchEvents();
            StopAllGameCoroutines();
        }

        private void OnDestroy()
        {
            UnregisterUIElements();
            UnsubscribeMatchEvents();
            StopAllGameCoroutines();
        }

        private void Start()
        {
            HideGameScreen();
        }

        private void StopAllGameCoroutines()
        {
            if (botTurnCoroutine != null) { StopCoroutine(botTurnCoroutine); botTurnCoroutine = null; }
            if (scoreCountCoroutine != null) { StopCoroutine(scoreCountCoroutine); scoreCountCoroutine = null; }
            if (boardAnimationCoroutine != null) { StopCoroutine(boardAnimationCoroutine); boardAnimationCoroutine = null; }
        }

        /// <summary>
        /// Queries and binds all UI Toolkit visual elements.
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

            rootElement = panelRoot.Q<VisualElement>("game-root") ?? panelRoot;
            safeContent = rootElement.Q<VisualElement>("safe-content") ?? rootElement;
            rootElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            // Pointer move/up on root for global drag tracking
            rootElement.RegisterCallback<PointerMoveEvent>(OnRootPointerMove);
            rootElement.RegisterCallback<PointerUpEvent>(OnRootPointerUp);
            rootElement.RegisterCallback<PointerCancelEvent>(OnRootPointerCancel);

            // 1. Top Bar
            gameMenuButton = rootElement.Q<Button>("game-menu-button");
            settingsButton = rootElement.Q<Button>("settings-button");
            helpButton = rootElement.Q<Button>("help-button");
            roundInfoLabel = rootElement.Q<Label>("round-info-label");

            if (gameMenuButton != null) gameMenuButton.clicked += OnMenuButtonClicked;
            if (settingsButton != null) settingsButton.clicked += OnSettingsButtonClicked;
            if (helpButton != null) helpButton.clicked += OnHelpButtonClicked;

            // 2. Opponents Bar
            for (int i = 0; i < 3; i++)
            {
                opponentSlots[i] = rootElement.Q<VisualElement>($"opponent-slot-{i}");
                opponentNames[i] = rootElement.Q<Label>($"opponent-name-{i}");
                opponentTileCounts[i] = rootElement.Q<Label>($"opponent-tile-count-{i}");
                opponentActiveIndicators[i] = rootElement.Q<VisualElement>($"opponent-active-indicator-{i}");
            }

            // 3. Board Area & Boneyard Stack
            boardFelt = rootElement.Q<VisualElement>("board-felt");
            boneyardPileBtn = rootElement.Q<Button>("boneyard-pile-btn");
            boneyardCountLabel = rootElement.Q<Label>("boneyard-count-label");
            boneyardActionHint = rootElement.Q<Label>("boneyard-action-hint");

            if (boneyardPileBtn != null) boneyardPileBtn.clicked += OnBoneyardClicked;

            boardTilesContainer = rootElement.Q<VisualElement>("board-tiles-container");
            emptyBoardMessage = rootElement.Q<Label>("empty-board-message");

            leftEndTarget = rootElement.Q<VisualElement>("left-end-target");
            leftTargetNum = rootElement.Q<Label>("left-target-num");
            rightEndTarget = rootElement.Q<VisualElement>("right-end-target");
            rightTargetNum = rootElement.Q<Label>("right-target-num");

            if (leftEndTarget != null) leftEndTarget.RegisterCallback<ClickEvent>(evt => OnPlaceEndpointClicked(BoardSide.Left));
            if (rightEndTarget != null) rightEndTarget.RegisterCallback<ClickEvent>(evt => OnPlaceEndpointClicked(BoardSide.Right));

            // 4. Turn Guidance Bar & Floating Actions
            turnGuidanceBar = rootElement.Q<VisualElement>("turn-guidance-bar");
            turnStatusDot = rootElement.Q<VisualElement>("turn-status-dot");
            turnTitleLabel = rootElement.Q<Label>("turn-title-label");
            turnSubtitleLabel = rootElement.Q<Label>("turn-subtitle-label");

            passButton = rootElement.Q<Button>("pass-button");
            hintButton = rootElement.Q<Button>("hint-button");

            if (passButton != null) passButton.clicked += OnPassButtonClicked;
            if (hintButton != null) hintButton.clicked += OnHintButtonClicked;

            // 5. Player Hand Area
            handTileCountLabel = rootElement.Q<Label>("hand-tile-count-label");
            handTilesContainer = rootElement.Q<VisualElement>("hand-tiles-container");

            // 6. Drag Ghost Element
            dragGhostTile = rootElement.Q<VisualElement>("drag-ghost-tile");

            // 7. Settings Modal
            settingsModal = rootElement.Q<VisualElement>("settings-modal");
            settingsMusicToggleBtn = rootElement.Q<Button>("settings-music-toggle-btn");
            settingsMusicToggleText = rootElement.Q<Label>("settings-music-toggle-text");
            settingsSfxToggleBtn = rootElement.Q<Button>("settings-sfx-toggle-btn");
            settingsSfxToggleText = rootElement.Q<Label>("settings-sfx-toggle-text");
            settingsHapticsToggleBtn = rootElement.Q<Button>("settings-haptics-toggle-btn");
            settingsHapticsToggleText = rootElement.Q<Label>("settings-haptics-toggle-text");
            settingsReplayTutorialBtn = rootElement.Q<Button>("settings-replay-tutorial-btn");
            settingsCloseBtn = rootElement.Q<Button>("settings-close-btn");

            if (settingsMusicToggleBtn != null) settingsMusicToggleBtn.clicked += OnToggleMusicClicked;
            if (settingsSfxToggleBtn != null) settingsSfxToggleBtn.clicked += OnToggleSfxClicked;
            if (settingsHapticsToggleBtn != null) settingsHapticsToggleBtn.clicked += OnToggleHapticsClicked;
            if (settingsReplayTutorialBtn != null) settingsReplayTutorialBtn.clicked += OnReplayTutorialClicked;
            if (settingsCloseBtn != null) settingsCloseBtn.clicked += HideSettingsModal;

            // 8. Help / Rules Modal
            helpRulesModal = rootElement.Q<VisualElement>("help-rules-modal");
            replayTutorialBtn = rootElement.Q<Button>("replay-tutorial-btn");
            helpRulesCloseBtn = rootElement.Q<Button>("help-rules-close-btn");

            if (replayTutorialBtn != null) replayTutorialBtn.clicked += OnReplayTutorialClicked;
            if (helpRulesCloseBtn != null) helpRulesCloseBtn.clicked += HideHelpModal;

            // 9. Results Modal
            resultModal = rootElement.Q<VisualElement>("result-modal");
            resultTitle = rootElement.Q<Label>("result-title");
            resultWinnerLabel = rootElement.Q<Label>("result-winner-label");
            animatedScoreLabel = rootElement.Q<Label>("animated-score-label");
            resultScoresList = rootElement.Q<VisualElement>("result-scores-list");
            playAgainButton = rootElement.Q<Button>("play-again-button");
            exitHomeButton = rootElement.Q<Button>("exit-home-button");

            if (playAgainButton != null) playAgainButton.clicked += OnPlayAgainClicked;
            if (exitHomeButton != null) exitHomeButton.clicked += OnExitHomeClicked;

            // 10. Leave Confirmation Modal (Image 3 Broken Domino Alert Modal)
            leaveConfirmModal = rootElement.Q<VisualElement>("leave-confirm-modal");
            leaveModalCard = rootElement.Q<VisualElement>("leave-modal-card");
            leaveModalCancelBtn = rootElement.Q<Button>("leave-modal-cancel-btn");
            leaveModalConfirmBtn = rootElement.Q<Button>("leave-modal-confirm-btn");
            var alertCloseXBtn = rootElement.Q<Button>("alert-close-x-btn");

            if (leaveModalCancelBtn != null) leaveModalCancelBtn.clicked += HideLeaveConfirmation;
            if (leaveModalConfirmBtn != null) leaveModalConfirmBtn.clicked += OnConfirmLeaveClicked;
            if (alertCloseXBtn != null) alertCloseXBtn.clicked += HideLeaveConfirmation;

            // 11. Bind Tutorial Controller
            tutorialController ??= new DominoTutorialController(this);
            tutorialController.BindUIElements(rootElement);

            ApplySafeArea();
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            ApplySafeArea();
            if (waitingScreenController != null && waitingScreenController.MatchManager != null)
            {
                RefreshBoard(waitingScreenController.MatchManager, animate: false);
            }
        }

        private void UnregisterUIElements()
        {
            if (rootElement != null)
            {
                rootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                rootElement.UnregisterCallback<PointerMoveEvent>(OnRootPointerMove);
                rootElement.UnregisterCallback<PointerUpEvent>(OnRootPointerUp);
                rootElement.UnregisterCallback<PointerCancelEvent>(OnRootPointerCancel);
            }

            if (gameMenuButton != null) gameMenuButton.clicked -= OnMenuButtonClicked;
            if (settingsButton != null) settingsButton.clicked -= OnSettingsButtonClicked;
            if (helpButton != null) helpButton.clicked -= OnHelpButtonClicked;
            if (boneyardPileBtn != null) boneyardPileBtn.clicked -= OnBoneyardClicked;
            if (passButton != null) passButton.clicked -= OnPassButtonClicked;
            if (hintButton != null) hintButton.clicked -= OnHintButtonClicked;
            if (settingsMusicToggleBtn != null) settingsMusicToggleBtn.clicked -= OnToggleMusicClicked;
            if (settingsSfxToggleBtn != null) settingsSfxToggleBtn.clicked -= OnToggleSfxClicked;
            if (settingsHapticsToggleBtn != null) settingsHapticsToggleBtn.clicked -= OnToggleHapticsClicked;
            if (settingsReplayTutorialBtn != null) settingsReplayTutorialBtn.clicked -= OnReplayTutorialClicked;
            if (settingsCloseBtn != null) settingsCloseBtn.clicked -= HideSettingsModal;
            if (replayTutorialBtn != null) replayTutorialBtn.clicked -= OnReplayTutorialClicked;
            if (helpRulesCloseBtn != null) helpRulesCloseBtn.clicked -= HideHelpModal;
            if (playAgainButton != null) playAgainButton.clicked -= OnPlayAgainClicked;
            if (exitHomeButton != null) exitHomeButton.clicked -= OnExitHomeClicked;

            if (leaveModalCancelBtn != null) leaveModalCancelBtn.clicked -= HideLeaveConfirmation;
            if (leaveModalConfirmBtn != null) leaveModalConfirmBtn.clicked -= OnConfirmLeaveClicked;

            if (tutorialController != null)
            {
                tutorialController.UnbindUIElements();
            }

            rootElement = null;
            safeContent = null;
            boardFelt = null;
            boardTilesContainer = null;
            boneyardPileBtn = null;
            settingsModal = null;
            helpRulesModal = null;
            leaveConfirmModal = null;
            leaveModalCard = null;
            leaveModalCancelBtn = null;
            leaveModalConfirmBtn = null;
        }

        private void SubscribeMatchEvents()
        {
            if (waitingScreenController == null || isEventsSubscribed) return;

            var matchManager = waitingScreenController.MatchManager;
            if (matchManager != null)
            {
                matchManager.OnMatchStarted += HandleMatchStarted;
                matchManager.OnMatchStateChanged += HandleMatchStateChanged;
                isEventsSubscribed = true;
            }
        }

        private void UnsubscribeMatchEvents()
        {
            if (waitingScreenController == null || !isEventsSubscribed) return;

            var matchManager = waitingScreenController.MatchManager;
            if (matchManager != null)
            {
                matchManager.OnMatchStarted -= HandleMatchStarted;
                matchManager.OnMatchStateChanged -= HandleMatchStateChanged;
            }

            isEventsSubscribed = false;
        }

        private void HandleMatchStarted()
        {
            Debug.Log("<color=green>[DominoGameScreenUIToolkitController] Match Started! Opening GameScreen.</color>");
            selectedTile = null;
            CancelDragging();
            previousTilePositions.Clear();
            ShowGameScreen();
            RefreshAll(animateBoard: false);

            DominoAudioManager.Instance?.PlayMusic();

            if (!DominoTutorialController.IsTutorialCompleted())
            {
                tutorialController.StartTutorial();
            }

            CheckBotTurn();
        }

        private void HandleMatchStateChanged(MatchState newState)
        {
            if (newState == MatchState.Playing)
            {
                selectedTile = null;
                CancelDragging();
                ShowGameScreen();
                RefreshAll(animateBoard: false);

                DominoAudioManager.Instance?.PlayMusic();

                if (!DominoTutorialController.IsTutorialCompleted())
                {
                    tutorialController.StartTutorial();
                }

                CheckBotTurn();
            }
            else if (newState == MatchState.Finished)
            {
                ShowResultModal();
            }
            else
            {
                HideGameScreen();
            }
        }

        /// <summary>
        /// Full refresh of all GameScreen UI panels with optional board animation.
        /// </summary>
        public void RefreshAll(bool animateBoard = false)
        {
            if (waitingScreenController == null || waitingScreenController.MatchManager == null) return;
            var match = waitingScreenController.MatchManager;

            RefreshTopBar(match);
            RefreshOpponents(match);
            RefreshBoard(match, animateBoard);
            RefreshTurnInstruction(match);
            RefreshPlayerHand(match);
            RefreshActionButtons(match);
        }

        private void RefreshTopBar(DominoMatchManager match)
        {
            if (roundInfoLabel != null)
            {
                int roundNum = match.RoundManager != null ? match.RoundManager.RoundNumber : 1;
                roundInfoLabel.text = $"ROUND {Math.Max(1, roundNum)}";
            }
        }

        private void RefreshOpponents(DominoMatchManager match)
        {
            var players = match.GameState.Players;
            var currentPlayer = match.TurnManager.GetCurrentPlayer(players);

            int oppIndex = 0;
            foreach (var player in players)
            {
                if (player.IsHuman) continue;
                if (oppIndex >= 3) break;

                var slot = opponentSlots[oppIndex];
                if (slot != null)
                {
                    slot.style.display = DisplayStyle.Flex;

                    bool isThisBotTurn = (currentPlayer == player);
                    if (isThisBotTurn) slot.AddToClassList("opponent-card--active");
                    else slot.RemoveFromClassList("opponent-card--active");

                    if (opponentActiveIndicators[oppIndex] != null)
                    {
                        opponentActiveIndicators[oppIndex].style.display = isThisBotTurn ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    if (opponentNames[oppIndex] != null)
                    {
                        opponentNames[oppIndex].text = player.PlayerName;
                    }

                    if (opponentTileCounts[oppIndex] != null)
                    {
                        opponentTileCounts[oppIndex].text = $"{player.HandCount} tiles";
                    }
                }

                oppIndex++;
            }

            for (int i = oppIndex; i < 3; i++)
            {
                if (opponentSlots[i] != null)
                {
                    opponentSlots[i].style.display = DisplayStyle.None;
                }
            }
        }

        private void RefreshBoard(DominoMatchManager match, bool animate = false)
        {
            var board = match.Board;
            if (boardTilesContainer == null) return;

            boardTilesContainer.Clear();

            // Refresh Boneyard Stack Badge
            int boneyardCount = match.Dealer != null ? match.Dealer.RemainingCount : 0;
            if (boneyardCountLabel != null)
            {
                boneyardCountLabel.text = $"{boneyardCount}";
            }

            if (board.IsEmpty)
            {
                if (emptyBoardMessage != null)
                {
                    emptyBoardMessage.style.display = DisplayStyle.Flex;
                    boardTilesContainer.Add(emptyBoardMessage);
                }
                if (leftEndTarget != null) leftEndTarget.style.display = DisplayStyle.None;
                if (rightEndTarget != null) rightEndTarget.style.display = DisplayStyle.None;
                return;
            }

            if (emptyBoardMessage != null) emptyBoardMessage.style.display = DisplayStyle.None;

            float containerW = boardTilesContainer.layout.width;
            float containerH = boardTilesContainer.layout.height;

            if (containerW <= 10f || containerH <= 10f)
            {
                containerW = 340f;
                containerH = 280f;
            }

            // Calculate non-overlapping 2D layout via DominoBoardLayoutEngine
            lastLayoutResult = DominoBoardLayoutEngine.CalculateLayout(
                board.PlacedTiles,
                board.LeftEndpoint,
                board.RightEndpoint,
                containerW,
                containerH
            );

            // Render 2D physical ivory dominoes
            var visualElements = new List<(VisualElement el, Vector2 startPos, Vector2 targetPos)>();

            for (int i = 0; i < lastLayoutResult.Placements.Count; i++)
            {
                var placement = lastLayoutResult.Placements[i];
                var tileEl = Create2DBoardTileVisual(placement);
                boardTilesContainer.Add(tileEl);

                Vector2 targetPos = placement.Position;
                Vector2 startPos = targetPos;

                if (animate && previousTilePositions.TryGetValue(placement.Tile, out var prevPos))
                {
                    startPos = prevPos;
                }

                visualElements.Add((tileEl, startPos, targetPos));
                previousTilePositions[placement.Tile] = targetPos;
            }

            if (animate && visualElements.Count > 0)
            {
                if (boardAnimationCoroutine != null) StopCoroutine(boardAnimationCoroutine);
                boardAnimationCoroutine = StartCoroutine(AnimateBoardTilesSmoothly(visualElements));
            }

            // Update on-board drop zones
            var currentPlayer = match.TurnManager.GetCurrentPlayer(match.GameState.Players);
            bool isHumanTurn = currentPlayer != null && currentPlayer.IsHuman;

            DominoTile testTile = isDragging ? draggingTile : selectedTile;
            bool canLeft = false;
            bool canRight = false;

            if (isHumanTurn && testTile != null)
            {
                canLeft = board.IsEmpty || board.CanConnectLeft(testTile);
                canRight = board.IsEmpty || board.CanConnectRight(testTile);
            }

            if (leftEndTarget != null)
            {
                if (testTile != null && canLeft && lastLayoutResult != null)
                {
                    leftEndTarget.style.display = DisplayStyle.Flex;
                    leftEndTarget.style.left = lastLayoutResult.LeftEndpoint.Position.x - 19f;
                    leftEndTarget.style.top = lastLayoutResult.LeftEndpoint.Position.y - 19f;
                    if (leftTargetNum != null) leftTargetNum.text = board.IsEmpty ? "Any" : $"{board.LeftEndpoint}";
                }
                else
                {
                    leftEndTarget.style.display = DisplayStyle.None;
                }
            }

            if (rightEndTarget != null)
            {
                if (testTile != null && canRight && !board.IsEmpty && lastLayoutResult != null)
                {
                    rightEndTarget.style.display = DisplayStyle.Flex;
                    rightEndTarget.style.left = lastLayoutResult.RightEndpoint.Position.x - 19f;
                    rightEndTarget.style.top = lastLayoutResult.RightEndpoint.Position.y - 19f;
                    if (rightTargetNum != null) rightTargetNum.text = $"{board.RightEndpoint}";
                }
                else
                {
                    rightEndTarget.style.display = DisplayStyle.None;
                }
            }
        }

        private IEnumerator AnimateBoardTilesSmoothly(List<(VisualElement el, Vector2 startPos, Vector2 targetPos)> tiles)
        {
            float duration = 0.28f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                foreach (var item in tiles)
                {
                    if (item.el != null)
                    {
                        Vector2 currentPos = Vector2.Lerp(item.startPos, item.targetPos, t);
                        item.el.style.left = currentPos.x;
                        item.el.style.top = currentPos.y;
                    }
                }

                yield return null;
            }

            foreach (var item in tiles)
            {
                if (item.el != null)
                {
                    item.el.style.left = item.targetPos.x;
                    item.el.style.top = item.targetPos.y;
                }
            }

            boardAnimationCoroutine = null;
        }

        private void RefreshTurnInstruction(DominoMatchManager match)
        {
            var currentPlayer = match.TurnManager.GetCurrentPlayer(match.GameState.Players);
            bool isHumanTurn = currentPlayer != null && currentPlayer.IsHuman;

            // Trigger turn chime if it just became human turn
            if (isHumanTurn && lastActivePlayer != currentPlayer)
            {
                DominoAudioManager.Instance?.PlayTurnChime();
                DominoHapticsManager.TriggerLightTap();
            }
            lastActivePlayer = currentPlayer;

            if (turnGuidanceBar != null)
            {
                if (isHumanTurn) turnGuidanceBar.AddToClassList("turn-guidance-bar--human");
                else turnGuidanceBar.RemoveFromClassList("turn-guidance-bar--human");
            }

            if (turnStatusDot != null)
            {
                if (isHumanTurn) turnStatusDot.RemoveFromClassList("turn-status-dot--bot");
                else turnStatusDot.AddToClassList("turn-status-dot--bot");
            }

            if (!isHumanTurn)
            {
                string botName = currentPlayer?.PlayerName ?? "Opponent";
                if (turnTitleLabel != null) turnTitleLabel.text = $"{botName.ToUpperInvariant()}'S TURN";
                if (turnSubtitleLabel != null) turnSubtitleLabel.text = $"{botName} is thinking...";
            }
            else if (selectedTile != null || isDragging)
            {
                var activeTile = isDragging ? draggingTile : selectedTile;
                if (turnTitleLabel != null) turnTitleLabel.text = "PLACE YOUR TILE";
                if (turnSubtitleLabel != null) turnSubtitleLabel.text = $"Drag [{activeTile?.Left}|{activeTile?.Right}] onto matching board end";
            }
            else
            {
                bool hasPlayable = passDrawManager.HasPlayableTile(match.Board, currentPlayer);
                if (turnTitleLabel != null) turnTitleLabel.text = "YOUR TURN";
                if (turnSubtitleLabel != null)
                {
                    turnSubtitleLabel.text = hasPlayable
                        ? "Drag a highlighted domino from your hand"
                        : "No matching dominoes — Tap Boneyard stack to Draw";
                }
            }
        }

        private void RefreshPlayerHand(DominoMatchManager match)
        {
            var players = match.GameState.Players;
            DominoPlayer human = null;
            foreach (var p in players)
            {
                if (p.IsHuman) { human = p; break; }
            }

            if (human == null || handTilesContainer == null) return;

            int tileCount = human.HandCount;

            if (handTileCountLabel != null)
            {
                handTileCountLabel.text = $"{tileCount} TILES";
            }

            handTilesContainer.Clear();
            var currentPlayer = match.TurnManager.GetCurrentPlayer(players);
            bool isHumanTurn = (currentPlayer == human);

            bool isTutorialActive = tutorialController != null && tutorialController.IsActive;
            DominoTile tutorialTarget = tutorialController != null ? tutorialController.TargetPlayableTile : null;

            // Responsive zoom: compact at 8-10 tiles, mini at 11+ tiles
            bool useCompact = tileCount >= 8 && tileCount < 11;
            bool useMini    = tileCount >= 11;

            foreach (var tile in human.Hand)
            {
                bool isPlayable = isHumanTurn && DominoMoveValidator.CanPlaceAnywhere(match.Board, tile, out _);
                bool isSelected = (tile == selectedTile);
                bool isTutorialHighlight = isTutorialActive && (tile == tutorialTarget || (tutorialTarget == null && isPlayable));

                var tileEl = CreateHandTileVisual(tile, isPlayable, isSelected, isTutorialActive, isTutorialHighlight);

                // Apply responsive zoom class for smooth scale animation
                if (useMini)
                    tileEl.AddToClassList("domino-tile-stand--mini");
                else if (useCompact)
                    tileEl.AddToClassList("domino-tile-stand--compact");

                handTilesContainer.Add(tileEl);
            }
        }

        private void RefreshActionButtons(DominoMatchManager match)
        {
            var players = match.GameState.Players;
            var currentPlayer = match.TurnManager.GetCurrentPlayer(players);
            bool isHumanTurn = currentPlayer != null && currentPlayer.IsHuman;

            if (humanPlayerExists(match, out var human))
            {
                bool hasPlayable = passDrawManager.HasPlayableTile(match.Board, human);
                int boneyardCount = match.Dealer != null ? match.Dealer.RemainingCount : 0;

                // Boneyard Draw glow
                bool mustDraw = isHumanTurn && !hasPlayable && boneyardCount > 0;
                if (boneyardPileBtn != null)
                {
                    if (mustDraw)
                    {
                        boneyardPileBtn.AddToClassList("boneyard-stack-btn--glowing");
                        if (boneyardActionHint != null) boneyardActionHint.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        boneyardPileBtn.RemoveFromClassList("boneyard-stack-btn--glowing");
                        if (boneyardActionHint != null) boneyardActionHint.style.display = DisplayStyle.None;
                    }
                }

                // Pass Button (Visible & enabled ONLY when pass is required)
                bool canPass = isHumanTurn && passDrawManager.CanPass(match.Board, human, boneyardCount);
                if (passButton != null)
                {
                    passButton.style.visibility = canPass ? Visibility.Visible : Visibility.Hidden;
                    passButton.SetEnabled(canPass);
                }

                // Hint Button
                if (hintButton != null)
                {
                    hintButton.SetEnabled(isHumanTurn && hasPlayable);
                }
            }
        }

        private bool humanPlayerExists(DominoMatchManager match, out DominoPlayer human)
        {
            human = null;
            if (match.GameState.Players == null) return false;
            foreach (var p in match.GameState.Players)
            {
                if (p.IsHuman) { human = p; return true; }
            }
            return false;
        }

        #region Visual Builders (Skeuomorphic Ivory Dominoes)

        private VisualElement Create2DBoardTileVisual(DominoVisualPlacement placement)
        {
            var el = new VisualElement();
            el.AddToClassList("domino-tile-board-2d");
            el.AddToClassList(placement.IsVertical ? "domino-tile-board-2d--vertical" : "domino-tile-board-2d--horizontal");

            el.style.position = Position.Absolute;
            el.style.left = placement.Position.x;
            el.style.top = placement.Position.y;
            el.style.width = placement.Size.x;
            el.style.height = placement.Size.y;

            var firstHalf = new VisualElement();
            firstHalf.AddToClassList("domino-half-board-2d");
            firstHalf.Add(CreatePipMatrix3x3(placement.FirstFace, isHand: false));

            var divider = new VisualElement();
            divider.AddToClassList(placement.IsVertical ? "domino-divider-board-v" : "domino-divider-board-h");

            var secondHalf = new VisualElement();
            secondHalf.AddToClassList("domino-half-board-2d");
            secondHalf.Add(CreatePipMatrix3x3(placement.SecondFace, isHand: false));

            el.Add(firstHalf);
            el.Add(divider);
            el.Add(secondHalf);
            return el;
        }

        private VisualElement CreateHandTileVisual(DominoTile tile, bool isPlayable, bool isSelected, bool isTutorialActive = false, bool isTutorialTarget = false)
        {
            var el = new VisualElement();
            el.AddToClassList("domino-tile-hand");

            if (isPlayable) el.AddToClassList("domino-tile-hand--playable");
            else el.AddToClassList("domino-tile-hand--unplayable");

            if (isSelected) el.AddToClassList("domino-tile-hand--selected");

            if (isTutorialActive)
            {
                if (isTutorialTarget) el.AddToClassList("domino-tile-hand--tutorial-target");
                else if (!isSelected) el.AddToClassList("domino-tile-hand--dimmed");
            }

            var topHalf = new VisualElement();
            topHalf.AddToClassList("domino-half-hand");
            topHalf.Add(CreatePipMatrix3x3(tile.Left, isHand: true));

            var divider = new VisualElement();
            divider.AddToClassList("domino-divider-hand");

            var bottomHalf = new VisualElement();
            bottomHalf.AddToClassList("domino-half-hand");
            bottomHalf.Add(CreatePipMatrix3x3(tile.Right, isHand: true));

            el.Add(topHalf);
            el.Add(divider);
            el.Add(bottomHalf);

            // Register Pointer Down for Drag & Drop
            el.RegisterCallback<PointerDownEvent>(evt =>
            {
                OnHandTilePointerDown(evt, tile, isPlayable);
            });

            return el;
        }

        private VisualElement CreatePipMatrix3x3(int count, bool isHand)
        {
            var matrix = new VisualElement();
            matrix.AddToClassList(isHand ? "pip-matrix-3x3" : "pip-matrix-3x3-board");

            bool[,] pips = new bool[3, 3];
            switch (count)
            {
                case 1: pips[1, 1] = true; break;
                case 2: pips[0, 0] = true; pips[2, 2] = true; break;
                case 3: pips[0, 0] = true; pips[1, 1] = true; pips[2, 2] = true; break;
                case 4: pips[0, 0] = true; pips[0, 2] = true; pips[2, 0] = true; pips[2, 2] = true; break;
                case 5: pips[0, 0] = true; pips[0, 2] = true; pips[1, 1] = true; pips[2, 0] = true; pips[2, 2] = true; break;
                case 6: pips[0, 0] = true; pips[0, 2] = true; pips[1, 0] = true; pips[1, 2] = true; pips[2, 0] = true; pips[2, 2] = true; break;
            }

            for (int r = 0; r < 3; r++)
            {
                var row = new VisualElement();
                row.AddToClassList(isHand ? "pip-row" : "pip-row-board");

                for (int c = 0; c < 3; c++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList(isHand ? "pip-cell" : "pip-cell-board");

                    if (pips[r, c])
                    {
                        var dot = new VisualElement();
                        dot.AddToClassList(isHand ? "pip-dot-hand" : "pip-dot-board");
                        cell.Add(dot);
                    }

                    row.Add(cell);
                }

                matrix.Add(row);
            }

            return matrix;
        }

        #endregion

        #region Drag & Drop and Tap Placement

        private void OnHandTilePointerDown(PointerDownEvent evt, DominoTile tile, bool isPlayable)
        {
            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;
            var currentPlayer = match.TurnManager.GetCurrentPlayer(match.GameState.Players);

            if (currentPlayer == null || !currentPlayer.IsHuman)
            {
                SetInstruction("Please wait for your turn.");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            if (!isPlayable)
            {
                SetInstruction($"[{tile.Left}|{tile.Right}] cannot match board ends ({match.Board.LeftEndpoint} or {match.Board.RightEndpoint}).");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            // Audio & Haptic pickup feedback
            DominoAudioManager.Instance?.PlayTilePickup();
            DominoHapticsManager.TriggerLightTap();

            // Tutorial interception
            if (tutorialController != null && tutorialController.IsActive)
            {
                if (!tutorialController.InterceptHandTileClick(tile, isPlayable)) return;
            }

            // Start Dragging
            isDragging = true;
            draggingTile = tile;
            selectedTile = tile;
            dragStartPosition = evt.position;

            if (dragGhostTile != null)
            {
                dragGhostTile.Clear();
                var topHalf = new VisualElement();
                topHalf.AddToClassList("domino-half-hand");
                topHalf.Add(CreatePipMatrix3x3(tile.Left, isHand: true));

                var divider = new VisualElement();
                divider.AddToClassList("domino-divider-hand");

                var bottomHalf = new VisualElement();
                bottomHalf.AddToClassList("domino-half-hand");
                bottomHalf.Add(CreatePipMatrix3x3(tile.Right, isHand: true));

                dragGhostTile.Add(topHalf);
                dragGhostTile.Add(divider);
                dragGhostTile.Add(bottomHalf);

                dragGhostTile.style.display = DisplayStyle.Flex;
                UpdateDragGhostPosition(evt.position);
            }

            RefreshAll(animateBoard: false);
        }

        private void OnRootPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging || draggingTile == null) return;

            UpdateDragGhostPosition(evt.position);

            // Test proximity to left/right endpoint drop zones
            if (boardTilesContainer != null && lastLayoutResult != null)
            {
                Vector2 boardLocalPos = boardTilesContainer.WorldToLocal(evt.position);

                bool nearLeft = Vector2.Distance(boardLocalPos, lastLayoutResult.LeftEndpoint.Position) < 65f;
                bool nearRight = Vector2.Distance(boardLocalPos, lastLayoutResult.RightEndpoint.Position) < 65f;

                if (leftEndTarget != null)
                {
                    if (nearLeft) leftEndTarget.AddToClassList("board-endpoint-dropzone--highlighted");
                    else leftEndTarget.RemoveFromClassList("board-endpoint-dropzone--highlighted");
                }

                if (rightEndTarget != null)
                {
                    if (nearRight) rightEndTarget.AddToClassList("board-endpoint-dropzone--highlighted");
                    else rightEndTarget.RemoveFromClassList("board-endpoint-dropzone--highlighted");
                }
            }
        }

        private void OnRootPointerUp(PointerUpEvent evt)
        {
            if (!isDragging || draggingTile == null) return;

            var tile = draggingTile;
            CancelDragging();

            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;

            // Empty board drop anywhere inside board
            if (match.Board.IsEmpty && boardFelt != null)
            {
                Vector2 feltLocalPos = boardFelt.WorldToLocal(evt.position);
                if (feltLocalPos.x >= 0 && feltLocalPos.x <= boardFelt.layout.width &&
                    feltLocalPos.y >= 0 && feltLocalPos.y <= boardFelt.layout.height)
                {
                    ExecutePlacement(tile, BoardSide.Left);
                    return;
                }
            }

            // Test drop near Left or Right Endpoint
            if (boardTilesContainer != null && lastLayoutResult != null)
            {
                Vector2 boardLocalPos = boardTilesContainer.WorldToLocal(evt.position);

                bool canLeft = match.Board.CanConnectLeft(tile);
                bool canRight = match.Board.CanConnectRight(tile);

                float distLeft = Vector2.Distance(boardLocalPos, lastLayoutResult.LeftEndpoint.Position);
                float distRight = Vector2.Distance(boardLocalPos, lastLayoutResult.RightEndpoint.Position);

                if (canLeft && distLeft < 70f)
                {
                    if (tutorialController != null && tutorialController.IsActive)
                    {
                        if (!tutorialController.InterceptEndpointClick(BoardSide.Left, true)) return;
                    }

                    ExecutePlacement(tile, BoardSide.Left);
                    return;
                }

                if (canRight && distRight < 70f)
                {
                    if (tutorialController != null && tutorialController.IsActive)
                    {
                        if (!tutorialController.InterceptEndpointClick(BoardSide.Right, true)) return;
                    }

                    ExecutePlacement(tile, BoardSide.Right);
                    return;
                }
            }

            // If tile was barely dragged (simple tap), keep it selected so user can tap endpoint
            if (Vector2.Distance(evt.position, dragStartPosition) < 15f)
            {
                bool canLeft = match.Board.IsEmpty || match.Board.CanConnectLeft(tile);
                bool canRight = !match.Board.IsEmpty && match.Board.CanConnectRight(tile);

                if (canLeft && !canRight)
                {
                    ExecutePlacement(tile, BoardSide.Left);
                    return;
                }
                else if (!canLeft && canRight)
                {
                    ExecutePlacement(tile, BoardSide.Right);
                    return;
                }
            }

            RefreshAll(animateBoard: false);
        }

        private void OnRootPointerCancel(PointerCancelEvent evt)
        {
            CancelDragging();
            RefreshAll(animateBoard: false);
        }

        private void CancelDragging()
        {
            isDragging = false;
            draggingTile = null;

            if (dragGhostTile != null)
            {
                dragGhostTile.style.display = DisplayStyle.None;
            }

            if (leftEndTarget != null) leftEndTarget.RemoveFromClassList("board-endpoint-dropzone--highlighted");
            if (rightEndTarget != null) rightEndTarget.RemoveFromClassList("board-endpoint-dropzone--highlighted");
        }

        private void UpdateDragGhostPosition(Vector2 pointerPos)
        {
            if (dragGhostTile == null || rootElement == null) return;
            Vector2 rootPos = rootElement.WorldToLocal(pointerPos);
            dragGhostTile.style.left = rootPos.x - 22f;
            dragGhostTile.style.top = rootPos.y - 40f;
        }

        private void OnPlaceEndpointClicked(BoardSide side)
        {
            DominoAudioManager.Instance?.PlayClick();

            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;
            var human = match.TurnManager.GetCurrentPlayer(match.GameState.Players);

            if (human == null || !human.IsHuman) return;

            if (selectedTile == null)
            {
                SetInstruction("Drag or select a highlighted domino first!");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            bool canConnect = (side == BoardSide.Left)
                ? (match.Board.IsEmpty || match.Board.CanConnectLeft(selectedTile))
                : (match.Board.IsEmpty || match.Board.CanConnectRight(selectedTile));

            if (tutorialController != null && tutorialController.IsActive)
            {
                if (!tutorialController.InterceptEndpointClick(side, canConnect)) return;
            }

            if (!canConnect)
            {
                int endVal = (side == BoardSide.Left) ? match.Board.LeftEndpoint : match.Board.RightEndpoint;
                SetInstruction($"Cannot place [{selectedTile.Left}|{selectedTile.Right}] on {side} end ({endVal}).");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            ExecutePlacement(selectedTile, side);
        }

        public void ExecutePlacement(DominoTile tile, BoardSide side)
        {
            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;
            var human = match.TurnManager.GetCurrentPlayer(match.GameState.Players);

            if (human == null || !human.IsHuman) return;

            var placement = placementManager.PlaceTile(match.Board, human, tile, side);
            if (placement.Success)
            {
                selectedTile = null;

                // Play tactile placement clack and haptic pulse
                DominoAudioManager.Instance?.PlayTileClack();
                DominoHapticsManager.TriggerMediumPulse();

                if (tutorialController != null && tutorialController.IsActive)
                {
                    tutorialController.NotifyMoveExecuted();
                }

                var completion = match.CompletionManager.CheckGameCompletion(match.GameState, match.Board, match.Dealer, passDrawManager);
                if (completion.IsGameOver)
                {
                    match.GameState.SetState(MatchState.Finished);
                    ShowResultModal();
                    return;
                }

                match.TurnManager.NextTurn(match.GameState);

                RefreshAll(animateBoard: true);
                CheckBotTurn();
            }
            else
            {
                SetInstruction($"Cannot place: {placement.Message}");
                DominoHapticsManager.TriggerWarningBuzz();
            }
        }

        #endregion

        #region Boneyard, Hint, and Pass Actions

        private void OnBoneyardClicked()
        {
            DominoAudioManager.Instance?.PlayClick();

            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;
            var human = match.TurnManager.GetCurrentPlayer(match.GameState.Players);

            if (human == null || !human.IsHuman) return;

            if (passDrawManager.HasPlayableTile(match.Board, human))
            {
                SetInstruction("You already hold a matching domino in your hand! Drag it to play.");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            if (match.Dealer == null || match.Dealer.RemainingCount == 0)
            {
                SetInstruction("The boneyard is empty! Tap PASS TURN to skip your turn.");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            if (tutorialController != null && tutorialController.IsActive)
            {
                tutorialController.InterceptDrawClick();
            }

            var result = passDrawManager.ExecuteDraw(match.Board, human, match.Dealer);
            if (result.Success && result.DrawnTile != null)
            {
                selectedTile = null;
                DominoAudioManager.Instance?.PlayTileDraw();
                DominoHapticsManager.TriggerLightTap();

                bool isDrawnPlayable = DominoMoveValidator.CanPlaceAnywhere(match.Board, result.DrawnTile, out _);
                if (isDrawnPlayable)
                {
                    selectedTile = result.DrawnTile;
                    SetInstruction($"Drew matching tile [{result.DrawnTile.Left}|{result.DrawnTile.Right}]! Drag it onto the board.");
                }
                else
                {
                    SetInstruction($"Drew [{result.DrawnTile.Left}|{result.DrawnTile.Right}]. (Still no match — Draw again or Pass if empty)");
                }
                RefreshAll(animateBoard: false);
            }
            else
            {
                SetInstruction(result.Message);
            }
        }

        private void OnHintButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();

            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;
            var human = match.TurnManager.GetCurrentPlayer(match.GameState.Players);

            if (human == null || !human.IsHuman)
            {
                SetInstruction("Wait for your turn to receive a hint.");
                return;
            }

            DominoTile recommendedTile = null;
            BoardSide recommendedSide = BoardSide.Left;

            foreach (var tile in human.Hand)
            {
                if (DominoMoveValidator.CanPlaceAnywhere(match.Board, tile, out var side))
                {
                    recommendedTile = tile;
                    recommendedSide = side;
                    break;
                }
            }

            if (recommendedTile != null)
            {
                selectedTile = recommendedTile;
                DominoHapticsManager.TriggerLightTap();
                SetInstruction($"💡 HINT: Try dragging [{recommendedTile.Left}|{recommendedTile.Right}] to the {recommendedSide} end!");
                RefreshAll(animateBoard: false);
            }
            else
            {
                int boneyardCount = match.Dealer != null ? match.Dealer.RemainingCount : 0;
                if (boneyardCount > 0)
                {
                    SetInstruction("💡 HINT: No matching tiles in hand. Tap the Boneyard stack to draw!");
                }
                else
                {
                    SetInstruction("💡 HINT: No matching tiles and boneyard is empty. Tap PASS TURN.");
                }
            }
        }

        private void OnPassButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();

            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;
            var human = match.TurnManager.GetCurrentPlayer(match.GameState.Players);

            if (human == null || !human.IsHuman) return;

            if (passDrawManager.HasPlayableTile(match.Board, human))
            {
                SetInstruction("Cannot pass! You hold a matching domino in your hand.");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            int boneyardCount = match.Dealer != null ? match.Dealer.RemainingCount : 0;
            if (boneyardCount > 0)
            {
                SetInstruction($"Cannot pass yet! Draw from the boneyard ({boneyardCount} remaining).");
                DominoHapticsManager.TriggerWarningBuzz();
                return;
            }

            if (tutorialController != null && tutorialController.IsActive)
            {
                tutorialController.InterceptPassClick();
            }

            var result = passDrawManager.ExecutePass(match.Board, human, match.Dealer);
            if (result.Success)
            {
                selectedTile = null;
                DominoAudioManager.Instance?.PlayPass();
                SetInstruction("You passed your turn.");

                var completion = match.CompletionManager.CheckGameCompletion(match.GameState, match.Board, match.Dealer, passDrawManager);
                if (completion.IsGameOver)
                {
                    match.GameState.SetState(MatchState.Finished);
                    RefreshAll(animateBoard: true);
                    ShowResultModal();
                    return;
                }

                match.TurnManager.NextTurn(match.GameState);
                RefreshAll(animateBoard: false);
                CheckBotTurn();
            }
            else
            {
                SetInstruction(result.Message);
            }
        }

        #endregion

        #region Bot Turn Handling

        private void CheckBotTurn()
        {
            if (waitingScreenController == null) return;
            var match = waitingScreenController.MatchManager;
            if (match.CurrentState != MatchState.Playing) return;

            var currentPlayer = match.TurnManager.GetCurrentPlayer(match.GameState.Players);
            if (currentPlayer != null && !currentPlayer.IsHuman)
            {
                if (botTurnCoroutine != null) StopCoroutine(botTurnCoroutine);
                botTurnCoroutine = StartCoroutine(SimulateBotTurnCoroutine(currentPlayer));
            }
        }

        private IEnumerator SimulateBotTurnCoroutine(DominoPlayer bot)
        {
            // Natural randomized thinking delay (0.8s to 1.5s)
            float thinkDelay = UnityEngine.Random.Range(0.85f, 1.45f);
            yield return new WaitForSeconds(thinkDelay);

            if (waitingScreenController == null) yield break;
            var match = waitingScreenController.MatchManager;
            if (match.CurrentState != MatchState.Playing) yield break;

            // 1. If bot has no playable tile, bot draws from boneyard until playable or empty
            while (!passDrawManager.HasPlayableTile(match.Board, bot) && match.Dealer != null && match.Dealer.RemainingCount > 0)
            {
                var drawRes = passDrawManager.ExecuteDraw(match.Board, bot, match.Dealer);
                if (drawRes.Success)
                {
                    DominoAudioManager.Instance?.PlayTileDraw();
                    SetInstruction($"{bot.PlayerName} drew a tile from the boneyard.");
                    RefreshAll(animateBoard: false);
                    yield return new WaitForSeconds(0.7f);
                }
                else
                {
                    break;
                }
            }

            // 2. Look for playable tile in bot's hand
            DominoTile playedTile = null;
            BoardSide playedSide = BoardSide.Left;

            foreach (var tile in bot.Hand)
            {
                if (match.Board.IsEmpty || match.Board.CanConnectLeft(tile))
                {
                    playedTile = tile;
                    playedSide = BoardSide.Left;
                    break;
                }
                if (match.Board.CanConnectRight(tile))
                {
                    playedTile = tile;
                    playedSide = BoardSide.Right;
                    break;
                }
            }

            if (playedTile != null)
            {
                var placement = placementManager.PlaceTile(match.Board, bot, playedTile, playedSide);
                if (placement.Success)
                {
                    DominoAudioManager.Instance?.PlayTileClack();
                    SetInstruction($"{bot.PlayerName} played [{playedTile.Left}|{playedTile.Right}].");
                }
            }
            else
            {
                passDrawManager.ExecutePass(match.Board, bot, match.Dealer);
                DominoAudioManager.Instance?.PlayPass();
                SetInstruction($"{bot.PlayerName} passed turn.");
            }

            // 3. Check Game Completion
            var completion = match.CompletionManager.CheckGameCompletion(match.GameState, match.Board, match.Dealer, passDrawManager);
            if (completion.IsGameOver)
            {
                match.GameState.SetState(MatchState.Finished);
                RefreshAll(animateBoard: true);
                ShowResultModal();
                yield break;
            }

            // 4. Advance Turn
            match.TurnManager.NextTurn(match.GameState);
            RefreshAll(animateBoard: true);

            if (tutorialController != null && tutorialController.IsActive)
            {
                tutorialController.NotifyBotTurnEnded();
            }

            CheckBotTurn();
        }

        #endregion

        #region Settings & Modals Navigation

        private void OnSettingsButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            ShowSettingsModal();
        }

        public void ShowSettingsModal()
        {
            if (settingsModal != null)
            {
                UpdateSettingsUI();
                settingsModal.style.display = DisplayStyle.Flex;
            }
        }

        public void HideSettingsModal()
        {
            DominoAudioManager.Instance?.PlayClick();
            if (settingsModal != null)
            {
                settingsModal.style.display = DisplayStyle.None;
            }
        }

        private void UpdateSettingsUI()
        {
            bool musicOn = DominoAudioManager.Instance == null || !DominoAudioManager.Instance.IsMusicMuted;
            bool sfxOn = DominoAudioManager.Instance == null || !DominoAudioManager.Instance.IsSfxMuted;
            bool hapticsOn = DominoHapticsManager.IsHapticsEnabled;

            if (settingsMusicToggleBtn != null)
            {
                if (musicOn) { settingsMusicToggleBtn.AddToClassList("settings-toggle-btn--on"); settingsMusicToggleBtn.RemoveFromClassList("settings-toggle-btn--off"); }
                else { settingsMusicToggleBtn.RemoveFromClassList("settings-toggle-btn--on"); settingsMusicToggleBtn.AddToClassList("settings-toggle-btn--off"); }
            }
            if (settingsMusicToggleText != null) settingsMusicToggleText.text = musicOn ? "ON" : "OFF";

            if (settingsSfxToggleBtn != null)
            {
                if (sfxOn) { settingsSfxToggleBtn.AddToClassList("settings-toggle-btn--on"); settingsSfxToggleBtn.RemoveFromClassList("settings-toggle-btn--off"); }
                else { settingsSfxToggleBtn.RemoveFromClassList("settings-toggle-btn--on"); settingsSfxToggleBtn.AddToClassList("settings-toggle-btn--off"); }
            }
            if (settingsSfxToggleText != null) settingsSfxToggleText.text = sfxOn ? "ON" : "OFF";

            if (settingsHapticsToggleBtn != null)
            {
                if (hapticsOn) { settingsHapticsToggleBtn.AddToClassList("settings-toggle-btn--on"); settingsHapticsToggleBtn.RemoveFromClassList("settings-toggle-btn--off"); }
                else { settingsHapticsToggleBtn.RemoveFromClassList("settings-toggle-btn--on"); settingsHapticsToggleBtn.AddToClassList("settings-toggle-btn--off"); }
            }
            if (settingsHapticsToggleText != null) settingsHapticsToggleText.text = hapticsOn ? "ON" : "OFF";
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

        private void OnHelpButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            ShowHelpModal();
        }

        public void ShowHelpModal()
        {
            if (helpRulesModal != null)
            {
                helpRulesModal.style.display = DisplayStyle.Flex;
            }
        }

        public void HideHelpModal()
        {
            DominoAudioManager.Instance?.PlayClick();
            if (helpRulesModal != null)
            {
                helpRulesModal.style.display = DisplayStyle.None;
            }
        }

        private void OnReplayTutorialClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideSettingsModal();
            HideHelpModal();
            if (tutorialController != null)
            {
                tutorialController.StartTutorial(force: true);
            }
        }

        private void ShowResultModal()
        {
            if (resultModal != null)
            {
                resultModal.style.display = DisplayStyle.Flex;

                if (waitingScreenController != null && waitingScreenController.MatchManager != null)
                {
                    var match = waitingScreenController.MatchManager;
                    var completion = match.CompletionManager.CheckGameCompletion(match.GameState, match.Board, match.Dealer, passDrawManager);

                    bool isHumanWinner = completion.Winner != null && completion.Winner.IsHuman;

                    if (isHumanWinner)
                    {
                        DominoAudioManager.Instance?.PlayWin();
                        DominoHapticsManager.TriggerWinCelebration();
                    }
                    else
                    {
                        DominoAudioManager.Instance?.PlayLoss();
                    }

                    if (resultTitle != null)
                    {
                        resultTitle.text = isHumanWinner ? "YOU WIN! 🏆" : "GOOD GAME!";
                    }

                    if (resultWinnerLabel != null)
                    {
                        resultWinnerLabel.text = isHumanWinner
                            ? "Excellent Play!"
                            : (completion.Winner != null ? $"Winner: {completion.Winner.PlayerName} — Better luck next round" : "Blocked Game!");
                    }

                    // Score Count-Up Animation
                    int targetScore = isHumanWinner ? 125 : 87;
                    if (animatedScoreLabel != null)
                    {
                        if (scoreCountCoroutine != null) StopCoroutine(scoreCountCoroutine);
                        scoreCountCoroutine = StartCoroutine(AnimateScoreCountUp(targetScore));
                    }

                    if (resultScoresList != null)
                    {
                        resultScoresList.Clear();
                        foreach (var player in match.GameState.Players)
                        {
                            var row = new VisualElement();
                            row.AddToClassList("result-score-row");

                            var nameLbl = new Label(player.PlayerName);
                            nameLbl.AddToClassList("result-score-name");

                            var valLbl = new Label($"{player.HandCount} tiles remaining");
                            valLbl.AddToClassList("result-score-val");

                            row.Add(nameLbl);
                            row.Add(valLbl);
                            resultScoresList.Add(row);
                        }
                    }
                }
            }
        }

        private IEnumerator AnimateScoreCountUp(int targetScore)
        {
            animatedScoreLabel.text = "+0";
            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int current = (int)Mathf.Lerp(0, targetScore, elapsed / duration);
                animatedScoreLabel.text = $"+{current}";
                yield return null;
            }

            animatedScoreLabel.text = $"+{targetScore}";
            scoreCountCoroutine = null;
        }

        private void OnMenuButtonClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
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
            if (leaveConfirmModal != null)
            {
                leaveConfirmModal.style.display = DisplayStyle.None;
            }
        }

        private void OnConfirmLeaveClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideLeaveConfirmation();
            OnExitHomeClicked();
        }

        private void OnPlayAgainClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideGameScreen();
            if (waitingScreenController != null)
            {
                waitingScreenController.OnPlayDominoesClicked();
            }
        }

        private void OnExitHomeClicked()
        {
            DominoAudioManager.Instance?.PlayClick();
            HideGameScreen();
            if (waitingScreenController != null)
            {
                waitingScreenController.OnLeaveWaitingClicked();
            }
            if (homeScreenController != null)
            {
                homeScreenController.ShowHomeScreen();
            }
        }

        public void SetInstruction(string message)
        {
            if (turnSubtitleLabel != null)
            {
                turnSubtitleLabel.text = message;
            }
        }

        public void ShowGameScreen()
        {
            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("game-root") ?? uiDocument.rootVisualElement;
                safeContent = rootElement.Q<VisualElement>("safe-content") ?? rootElement;
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.Flex;
                ApplySafeArea();
            }

            if (resultModal != null)
            {
                resultModal.style.display = DisplayStyle.None;
            }

            if (settingsModal != null)
            {
                settingsModal.style.display = DisplayStyle.None;
            }

            if (helpRulesModal != null)
            {
                helpRulesModal.style.display = DisplayStyle.None;
            }

            if (homeScreenController != null) homeScreenController.HideHomeScreen();
            if (waitingScreenUIToolkitController != null) waitingScreenUIToolkitController.HideWaitingScreen();
        }

        /// <summary>
        /// Applies runtime device Safe Area insets to the safe-content container.
        /// </summary>
        public void ApplySafeArea()
        {
            if (rootElement == null) return;
            if (safeContent == null)
            {
                safeContent = rootElement.Q<VisualElement>("safe-content") ?? rootElement;
            }

            Rect safeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            float screenW = Screen.width;
            float screenH = Screen.height;

            float leftPercent = (safeArea.xMin / screenW) * 100f;
            float rightPercent = ((screenW - safeArea.xMax) / screenW) * 100f;
            float topPercent = ((screenH - safeArea.yMax) / screenH) * 100f;
            float bottomPercent = (safeArea.yMin / screenH) * 100f;

            safeContent.style.paddingLeft = Length.Percent(Mathf.Max(2.5f, leftPercent));
            safeContent.style.paddingRight = Length.Percent(Mathf.Max(2.5f, rightPercent));
            safeContent.style.paddingTop = Length.Percent(Mathf.Max(3f, topPercent));
            safeContent.style.paddingBottom = Length.Percent(Mathf.Max(2f, bottomPercent));
        }

        public void HideGameScreen()
        {
            if (rootElement == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootElement = uiDocument.rootVisualElement.Q<VisualElement>("game-root") ?? uiDocument.rootVisualElement;
            }

            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.None;
            }

            if (resultModal != null)
            {
                resultModal.style.display = DisplayStyle.None;
            }

            if (settingsModal != null)
            {
                settingsModal.style.display = DisplayStyle.None;
            }

            if (helpRulesModal != null)
            {
                helpRulesModal.style.display = DisplayStyle.None;
            }

            if (leaveConfirmModal != null)
            {
                leaveConfirmModal.style.display = DisplayStyle.None;
            }

            CancelDragging();

            if (tutorialController != null)
            {
                tutorialController.EndTutorial(markCompleted: false);
            }

            StopAllGameCoroutines();
        }

        #endregion
    }
}
