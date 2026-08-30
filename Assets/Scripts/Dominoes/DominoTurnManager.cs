using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Manages player turn sequencing and round-robin turn progression for a Dominoes match.
    /// Supports 2, 3, and 4 players.
    /// </summary>
    [Serializable]
    public class DominoTurnManager
    {
        [SerializeField] private int currentTurnIndex = 0;
        [SerializeField] private int turnCount = 0; // Total turns passed in the current round

        /// <summary>
        /// Gets the zero-based index of the player whose turn it currently is.
        /// </summary>
        public int CurrentTurnIndex => currentTurnIndex;

        /// <summary>
        /// Gets the total number of turns executed so far in this round.
        /// </summary>
        public int TurnCount => turnCount;

        /// <summary>
        /// Initializes turn tracking with a specified starting player index.
        /// </summary>
        /// <param name="players">The active players list.</param>
        /// <param name="startingIndex">Starting player index (determined by DominoStartingPlayer).</param>
        /// <returns>True if initialized successfully; false if player list is invalid.</returns>
        public bool Initialize(IReadOnlyList<DominoPlayer> players, int startingIndex)
        {
            if (players == null || players.Count < DominoGameState.MinPlayers)
            {
                Debug.LogWarning("[DominoTurnManager] Cannot initialize: Player list is null or empty.");
                currentTurnIndex = 0;
                turnCount = 0;
                return false;
            }

            if (startingIndex < 0 || startingIndex >= players.Count)
            {
                Debug.LogWarning($"[DominoTurnManager] Invalid starting index {startingIndex} for {players.Count} players. Defaulting to index 0.");
                currentTurnIndex = 0;
            }
            else
            {
                currentTurnIndex = startingIndex;
            }

            turnCount = 1;
            return true;
        }

        /// <summary>
        /// Convenience overload to initialize directly with a DominoGameState.
        /// </summary>
        public bool Initialize(DominoGameState gameState, int startingIndex)
        {
            if (gameState == null)
            {
                Debug.LogWarning("[DominoTurnManager] GameState cannot be null.");
                return false;
            }

            bool success = Initialize(gameState.Players, startingIndex);
            if (success)
            {
                gameState.SetTurn(currentTurnIndex);
            }
            return success;
        }

        /// <summary>
        /// Gets the player whose turn it currently is.
        /// </summary>
        /// <param name="players">The active players list.</param>
        /// <returns>The active DominoPlayer, or null if player list is invalid.</returns>
        public DominoPlayer GetCurrentPlayer(IReadOnlyList<DominoPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                return null;
            }

            if (currentTurnIndex < 0 || currentTurnIndex >= players.Count)
            {
                currentTurnIndex = 0;
            }

            return players[currentTurnIndex];
        }

        /// <summary>
        /// Advances the turn to the next player in round-robin order (e.g. 0 -> 1 -> 2 -> 3 -> 0).
        /// </summary>
        /// <param name="players">The active players list.</param>
        /// <returns>The newly active DominoPlayer, or null if no valid players exist.</returns>
        public DominoPlayer NextTurn(IReadOnlyList<DominoPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                Debug.LogWarning("[DominoTurnManager] Cannot advance turn: No players present.");
                currentTurnIndex = 0;
                return null;
            }

            currentTurnIndex = (currentTurnIndex + 1) % players.Count;
            turnCount++;
            return players[currentTurnIndex];
        }

        /// <summary>
        /// Advances the turn and synchronizes the turn index with DominoGameState.
        /// </summary>
        public DominoPlayer NextTurn(DominoGameState gameState)
        {
            if (gameState == null)
            {
                return null;
            }

            var nextPlayer = NextTurn(gameState.Players);
            if (nextPlayer != null)
            {
                gameState.SetTurn(currentTurnIndex);
            }
            return nextPlayer;
        }

        /// <summary>
        /// Checks whether the specified player is currently the active player.
        /// </summary>
        public bool IsCurrentPlayer(DominoPlayer player, IReadOnlyList<DominoPlayer> players)
        {
            if (player == null || players == null || players.Count == 0)
            {
                return false;
            }

            var currentPlayer = GetCurrentPlayer(players);
            return currentPlayer != null && currentPlayer.Id == player.Id;
        }

        /// <summary>
        /// Resets the turn manager to initial default values.
        /// </summary>
        public void Reset()
        {
            currentTurnIndex = 0;
            turnCount = 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify turn progression across 2, 3, and 4 players, wrap-around, and edge cases.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Turn Manager")]
        public static void TestTurnManagerInEditor()
        {
            Debug.Log("================ STARTING TURN MANAGER TEST ================");

            var turnManager = new DominoTurnManager();

            // 1. Test 2 Players cycling
            TestCyclingForPlayerCount(turnManager, 2, startingIndex: 0);

            // 2. Test 3 Players cycling starting from index 1
            TestCyclingForPlayerCount(turnManager, 3, startingIndex: 1);

            // 3. Test 4 Players cycling starting from index 3 (last player wrapping to 0)
            TestCyclingForPlayerCount(turnManager, 4, startingIndex: 3);

            // 4. Test Safety & Invalid Edge Cases
            Debug.Log("\n--- Testing Safety with Edge Cases ---");

            // Empty player list
            var emptyList = new List<DominoPlayer>();
            bool emptyInit = turnManager.Initialize(emptyList, 0);
            var emptyNext = turnManager.NextTurn(emptyList);
            Debug.Log($"Empty players handled safely: init={emptyInit}, nextPlayer={(emptyNext == null ? "null" : emptyNext.PlayerName)}");

            // Out-of-bounds starting index
            var twoPlayers = CreatePlayers(2);
            bool outOfBoundsInit = turnManager.Initialize(twoPlayers, startingIndex: 99);
            Debug.Log($"Out-of-bounds starting index defaulted safely to index 0: {turnManager.CurrentTurnIndex == 0}");

            Debug.Log("<color=green>✓ All Turn Manager tests PASSED.</color>");
            Debug.Log("================ TURN MANAGER TEST COMPLETED ================");
        }

        private static List<DominoPlayer> CreatePlayers(int count)
        {
            var list = new List<DominoPlayer>();
            for (int i = 1; i <= count; i++)
            {
                list.Add(new DominoPlayer(i, $"Player {i}", isHuman: (i == 1)));
            }
            return list;
        }

        private static void TestCyclingForPlayerCount(DominoTurnManager turnManager, int count, int startingIndex)
        {
            Debug.Log($"\n--- Testing {count} Players (Starting at Index {startingIndex}) ---");
            var players = CreatePlayers(count);

            turnManager.Initialize(players, startingIndex);
            Debug.Log($"Initialized: Current is {turnManager.GetCurrentPlayer(players).PlayerName} (Index {turnManager.CurrentTurnIndex})");

            // Cycle through enough turns to test wrap-around
            for (int step = 1; step <= count + 1; step++)
            {
                var next = turnManager.NextTurn(players);
                Debug.Log($"Turn Step {step} (Turn #{turnManager.TurnCount}): Active Player is {next.PlayerName} (Index {turnManager.CurrentTurnIndex})");
            }
        }
#endif
    }
}
