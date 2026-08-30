#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Comprehensive test suite verifying all waiting flow scenarios:
    /// Test A (1 Player - No start)
    /// Test B (2 Players - Start on expiry)
    /// Test C (3 Players - Start on expiry)
    /// Test D (4 Players - Start on expiry)
    /// Test E (Leave waiting state)
    /// Test F (Re-enter waiting state & timer reset)
    /// Test G (Duplicate listeners guard)
    /// </summary>
    public static class DominoWaitingFlowTests
    {
        [MenuItem("Dominoes/Run Waiting Flow Tests")]
        public static void RunAllWaitingFlowTests()
        {
            Debug.Log("===============================================================");
            Debug.Log(">>> RUNNING DOMINOES WAITING FLOW & MATCH TESTS <<<");
            Debug.Log("===============================================================");

            bool allPassed = true;

            allPassed &= TestScenarioA_OnePlayerNoStart();
            allPassed &= TestScenarioB_TwoPlayersStartOnExpiry();
            allPassed &= TestScenarioC_ThreePlayersStart();
            allPassed &= TestScenarioD_FourPlayersStart();
            allPassed &= TestScenarioE_LeaveWaiting();
            allPassed &= TestScenarioF_ReenterWaitingAndTimerReset();
            allPassed &= TestScenarioG_NoDuplicateListeners();

            Debug.Log("===============================================================");
            if (allPassed)
            {
                Debug.Log("<color=green><b>ALL WAITING FLOW TESTS PASSED SUCCESSFULLY! (7/7)</b></color>");
            }
            else
            {
                Debug.LogError("<color=red><b>SOME WAITING FLOW TESTS FAILED!</b></color>");
            }
            Debug.Log("===============================================================");
        }

        private static bool TestScenarioA_OnePlayerNoStart()
        {
            Debug.Log("\n[TEST A] 1 Player alone must NOT start the game when timer expires.");
            var matchManager = new DominoMatchManager(waitingCountdownDuration: 15f);
            matchManager.Initialize();

            bool matchStarted = false;
            matchManager.OnMatchStarted += () => { matchStarted = true; };

            // Add 1 human player
            matchManager.AddPlayer(new DominoPlayer(1, "Player 1 (You)", isHuman: true));
            matchManager.StartWaitingPhase(15f);

            // Verify player count is 1
            if (matchManager.WaitingManager.PlayerCount != 1)
            {
                Debug.LogError($"[TEST A FAILED] Expected 1 player, got {matchManager.WaitingManager.PlayerCount}");
                return false;
            }

            // Fast-forward countdown past 15 seconds
            matchManager.Update(16f);

            // Verify match did not start and remains in Waiting state
            if (matchStarted || matchManager.CurrentState != MatchState.Waiting)
            {
                Debug.LogError($"[TEST A FAILED] Match should NOT have started with 1 player. State: {matchManager.CurrentState}, Started: {matchStarted}");
                return false;
            }

            Debug.Log("<color=green>✓ Test A Passed: 1 player safely kept in Waiting state without starting game.</color>");
            return true;
        }

        private static bool TestScenarioB_TwoPlayersStartOnExpiry()
        {
            Debug.Log("\n[TEST B] 2 Players start game when timer expires.");
            var matchManager = new DominoMatchManager(waitingCountdownDuration: 15f);
            matchManager.Initialize();

            bool matchStarted = false;
            matchManager.OnMatchStarted += () => { matchStarted = true; };

            matchManager.AddPlayer(new DominoPlayer(1, "Player 1 (You)", isHuman: true));
            matchManager.StartWaitingPhase(15f);

            // Bot 1 joins at T+2s
            matchManager.Update(2f);
            matchManager.AddPlayer(new DominoPlayer(2, "Bot 1", isHuman: false));

            if (matchManager.WaitingManager.PlayerCount != 2)
            {
                Debug.LogError($"[TEST B FAILED] Expected 2 players, got {matchManager.WaitingManager.PlayerCount}");
                return false;
            }

            // Fast-forward remaining 13s
            matchManager.Update(14f);

            if (!matchStarted || matchManager.CurrentState != MatchState.Playing)
            {
                Debug.LogError($"[TEST B FAILED] Match should have started with 2 players. State: {matchManager.CurrentState}, Started: {matchStarted}");
                return false;
            }

            // Verify hands were dealt
            if (matchManager.GameState.Players[0].HandCount != 7 || matchManager.GameState.Players[1].HandCount != 7)
            {
                Debug.LogError($"[TEST B FAILED] Players did not receive dealt tiles. P1: {matchManager.GameState.Players[0].HandCount}, P2: {matchManager.GameState.Players[1].HandCount}");
                return false;
            }

            Debug.Log("<color=green>✓ Test B Passed: 2 players successfully started game and received 7 tiles each on timer expiry.</color>");
            return true;
        }

        private static bool TestScenarioC_ThreePlayersStart()
        {
            Debug.Log("\n[TEST C] 3 Players start game when timer expires.");
            var matchManager = new DominoMatchManager(waitingCountdownDuration: 15f);
            matchManager.Initialize();

            bool matchStarted = false;
            matchManager.OnMatchStarted += () => { matchStarted = true; };

            matchManager.AddPlayer(new DominoPlayer(1, "Player 1 (You)", isHuman: true));
            matchManager.StartWaitingPhase(15f);

            matchManager.Update(2f);
            matchManager.AddPlayer(new DominoPlayer(2, "Bot 1", isHuman: false));

            matchManager.Update(3f);
            matchManager.AddPlayer(new DominoPlayer(3, "Bot 2", isHuman: false));

            if (matchManager.WaitingManager.PlayerCount != 3)
            {
                Debug.LogError($"[TEST C FAILED] Expected 3 players, got {matchManager.WaitingManager.PlayerCount}");
                return false;
            }

            // Expire timer
            matchManager.Update(11f);

            if (!matchStarted || matchManager.CurrentState != MatchState.Playing)
            {
                Debug.LogError($"[TEST C FAILED] Match should have started with 3 players. State: {matchManager.CurrentState}");
                return false;
            }

            Debug.Log("<color=green>✓ Test C Passed: 3 players successfully started game upon timer expiry.</color>");
            return true;
        }

        private static bool TestScenarioD_FourPlayersStart()
        {
            Debug.Log("\n[TEST D] 4 Players start game when timer expires.");
            var matchManager = new DominoMatchManager(waitingCountdownDuration: 15f);
            matchManager.Initialize();

            bool matchStarted = false;
            matchManager.OnMatchStarted += () => { matchStarted = true; };

            matchManager.AddPlayer(new DominoPlayer(1, "Player 1 (You)", isHuman: true));
            matchManager.StartWaitingPhase(15f);

            matchManager.AddPlayer(new DominoPlayer(2, "Bot 1", isHuman: false));
            matchManager.AddPlayer(new DominoPlayer(3, "Bot 2", isHuman: false));
            matchManager.AddPlayer(new DominoPlayer(4, "Bot 3", isHuman: false));

            if (matchManager.WaitingManager.PlayerCount != 4)
            {
                Debug.LogError($"[TEST D FAILED] Expected 4 players, got {matchManager.WaitingManager.PlayerCount}");
                return false;
            }

            matchManager.Update(16f);

            if (!matchStarted || matchManager.CurrentState != MatchState.Playing)
            {
                Debug.LogError($"[TEST D FAILED] Match should have started with 4 players. State: {matchManager.CurrentState}");
                return false;
            }

            Debug.Log("<color=green>✓ Test D Passed: 4 players successfully started game upon timer expiry.</color>");
            return true;
        }

        private static bool TestScenarioE_LeaveWaiting()
        {
            Debug.Log("\n[TEST E] Leave button cancels waiting, resets state, and starts no game.");
            var matchManager = new DominoMatchManager(waitingCountdownDuration: 15f);
            matchManager.Initialize();

            bool matchStarted = false;
            matchManager.OnMatchStarted += () => { matchStarted = true; };

            matchManager.AddPlayer(new DominoPlayer(1, "Player 1", isHuman: true));
            matchManager.AddPlayer(new DominoPlayer(2, "Bot 1", isHuman: false));
            matchManager.StartWaitingPhase(15f);
            matchManager.Update(5f);

            // User clicks Leave -> ResetMatch()
            matchManager.ResetMatch();

            if (matchManager.WaitingManager.IsWaitingActive || matchManager.WaitingManager.PlayerCount != 0 || matchStarted)
            {
                Debug.LogError($"[TEST E FAILED] Waiting state was not properly reset on leave.");
                return false;
            }

            Debug.Log("<color=green>✓ Test E Passed: Leave cleanly reset waiting state without starting match.</color>");
            return true;
        }

        private static bool TestScenarioF_ReenterWaitingAndTimerReset()
        {
            Debug.Log("\n[TEST F] Re-entering waiting resets timer and player count correctly.");
            var matchManager = new DominoMatchManager(waitingCountdownDuration: 15f);
            matchManager.Initialize();

            // First entrance
            matchManager.AddPlayer(new DominoPlayer(1, "Player 1", isHuman: true));
            matchManager.StartWaitingPhase(15f);
            matchManager.Update(7f); // 8s left

            // Leave
            matchManager.ResetMatch();

            // Re-enter
            matchManager.AddPlayer(new DominoPlayer(1, "Player 1", isHuman: true));
            matchManager.StartWaitingPhase(15f);

            if (Math.Abs(matchManager.WaitingManager.RemainingTime - 15f) > 0.01f || matchManager.WaitingManager.PlayerCount != 1)
            {
                Debug.LogError($"[TEST F FAILED] Re-entered waiting does not have fresh 15s timer or 1 player. Remaining: {matchManager.WaitingManager.RemainingTime}, Players: {matchManager.WaitingManager.PlayerCount}");
                return false;
            }

            Debug.Log("<color=green>✓ Test F Passed: Re-entering waiting cleanly resets timer to 15s and player count to 1.</color>");
            return true;
        }

        private static bool TestScenarioG_NoDuplicateListeners()
        {
            Debug.Log("\n[TEST G] Calling Initialize multiple times or resetting does not produce duplicate event triggers.");
            var matchManager = new DominoMatchManager(waitingCountdownDuration: 15f);
            matchManager.Initialize();
            matchManager.Initialize(); // Second initialize call

            int startCalls = 0;
            matchManager.OnMatchStarted += () => { startCalls++; };

            matchManager.AddPlayer(new DominoPlayer(1, "P1", true));
            matchManager.AddPlayer(new DominoPlayer(2, "P2", false));
            matchManager.StartWaitingPhase(15f);
            matchManager.Update(16f); // Expire timer

            if (startCalls != 1)
            {
                Debug.LogError($"[TEST G FAILED] OnMatchStarted fired {startCalls} times instead of exactly 1.");
                return false;
            }

            Debug.Log("<color=green>✓ Test G Passed: Exactly 1 start event fired (no duplicate listeners).</color>");
            return true;
        }
    }
}
#endif
