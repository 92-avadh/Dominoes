using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Manages the pre-game waiting and countdown phase for a local/standalone Dominoes match.
    /// Enforces 2 to 4 player limits and controls countdown progression.
    /// </summary>
    [Serializable]
    public class DominoWaitingManager
    {
        public const int MinPlayersToStart = 2;
        public const int MaxPlayers = 4;
        public const float DefaultCountdownDuration = 10f;

        [SerializeField] private float countdownDuration = DefaultCountdownDuration;
        [SerializeField] private float remainingTime = 0f;
        [SerializeField] private bool isWaitingActive = false;
        [SerializeField] private List<DominoPlayer> waitingPlayers = new List<DominoPlayer>();

        // Events for UI and game coordinators
        public event Action<int> OnPlayerCountChanged;
        public event Action<float> OnCountdownTick;
        public event Action OnWaitingStarted;
        public event Action OnCountdownExpired;
        public event Action<IReadOnlyList<DominoPlayer>> OnGameReadyToStart;
        public event Action<string> OnPlayerJoinFailed;

        /// <summary>
        /// Gets the list of currently waiting players.
        /// </summary>
        public IReadOnlyList<DominoPlayer> WaitingPlayers => waitingPlayers;

        /// <summary>
        /// Gets the current number of waiting players.
        /// </summary>
        public int PlayerCount => waitingPlayers.Count;

        /// <summary>
        /// Gets the remaining countdown time in seconds.
        /// </summary>
        public float RemainingTime => remainingTime;

        /// <summary>
        /// Gets whether the waiting/countdown phase is currently active.
        /// </summary>
        public bool IsWaitingActive => isWaitingActive;

        /// <summary>
        /// Gets or sets the total countdown duration in seconds.
        /// </summary>
        public float CountdownDuration
        {
            get => countdownDuration;
            set => countdownDuration = Math.Max(1f, value);
        }

        public DominoWaitingManager(float countdownDuration = DefaultCountdownDuration)
        {
            this.countdownDuration = countdownDuration;
            ResetWaiting();
        }

        /// <summary>
        /// Starts or resets the waiting countdown without removing already added players.
        /// </summary>
        /// <param name="customDuration">Optional custom duration override in seconds.</param>
        public void StartWaiting(float? customDuration = null)
        {
            countdownDuration = customDuration ?? countdownDuration;
            remainingTime = countdownDuration;
            isWaitingActive = true;

            OnWaitingStarted?.Invoke();
            OnCountdownTick?.Invoke(remainingTime);
        }

        /// <summary>
        /// Adds a player to the waiting room.
        /// Joining does NOT reset or restart the countdown.
        /// </summary>
        /// <param name="player">The player attempting to join.</param>
        /// <returns>True if added; false if room is full or player is invalid.</returns>
        public bool AddPlayer(DominoPlayer player)
        {
            if (player == null)
            {
                OnPlayerJoinFailed?.Invoke("Cannot add a null player.");
                return false;
            }

            if (waitingPlayers.Count >= MaxPlayers)
            {
                OnPlayerJoinFailed?.Invoke($"Room is full (Maximum {MaxPlayers} players reached).");
                return false;
            }

            if (waitingPlayers.Exists(p => p.Id == player.Id))
            {
                OnPlayerJoinFailed?.Invoke($"Player with ID {player.Id} is already in the waiting room.");
                return false;
            }

            waitingPlayers.Add(player);
            OnPlayerCountChanged?.Invoke(waitingPlayers.Count);
            return true;
        }

        /// <summary>
        /// Removes a player from the waiting room.
        /// </summary>
        /// <param name="playerId">ID of the player to remove.</param>
        /// <returns>True if player was found and removed; false otherwise.</returns>
        public bool RemovePlayer(int playerId)
        {
            int index = waitingPlayers.FindIndex(p => p.Id == playerId);
            if (index >= 0)
            {
                waitingPlayers.RemoveAt(index);
                OnPlayerCountChanged?.Invoke(waitingPlayers.Count);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the countdown timer. Call with deltaTime (e.g. from Update()).
        /// </summary>
        /// <param name="deltaTime">Elapsed time in seconds.</param>
        public void UpdateCountdown(float deltaTime)
        {
            if (!isWaitingActive || remainingTime <= 0f)
            {
                return;
            }

            remainingTime = Math.Max(0f, remainingTime - deltaTime);
            OnCountdownTick?.Invoke(remainingTime);

            if (remainingTime <= 0f)
            {
                HandleCountdownExpired();
            }
        }

        private void HandleCountdownExpired()
        {
            isWaitingActive = false;
            OnCountdownExpired?.Invoke();

            if (CanStartGame())
            {
                OnGameReadyToStart?.Invoke(waitingPlayers);
            }
            else
            {
                Debug.Log($"[DominoWaitingManager] Countdown expired with {waitingPlayers.Count} player(s). Minimum {MinPlayersToStart} required to start. Remaining in waiting state.");
            }
        }

        /// <summary>
        /// Checks whether the game meets the criteria to start (2 to 4 players).
        /// </summary>
        public bool CanStartGame()
        {
            return waitingPlayers.Count >= MinPlayersToStart && waitingPlayers.Count <= MaxPlayers;
        }

        /// <summary>
        /// Resets the waiting manager, clearing all players and stopping the countdown.
        /// </summary>
        public void ResetWaiting()
        {
            isWaitingActive = false;
            remainingTime = countdownDuration;
            waitingPlayers.Clear();
            OnPlayerCountChanged?.Invoke(0);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify waiting countdown, mid-countdown joins, 5th player rejection, and start requirements.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Waiting Manager")]
        public static void TestWaitingManagerInEditor()
        {
            Debug.Log("================ STARTING WAITING MANAGER TEST ================");

            var manager = new DominoWaitingManager(countdownDuration: 10f);

            // 1. Test 1 Player & Start Countdown
            Debug.Log("\n--- Test 1: 1 Player & Countdown Start ---");
            manager.StartWaiting(5f);
            manager.AddPlayer(new DominoPlayer(1, "Alice"));
            Debug.Log($"Waiting Active: {manager.IsWaitingActive}, Remaining: {manager.RemainingTime}s, Players: {manager.PlayerCount}");

            // Tick 2 seconds
            manager.UpdateCountdown(2f);
            Debug.Log($"After 2s tick: Remaining = {manager.RemainingTime}s");

            // 2. Add 2nd Player while countdown is running -> Timer should NOT restart
            Debug.Log("\n--- Test 2: Add Player 2 while Countdown is running ---");
            float timeBeforeJoin = manager.RemainingTime;
            manager.AddPlayer(new DominoPlayer(2, "Bob"));
            bool timerPreserved = (manager.RemainingTime == timeBeforeJoin);
            Debug.Log($"Timer preserved after Player 2 joined: {timerPreserved} (Remaining: {manager.RemainingTime}s, Players: {manager.PlayerCount})");

            // 3. Add Player 3 and Player 4 (Reaching Max 4)
            Debug.Log("\n--- Test 3: Add Players 3 & 4 (Reaching Max 4) ---");
            manager.AddPlayer(new DominoPlayer(3, "Charlie"));
            manager.AddPlayer(new DominoPlayer(4, "Dave"));
            Debug.Log($"Current Player Count: {manager.PlayerCount} (CanStart: {manager.CanStartGame()})");

            // 4. Attempt to add 5th Player -> Must be rejected
            Debug.Log("\n--- Test 4: Reject 5th Player ---");
            bool fifthAdded = manager.AddPlayer(new DominoPlayer(5, "Eve"));
            Debug.Log($"5th Player rejected: {!fifthAdded} (Current Count: {manager.PlayerCount})");

            // 5. Test Countdown Expiry with 1 Player (Must NOT start)
            Debug.Log("\n--- Test 5: Countdown Expiry with 1 Player ---");
            manager.ResetWaiting();
            manager.StartWaiting(2f);
            manager.AddPlayer(new DominoPlayer(1, "SoloPlayer"));
            manager.UpdateCountdown(2.5f); // Expire timer
            Debug.Log($"1-Player CanStart after expiry: {manager.CanStartGame()} (Expected: false)");

            // 6. Test Countdown Expiry with 2 Players (Must start)
            Debug.Log("\n--- Test 6: Countdown Expiry with 2 Players ---");
            manager.ResetWaiting();
            manager.StartWaiting(2f);
            manager.AddPlayer(new DominoPlayer(1, "P1"));
            manager.AddPlayer(new DominoPlayer(2, "P2"));
            bool readyFired2 = false;
            manager.OnGameReadyToStart += (players) => { readyFired2 = true; };
            manager.UpdateCountdown(2.5f); // Expire timer
            Debug.Log($"2-Player CanStart: {manager.CanStartGame()} (ReadyToStart event fired: {readyFired2})");

            // 7. Test 3 and 4 Players CanStart
            Debug.Log("\n--- Test 7: 3 and 4 Players CanStart ---");
            manager.AddPlayer(new DominoPlayer(3, "P3"));
            Debug.Log($"3 Players CanStart: {manager.CanStartGame()} (Expected: true)");
            manager.AddPlayer(new DominoPlayer(4, "P4"));
            Debug.Log($"4 Players CanStart: {manager.CanStartGame()} (Expected: true)");

            if (!fifthAdded && timerPreserved && readyFired2 && manager.CanStartGame())
            {
                Debug.Log("<color=green>✓ All Waiting Manager tests PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Waiting Manager tests FAILED.</color>");
            }

            Debug.Log("================ WAITING MANAGER TEST COMPLETED ================");
        }
#endif
    }
}
