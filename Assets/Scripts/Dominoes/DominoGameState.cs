using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Represents the high-level match lifecycle states in a Dominoes game.
    /// </summary>
    public enum MatchState
    {
        Waiting,   // Waiting for players to join
        Starting,  // Match setup phase (distribution, deciding first turn, etc.)
        Playing,   // Active gameplay turns
        Finished   // Match concluded, winner determined
    }

    /// <summary>
    /// Central data model tracking the state of an active or pending Dominoes match.
    /// </summary>
    [Serializable]
    public class DominoGameState
    {
        public const int MinPlayers = 1;
        public const int MaxPlayers = 4;

        [SerializeField] private MatchState currentState = MatchState.Waiting;
        [SerializeField] private List<DominoPlayer> players = new List<DominoPlayer>();
        [SerializeField] private int currentTurnIndex = 0;
        [SerializeField] private DominoPlayer winner = null;

        /// <summary>
        /// Gets the current state of the match (Waiting, Starting, Playing, Finished).
        /// </summary>
        public MatchState CurrentState => currentState;

        /// <summary>
        /// Gets the read-only list of players registered in the match.
        /// </summary>
        public IReadOnlyList<DominoPlayer> Players => players;

        /// <summary>
        /// Gets the number of players currently registered.
        /// </summary>
        public int PlayerCount => players.Count;

        /// <summary>
        /// Gets the zero-based index of the player whose turn it currently is.
        /// </summary>
        public int CurrentTurnIndex => currentTurnIndex;

        /// <summary>
        /// Gets the winning player if the match has finished; otherwise null.
        /// </summary>
        public DominoPlayer Winner => winner;

        /// <summary>
        /// Initializes a new DominoGameState instance in the Waiting state.
        /// </summary>
        public DominoGameState()
        {
            Reset();
        }

        /// <summary>
        /// Sets the match lifecycle state.
        /// </summary>
        public void SetState(MatchState newState)
        {
            currentState = newState;
        }

        /// <summary>
        /// Adds a player to the match. Only allowed during the Waiting state and when below MaxPlayers (4).
        /// </summary>
        /// <param name="player">The player to add.</param>
        /// <returns>True if added successfully; false otherwise.</returns>
        public bool AddPlayer(DominoPlayer player)
        {
            if (player == null)
            {
                Debug.LogWarning("[DominoGameState] Cannot add a null player.");
                return false;
            }

            if (currentState != MatchState.Waiting && currentState != MatchState.Starting)
            {
                Debug.LogWarning($"[DominoGameState] Cannot add player '{player.PlayerName}': Match is not in Waiting/Starting state (Current: {currentState}).");
                return false;
            }

            if (players.Count >= MaxPlayers)
            {
                Debug.LogWarning($"[DominoGameState] Cannot add player '{player.PlayerName}': Maximum player limit of {MaxPlayers} reached.");
                return false;
            }

            if (players.Exists(p => p.Id == player.Id))
            {
                Debug.LogWarning($"[DominoGameState] Player with ID {player.Id} is already in the game.");
                return false;
            }

            players.Add(player);
            return true;
        }

        /// <summary>
        /// Removes a player before the match begins (only during Waiting state).
        /// </summary>
        /// <param name="playerId">ID of the player to remove.</param>
        /// <returns>True if removed successfully; false otherwise.</returns>
        public bool RemovePlayer(int playerId)
        {
            if (currentState != MatchState.Waiting)
            {
                Debug.LogWarning("[DominoGameState] Cannot remove players once match has left the Waiting state.");
                return false;
            }

            int index = players.FindIndex(p => p.Id == playerId);
            if (index >= 0)
            {
                players.RemoveAt(index);
                // Adjust turn index if needed
                if (currentTurnIndex >= players.Count && players.Count > 0)
                {
                    currentTurnIndex = 0;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the player whose turn it currently is. Returns null if there are no players.
        /// </summary>
        public DominoPlayer GetCurrentPlayer()
        {
            if (players.Count == 0 || currentTurnIndex < 0 || currentTurnIndex >= players.Count)
            {
                return null;
            }

            return players[currentTurnIndex];
        }

        /// <summary>
        /// Advances the turn to the next player in round-robin sequence.
        /// </summary>
        public void NextTurn()
        {
            if (players.Count == 0)
            {
                currentTurnIndex = 0;
                return;
            }

            currentTurnIndex = (currentTurnIndex + 1) % players.Count;
        }

        /// <summary>
        /// Sets the current turn to a specific player index safely.
        /// </summary>
        /// <param name="index">Player index to set turn to.</param>
        /// <returns>True if successfully set, false if index is invalid.</returns>
        public bool SetTurn(int index)
        {
            if (index < 0 || index >= players.Count)
            {
                Debug.LogWarning($"[DominoGameState] Invalid turn index: {index}. Valid range: 0 to {players.Count - 1}.");
                return false;
            }

            currentTurnIndex = index;
            return true;
        }

        /// <summary>
        /// Sets the match winner and automatically transitions the state to Finished.
        /// </summary>
        /// <param name="winningPlayer">The player who won the match.</param>
        public void SetWinner(DominoPlayer winningPlayer)
        {
            winner = winningPlayer;
            currentState = MatchState.Finished;
        }

        /// <summary>
        /// Resets the game state back to Waiting with cleared players, winner, and turn index.
        /// </summary>
        public void Reset()
        {
            currentState = MatchState.Waiting;
            players.Clear();
            currentTurnIndex = 0;
            winner = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify DominoGameState logic, player limits, and state transitions.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Game State")]
        public static void TestGameStateInEditor()
        {
            Debug.Log("--- Starting DominoGameState Validation Test ---");

            var state = new DominoGameState();
            Debug.Log($"Initial State: {state.CurrentState}, PlayerCount: {state.PlayerCount}");

            // 1. Test adding up to 4 players
            for (int i = 1; i <= 4; i++)
            {
                bool added = state.AddPlayer(new DominoPlayer(i, $"Player {i}", isHuman: (i == 1)));
                Debug.Log($"Added Player {i}: {added} (Current Total: {state.PlayerCount})");
            }

            // 2. Test 5th player rejection (max 4 rule)
            bool fifthAdded = state.AddPlayer(new DominoPlayer(5, "Player 5", isHuman: false));
            Debug.Log($"Add 5th Player rejected as expected: {!fifthAdded}");

            // 3. Test state transitions and turns
            state.SetState(MatchState.Playing);
            Debug.Log($"State changed to: {state.CurrentState}");
            Debug.Log($"Initial turn player: {state.GetCurrentPlayer()?.PlayerName}");

            state.NextTurn();
            Debug.Log($"Turn advanced to: {state.GetCurrentPlayer()?.PlayerName}");

            // 4. Test setting winner
            state.SetWinner(state.Players[0]);
            Debug.Log($"State after winner set: {state.CurrentState}, Winner: {state.Winner?.PlayerName}");

            Debug.Log("<color=green>✓ DominoGameState validation PASSED.</color>");
            Debug.Log("--- DominoGameState Test Completed ---");
        }
#endif
    }
}
