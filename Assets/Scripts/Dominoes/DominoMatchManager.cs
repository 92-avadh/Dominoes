using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Master coordinator for a Dominoes match lifecycle.
    /// Orchestrates the transition from the waiting phase into active round gameplay.
    /// </summary>
    [Serializable]
    public class DominoMatchManager
    {
        [SerializeField] private DominoGameState gameState = new DominoGameState();
        [SerializeField] private DominoWaitingManager waitingManager = new DominoWaitingManager();
        [SerializeField] private DominoDealer dealer = new DominoDealer();
        [SerializeField] private DominoBoard board = new DominoBoard();
        [SerializeField] private DominoStartingPlayer startingPlayerSelector = new DominoStartingPlayer();
        [SerializeField] private DominoTurnManager turnManager = new DominoTurnManager();
        [SerializeField] private DominoScoringManager scoringManager = new DominoScoringManager();
        [SerializeField] private DominoGameCompletionManager completionManager = new DominoGameCompletionManager();
        [SerializeField] private DominoRoundManager roundManager = new DominoRoundManager();

        [SerializeField] private int tilesPerPlayer = 7;

        // Events for UI and outer controllers
        public event Action<MatchState> OnMatchStateChanged;
        public event Action<string> OnMatchStartFailed;
        public event Action OnMatchStarted;

        public DominoGameState GameState => gameState;
        public DominoWaitingManager WaitingManager => waitingManager;
        public DominoDealer Dealer => dealer;
        public DominoBoard Board => board;
        public DominoStartingPlayer StartingPlayerSelector => startingPlayerSelector;
        public DominoTurnManager TurnManager => turnManager;
        public DominoScoringManager ScoringManager => scoringManager;
        public DominoGameCompletionManager CompletionManager => completionManager;
        public DominoRoundManager RoundManager => roundManager;

        public MatchState CurrentState => gameState.CurrentState;

        /// <summary>
        /// Gets or sets the configurable number of tiles dealt to each player at match start.
        /// </summary>
        public int TilesPerPlayer
        {
            get => tilesPerPlayer;
            set => tilesPerPlayer = Math.Max(1, value);
        }

        public DominoMatchManager(float waitingCountdownDuration = 10f, int tilesPerPlayer = 7)
        {
            this.tilesPerPlayer = tilesPerPlayer;
            this.gameState = new DominoGameState();
            this.waitingManager = new DominoWaitingManager(waitingCountdownDuration);
            this.dealer = new DominoDealer();
            this.board = new DominoBoard();
            this.startingPlayerSelector = new DominoStartingPlayer();
            this.turnManager = new DominoTurnManager();
            this.scoringManager = new DominoScoringManager();
            this.completionManager = new DominoGameCompletionManager();
            this.roundManager = new DominoRoundManager(tilesPerPlayer);

            Initialize();
        }

        /// <summary>
        /// Ensures internal references and event subscriptions are active after Unity serialization/deserialization.
        /// </summary>
        public void Initialize()
        {
            gameState ??= new DominoGameState();
            waitingManager ??= new DominoWaitingManager();
            dealer ??= new DominoDealer();
            board ??= new DominoBoard();
            startingPlayerSelector ??= new DominoStartingPlayer();
            turnManager ??= new DominoTurnManager();
            scoringManager ??= new DominoScoringManager();
            completionManager ??= new DominoGameCompletionManager();
            roundManager ??= new DominoRoundManager(tilesPerPlayer);

            // Re-subscribe cleanly to prevent duplicate or missing subscriptions
            waitingManager.OnGameReadyToStart -= HandleWaitingReady;
            waitingManager.OnGameReadyToStart += HandleWaitingReady;
        }

        /// <summary>
        /// Adds a player to the match while in Waiting state.
        /// </summary>
        public bool AddPlayer(DominoPlayer player)
        {
            if (CurrentState != MatchState.Waiting)
            {
                Debug.LogWarning($"[DominoMatchManager] Cannot add player '{player?.PlayerName}': Match is already in state {CurrentState}.");
                return false;
            }

            return waitingManager.AddPlayer(player);
        }

        /// <summary>
        /// Starts the waiting countdown.
        /// </summary>
        public void StartWaitingPhase(float? customCountdownDuration = null)
        {
            if (CurrentState != MatchState.Waiting)
            {
                Debug.LogWarning($"[DominoMatchManager] Cannot start waiting phase: Match is in state {CurrentState}.");
                return;
            }

            waitingManager.StartWaiting(customCountdownDuration);
        }

        /// <summary>
        /// Ticks the waiting countdown timer. Call from Unity Update().
        /// </summary>
        public void Update(float deltaTime)
        {
            if (CurrentState == MatchState.Waiting)
            {
                waitingManager.UpdateCountdown(deltaTime);
            }
        }

        private void HandleWaitingReady(IReadOnlyList<DominoPlayer> players)
        {
            TryStartMatch(out _);
        }

        /// <summary>
        /// Prepares and starts the match with current waiting players.
        /// Deals tiles, selects starting player, and sets state to Playing.
        /// </summary>
        /// <param name="errorMessage">Output error details if start fails.</param>
        /// <returns>True if match started successfully; false otherwise.</returns>
        public bool TryStartMatch(out string errorMessage)
        {
            // 1. Guard against repeated start calls
            if (CurrentState != MatchState.Waiting)
            {
                errorMessage = $"Cannot start match: Match is already in {CurrentState} state.";
                Debug.LogWarning($"[DominoMatchManager] {errorMessage}");
                OnMatchStartFailed?.Invoke(errorMessage);
                return false;
            }

            // 2. Validate player count (2 to 4 players required)
            if (!waitingManager.CanStartGame())
            {
                errorMessage = $"Cannot start match: Requires between {DominoWaitingManager.MinPlayersToStart} and {DominoWaitingManager.MaxPlayers} players (Current: {waitingManager.PlayerCount}).";
                Debug.LogWarning($"[DominoMatchManager] {errorMessage}");
                OnMatchStartFailed?.Invoke(errorMessage);
                return false;
            }

            // 3. Transition to Starting/Preparing phase
            gameState.Reset();

            // Register waiting players into active GameState
            foreach (var player in waitingManager.WaitingPlayers)
            {
                gameState.AddPlayer(player);
            }

            gameState.SetState(MatchState.Starting);
            OnMatchStateChanged?.Invoke(MatchState.Starting);

            // 4. Use DominoRoundManager to prepare the round, deal tiles, pick starting player, and start turns
            bool roundStarted = roundManager.StartNewRound(
                gameState,
                board,
                dealer,
                startingPlayerSelector,
                turnManager,
                completionManager,
                tilesPerPlayer,
                out string roundError
            );

            if (!roundStarted)
            {
                errorMessage = $"Failed to start round: {roundError}";
                Debug.LogError($"[DominoMatchManager] {errorMessage}");
                gameState.SetState(MatchState.Waiting);
                OnMatchStateChanged?.Invoke(MatchState.Waiting);
                OnMatchStartFailed?.Invoke(errorMessage);
                return false;
            }

            // 5. Match is now in Playing state
            errorMessage = string.Empty;
            OnMatchStateChanged?.Invoke(MatchState.Playing);
            OnMatchStarted?.Invoke();
            return true;
        }

        /// <summary>
        /// Resets the entire match back to the initial Waiting state for a fresh test.
        /// </summary>
        public void ResetMatch()
        {
            roundManager.FullMatchReset(gameState, board, dealer, turnManager, scoringManager, completionManager);
            waitingManager.ResetWaiting();
            OnMatchStateChanged?.Invoke(MatchState.Waiting);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify match preparation, player limits, tile dealing, starting player selection, and duplicate start guards.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Match Manager")]
        public static void TestMatchManagerInEditor()
        {
            Debug.Log("================ STARTING MATCH MANAGER TEST ================");

            var matchManager = new DominoMatchManager(waitingCountdownDuration: 5f, tilesPerPlayer: 7);

            // 1. Test 1 Player -> Match must NOT start
            Debug.Log("\n--- Test 1: 1 Player Start Attempt ---");
            matchManager.AddPlayer(new DominoPlayer(1, "Alice"));
            bool onePlayerStart = matchManager.TryStartMatch(out string err1);
            Debug.Log($"1 Player start rejected: {!onePlayerStart} (Reason: '{err1}', CurrentState: {matchManager.CurrentState})");

            // 2. Test 2 Players -> Match should start
            Debug.Log("\n--- Test 2: 2 Players Start ---");
            matchManager.AddPlayer(new DominoPlayer(2, "Bob"));
            bool twoPlayerStart = matchManager.TryStartMatch(out string err2);
            Debug.Log($"2 Players start success: {twoPlayerStart} (State: {matchManager.CurrentState})");
            Debug.Log($"Player 1 (Alice) Hand: {matchManager.GameState.Players[0].HandCount} tiles, Player 2 (Bob) Hand: {matchManager.GameState.Players[1].HandCount} tiles");
            Debug.Log($"Active Turn Player: {matchManager.TurnManager.GetCurrentPlayer(matchManager.GameState.Players)?.PlayerName}");

            // 3. Test Repeated Start Call -> Must be rejected
            Debug.Log("\n--- Test 3: Repeated Start Call Guard ---");
            bool duplicateStart = matchManager.TryStartMatch(out string errDup);
            Debug.Log($"Duplicate start rejected: {!duplicateStart} (Reason: '{errDup}')");

            // 4. Test Adding Player while in Playing State -> Must be rejected
            Debug.Log("\n--- Test 4: Add Player during Playing state ---");
            bool midGameJoin = matchManager.AddPlayer(new DominoPlayer(3, "Charlie"));
            Debug.Log($"Mid-game join rejected: {!midGameJoin}");

            // 5. Test 3 Players Start
            Debug.Log("\n--- Test 5: 3 Players Start ---");
            matchManager.ResetMatch();
            matchManager.TilesPerPlayer = 6;
            matchManager.AddPlayer(new DominoPlayer(1, "Alice"));
            matchManager.AddPlayer(new DominoPlayer(2, "Bob"));
            matchManager.AddPlayer(new DominoPlayer(3, "Charlie"));
            bool threePlayerStart = matchManager.TryStartMatch(out string err3);
            Debug.Log($"3 Players start success: {threePlayerStart} (State: {matchManager.CurrentState}, Each received: {matchManager.GameState.Players[0].HandCount} tiles)");

            // 6. Test 4 Players Start
            Debug.Log("\n--- Test 6: 4 Players Start ---");
            matchManager.ResetMatch();
            matchManager.TilesPerPlayer = 7;
            matchManager.AddPlayer(new DominoPlayer(1, "Alice"));
            matchManager.AddPlayer(new DominoPlayer(2, "Bob"));
            matchManager.AddPlayer(new DominoPlayer(3, "Charlie"));
            matchManager.AddPlayer(new DominoPlayer(4, "Dave"));
            bool fourPlayerStart = matchManager.TryStartMatch(out string err4);
            Debug.Log($"4 Players start success: {fourPlayerStart} (State: {matchManager.CurrentState}, Each received: {matchManager.GameState.Players[0].HandCount} tiles)");

            if (!onePlayerStart && twoPlayerStart && !duplicateStart && !midGameJoin && threePlayerStart && fourPlayerStart)
            {
                Debug.Log("<color=green>✓ All Match Manager tests PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Match Manager tests FAILED.</color>");
            }

            Debug.Log("================ MATCH MANAGER TEST COMPLETED ================");
        }
#endif
    }
}
