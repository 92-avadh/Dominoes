using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Reasons why a match/round concluded.
    /// </summary>
    public enum GameEndReason
    {
        None,             // Match still in progress
        HandEmptied,      // Standard win: a player placed all their tiles
        BlockedGame,      // [PLACEHOLDER] Blocked game: no player can move and boneyard is empty
        ManualTermination // Match ended prematurely or by custom rule
    }

    /// <summary>
    /// Result structure detailing game completion status and winning player.
    /// </summary>
    public struct GameCompletionResult
    {
        public bool IsGameOver;
        public GameEndReason Reason;
        public DominoPlayer Winner;
        public string Description;

        public static GameCompletionResult InProgress() => new GameCompletionResult
        {
            IsGameOver = false,
            Reason = GameEndReason.None,
            Winner = null,
            Description = "Game is in progress."
        };

        public static GameCompletionResult Completed(DominoPlayer winner, GameEndReason reason, string description) => new GameCompletionResult
        {
            IsGameOver = true,
            Reason = reason,
            Winner = winner,
            Description = description
        };
    }

    /// <summary>
    /// Evaluates match completion conditions (e.g. empty hands or blocked game) and designates the winning player.
    /// Keeps unconfirmed blocked-game rules configurable as placeholders pending client confirmation.
    /// </summary>
    [Serializable]
    public class DominoGameCompletionManager
    {
        [SerializeField] private bool enableBlockedGameDetection = true;

        private bool isGameOver = false;
        private DominoPlayer winner = null;
        private GameEndReason endReason = GameEndReason.None;

        /// <summary>
        /// Gets whether the current round has concluded.
        /// </summary>
        public bool IsGameOver => isGameOver;

        /// <summary>
        /// Gets the winning player if the match has concluded; otherwise null.
        /// </summary>
        public DominoPlayer Winner => winner;

        /// <summary>
        /// Gets the reason the match concluded.
        /// </summary>
        public GameEndReason EndReason => endReason;

        /// <summary>
        /// Gets or sets whether to detect blocked games when no players have legal moves.
        /// </summary>
        public bool EnableBlockedGameDetection
        {
            get => enableBlockedGameDetection;
            set => enableBlockedGameDetection = value;
        }

        public DominoGameCompletionManager(bool enableBlockedGameDetection = true)
        {
            this.enableBlockedGameDetection = enableBlockedGameDetection;
            Reset();
        }

        /// <summary>
        /// Evaluates all players and board state to determine if the match has finished.
        /// If finished, updates DominoGameState appropriately.
        /// </summary>
        public GameCompletionResult CheckGameCompletion(
            DominoGameState gameState, 
            DominoBoard board, 
            DominoDealer dealer = null, 
            DominoPassDrawManager passDrawManager = null)
        {
            if (gameState == null || gameState.Players == null || gameState.Players.Count == 0)
            {
                return GameCompletionResult.InProgress();
            }

            // If already marked finished, return current status
            if (isGameOver)
            {
                return GameCompletionResult.Completed(winner, endReason, $"Game already completed. Winner: {winner?.PlayerName ?? "None"} ({endReason}).");
            }

            // Condition 1: Check if any player emptied their hand (Standard Domino Win)
            foreach (var player in gameState.Players)
            {
                if (player != null && player.HandCount == 0)
                {
                    isGameOver = true;
                    winner = player;
                    endReason = GameEndReason.HandEmptied;

                    gameState.SetWinner(winner);

                    return GameCompletionResult.Completed(
                        winner, 
                        endReason, 
                        $"'{player.PlayerName}' played all their tiles and won the round!"
                    );
                }
            }

            // Condition 2: [PLACEHOLDER] Check for Blocked Game (no player can move and boneyard is empty)
            if (enableBlockedGameDetection && board != null && !board.IsEmpty)
            {
                bool boneyardEmpty = (dealer == null || dealer.RemainingCount == 0);
                if (boneyardEmpty)
                {
                    bool anyPlayerHasMove = false;
                    var checker = passDrawManager ?? new DominoPassDrawManager();

                    foreach (var player in gameState.Players)
                    {
                        if (player != null && checker.HasPlayableTile(board, player))
                        {
                            anyPlayerHasMove = true;
                            break;
                        }
                    }

                    if (!anyPlayerHasMove)
                    {
                        isGameOver = true;
                        endReason = GameEndReason.BlockedGame;

                        // PLACEHOLDER: Determine winner by lowest total pips remaining in hand
                        winner = DetermineLowestPipWinner(gameState.Players);
                        if (winner != null)
                        {
                            gameState.SetWinner(winner);
                        }
                        else
                        {
                            gameState.SetState(MatchState.Finished);
                        }

                        return GameCompletionResult.Completed(
                            winner, 
                            endReason, 
                            $"[PLACEHOLDER] Blocked game detected! No player can make a move. Lowest pip player: {winner?.PlayerName ?? "Tie/Unresolved"}."
                        );
                    }
                }
            }

            return GameCompletionResult.InProgress();
        }

        /// <summary>
        /// [PLACEHOLDER] Helper to find player with lowest pip total in a blocked game.
        /// </summary>
        private DominoPlayer DetermineLowestPipWinner(IReadOnlyList<DominoPlayer> players)
        {
            DominoPlayer lowestPlayer = null;
            int minPips = int.MaxValue;

            foreach (var player in players)
            {
                if (player == null) continue;

                int pips = 0;
                foreach (var tile in player.Hand)
                {
                    if (tile != null) pips += (tile.Left + tile.Right);
                }

                if (pips < minPips)
                {
                    minPips = pips;
                    lowestPlayer = player;
                }
            }

            return lowestPlayer;
        }

        /// <summary>
        /// Resets the completion manager state for a new round.
        /// </summary>
        public void Reset()
        {
            isGameOver = false;
            winner = null;
            endReason = GameEndReason.None;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify game completion detection, winner storage, and reset behavior.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Game Completion Manager")]
        public static void TestGameCompletionManagerInEditor()
        {
            Debug.Log("================ STARTING GAME COMPLETION MANAGER TEST ================");
            Debug.Log("<color=yellow>[NOTE] Blocked game resolutions are PLACEHOLDERS pending client rule confirmation.</color>");

            var completionManager = new DominoGameCompletionManager();
            var gameState = new DominoGameState();
            var board = new DominoBoard();

            var p1 = new DominoPlayer(1, "Alice");
            var p2 = new DominoPlayer(2, "Bob");

            gameState.AddPlayer(p1);
            gameState.AddPlayer(p2);
            gameState.SetState(MatchState.Playing);

            // 1. Test In-Progress Game (both players have tiles)
            Debug.Log("\n--- Test 1: Game In-Progress ---");
            p1.AddTile(new DominoTile(6, 6));
            p2.AddTile(new DominoTile(5, 5));

            var resInProgress = completionManager.CheckGameCompletion(gameState, board);
            Debug.Log($"In-Progress Result: IsGameOver={resInProgress.IsGameOver} (Message: '{resInProgress.Description}')");
            if (!resInProgress.IsGameOver && completionManager.Winner == null && gameState.CurrentState == MatchState.Playing)
            {
                Debug.Log("<color=green>✓ In-progress state PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ In-progress state FAILED.</color>");
            }

            // 2. Test Player Empties Hand (Alice plays all tiles)
            Debug.Log("\n--- Test 2: Alice Empties Hand ---");
            p1.ClearHand(); // Alice has 0 tiles remaining

            var resWon = completionManager.CheckGameCompletion(gameState, board);
            Debug.Log($"Empty Hand Result: IsGameOver={resWon.IsGameOver}, Winner={resWon.Winner?.PlayerName}, GameState={gameState.CurrentState}");
            if (resWon.IsGameOver && resWon.Winner == p1 && gameState.CurrentState == MatchState.Finished && gameState.Winner == p1)
            {
                Debug.Log("<color=green>✓ Empty hand completion & winner storage PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Empty hand completion FAILED.</color>");
            }

            // 3. Test Reset for Next Round
            Debug.Log("\n--- Test 3: Reset for Next Round ---");
            completionManager.Reset();
            gameState.Reset();
            gameState.AddPlayer(p1);
            gameState.AddPlayer(p2);
            gameState.SetState(MatchState.Playing);
            p1.AddTile(new DominoTile(1, 1));

            var resAfterReset = completionManager.CheckGameCompletion(gameState, board);
            Debug.Log($"After Reset Result: IsGameOver={resAfterReset.IsGameOver}, Winner={completionManager.Winner}");
            if (!resAfterReset.IsGameOver && completionManager.Winner == null)
            {
                Debug.Log("<color=green>✓ Completion manager Reset PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Completion manager Reset FAILED.</color>");
            }

            Debug.Log("================ GAME COMPLETION TEST COMPLETED ================");
        }
#endif
    }
}
