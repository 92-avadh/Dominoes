using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Coordinates round lifecycle operations including starting new rounds, resetting boards, dealing tiles,
    /// selecting the opening player, and performing full match resets.
    /// </summary>
    [Serializable]
    public class DominoRoundManager
    {
        [SerializeField] private int roundNumber = 0;
        [SerializeField] private int defaultTilesPerPlayer = 7;

        /// <summary>
        /// Gets the current round number (1-based for active rounds, 0 before match start).
        /// </summary>
        public int RoundNumber => roundNumber;

        /// <summary>
        /// Gets or sets default tiles dealt per player when not specified.
        /// </summary>
        public int DefaultTilesPerPlayer
        {
            get => defaultTilesPerPlayer;
            set => defaultTilesPerPlayer = Math.Max(1, value);
        }

        public DominoRoundManager(int defaultTilesPerPlayer = 7)
        {
            this.defaultTilesPerPlayer = defaultTilesPerPlayer;
            this.roundNumber = 0;
        }

        /// <summary>
        /// Starts a fresh round: clears the board, resets dealer, deals hands, determines opening player,
        /// and initializes turns. Preserves existing cumulative scores.
        /// </summary>
        /// <param name="gameState">Match game state containing registered players.</param>
        /// <param name="board">Board to clear and initialize.</param>
        /// <param name="dealer">Dealer for shuffling and distributing tiles.</param>
        /// <param name="startingPlayerSelector">Strategy selector for opening player.</param>
        /// <param name="turnManager">Turn manager to initialize with starting player.</param>
        /// <param name="completionManager">Completion manager to reset.</param>
        /// <param name="tilesPerPlayer">Number of tiles to deal each player (defaults to DefaultTilesPerPlayer).</param>
        /// <param name="errorMessage">Output error details if start fails.</param>
        /// <returns>True if round started successfully; false otherwise.</returns>
        public bool StartNewRound(
            DominoGameState gameState,
            DominoBoard board,
            DominoDealer dealer,
            DominoStartingPlayer startingPlayerSelector,
            DominoTurnManager turnManager,
            DominoGameCompletionManager completionManager,
            int? tilesPerPlayer,
            out string errorMessage)
        {
            if (gameState == null)
            {
                errorMessage = "GameState reference is null.";
                return false;
            }

            if (gameState.PlayerCount < DominoDealer.MinPlayersToDeal || gameState.PlayerCount > DominoDealer.MaxPlayersToDeal)
            {
                errorMessage = $"Cannot start round: Player count ({gameState.PlayerCount}) must be between {DominoDealer.MinPlayersToDeal} and {DominoDealer.MaxPlayersToDeal}.";
                Debug.LogWarning($"[DominoRoundManager] {errorMessage}");
                return false;
            }

            int dealCount = (gameState.PlayerCount == 4 && (!tilesPerPlayer.HasValue || tilesPerPlayer.Value == 7))
                ? 5 // Standard 4-player Draw Dominoes: 5 tiles each leaving 8 in boneyard
                : (tilesPerPlayer ?? defaultTilesPerPlayer);

            // 1. Reset board and completion state
            board?.Clear();
            completionManager?.Reset();

            // 2. Clear hands and deal fresh shuffled tiles
            dealer ??= new DominoDealer();
            bool dealSuccess = dealer.Deal(gameState.Players, dealCount, out string dealError);
            if (!dealSuccess)
            {
                errorMessage = $"Failed dealing tiles: {dealError}";
                return false;
            }

            // 3. Determine starting player using configured strategy
            startingPlayerSelector ??= new DominoStartingPlayer();
            var startResult = startingPlayerSelector.DetermineStartingPlayer(gameState.Players);
            int startingIndex = startResult.Success ? startResult.PlayerIndex : 0;

            // 4. Initialize turn tracking with chosen opening player
            turnManager ??= new DominoTurnManager();
            turnManager.Initialize(gameState, startingIndex);

            // 5. Update game state to Playing
            gameState.SetState(MatchState.Playing);
            roundNumber++;

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Performs a full match reset: resets cumulative scores, clears board, hands, and dealer,
        /// and resets state back to Waiting.
        /// </summary>
        public void FullMatchReset(
            DominoGameState gameState,
            DominoBoard board,
            DominoDealer dealer,
            DominoTurnManager turnManager,
            DominoScoringManager scoringManager,
            DominoGameCompletionManager completionManager)
        {
            board?.Clear();
            completionManager?.Reset();
            turnManager?.Reset();
            scoringManager?.ResetAllScores();
            dealer?.DominoSet.Reset();

            if (gameState != null)
            {
                foreach (var player in gameState.Players)
                {
                    player?.ClearHand();
                }
                gameState.Reset();
            }

            roundNumber = 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify starting rounds, board clearing, score preservation, and full resets.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Round Manager")]
        public static void TestRoundManagerInEditor()
        {
            Debug.Log("================ STARTING ROUND MANAGER TEST ================");

            var roundManager = new DominoRoundManager();
            var gameState = new DominoGameState();
            var board = new DominoBoard();
            var dealer = new DominoDealer();
            var startingSelector = new DominoStartingPlayer();
            var turnManager = new DominoTurnManager();
            var scoringManager = new DominoScoringManager();
            var completionManager = new DominoGameCompletionManager();

            var p1 = new DominoPlayer(1, "Alice");
            var p2 = new DominoPlayer(2, "Bob");
            gameState.AddPlayer(p1);
            gameState.AddPlayer(p2);

            // Pre-add score to simulate past round score
            scoringManager.AddPoints(p1, 50);
            Debug.Log($"Initial Cumulative Score for Alice: {scoringManager.GetScore(p1)}");

            // 1. Start Round 1
            Debug.Log("\n--- Test 1: Starting Round 1 ---");
            bool r1Started = roundManager.StartNewRound(
                gameState, board, dealer, startingSelector, turnManager, completionManager,
                tilesPerPlayer: 7, out string err1
            );
            Debug.Log($"Round 1 Started: {r1Started} (Round #{roundManager.RoundNumber}, Current Player: {turnManager.GetCurrentPlayer(gameState.Players).PlayerName})");
            Debug.Log($"Alice Hand: {p1.HandCount} tiles, Bob Hand: {p2.HandCount} tiles, Board Count: {board.TileCount}, MatchState: {gameState.CurrentState}");
            if (r1Started && p1.HandCount == 7 && p2.HandCount == 7 && board.IsEmpty && gameState.CurrentState == MatchState.Playing)
            {
                Debug.Log("<color=green>✓ Round 1 start PASSED.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>✗ Round 1 start FAILED: {err1}</color>");
                return;
            }

            // Simulate round progress: place tile on board and finish round
            board.PlaceTile(new DominoTile(6, 6), BoardSide.Left, out _);
            completionManager.CheckGameCompletion(gameState, board);

            // 2. Start Round 2 (Score must be preserved)
            Debug.Log("\n--- Test 2: Starting Round 2 (Score Preservation) ---");
            bool r2Started = roundManager.StartNewRound(
                gameState, board, dealer, startingSelector, turnManager, completionManager,
                tilesPerPlayer: 7, out string err2
            );
            Debug.Log($"Round 2 Started: {r2Started} (Round #{roundManager.RoundNumber})");
            Debug.Log($"Board IsEmpty after new round: {board.IsEmpty} (Count: {board.TileCount})");
            Debug.Log($"Alice Score after new round: {scoringManager.GetScore(p1)} (Expected: 50)");
            if (r2Started && board.IsEmpty && scoringManager.GetScore(p1) == 50 && roundManager.RoundNumber == 2)
            {
                Debug.Log("<color=green>✓ New round started and scores preserved PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ New round score preservation FAILED.</color>");
            }

            // 3. Test Full Match Reset
            Debug.Log("\n--- Test 3: Full Match Reset ---");
            roundManager.FullMatchReset(gameState, board, dealer, turnManager, scoringManager, completionManager);
            Debug.Log($"After Full Reset: Round #{roundManager.RoundNumber}, State: {gameState.CurrentState}, Alice Score: {scoringManager.GetScore(p1)} (Expected: 0)");
            if (roundManager.RoundNumber == 0 && gameState.CurrentState == MatchState.Waiting && scoringManager.GetScore(p1) == 0)
            {
                Debug.Log("<color=green>✓ Full match reset PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Full match reset FAILED.</color>");
            }

            Debug.Log("================ ROUND MANAGER TEST COMPLETED ================");
        }
#endif
    }
}
