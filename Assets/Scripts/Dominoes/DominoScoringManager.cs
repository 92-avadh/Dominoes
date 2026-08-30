using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Supported scoring formula types.
    /// IMPORTANT: Round-end calculations are temporary PLACEHOLDERS until the client confirms the official rule set.
    /// </summary>
    public enum ScoringFormulaType
    {
        /// <summary>
        /// [PLACEHOLDER] Winner receives the sum of all pips remaining in opponents' hands.
        /// </summary>
        RemainingOpponentPipsSum,

        /// <summary>
        /// Fixed point increment (e.g. +1 point per round win).
        /// </summary>
        FixedPerRoundWin,

        /// <summary>
        /// Custom manual score adjustments.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Pure C# scoring manager for tracking player points across rounds.
    /// Keeps scoring formula configurable so it can be updated after client rule confirmation.
    /// </summary>
    [Serializable]
    public class DominoScoringManager
    {
        [SerializeField] private ScoringFormulaType formulaType = ScoringFormulaType.RemainingOpponentPipsSum;
        [SerializeField] private int pointsPerRoundWin = 1;

        // Player ID -> Score dictionary
        private Dictionary<int, int> playerScores = new Dictionary<int, int>();

        /// <summary>
        /// Gets or sets the active scoring calculation formula type.
        /// </summary>
        public ScoringFormulaType FormulaType
        {
            get => formulaType;
            set => formulaType = value;
        }

        /// <summary>
        /// Gets or sets fixed points awarded per win (used when FormulaType is FixedPerRoundWin).
        /// </summary>
        public int PointsPerRoundWin
        {
            get => pointsPerRoundWin;
            set => pointsPerRoundWin = Math.Max(0, value);
        }

        public DominoScoringManager(ScoringFormulaType formulaType = ScoringFormulaType.RemainingOpponentPipsSum)
        {
            this.formulaType = formulaType;
            playerScores = new Dictionary<int, int>();
        }

        /// <summary>
        /// Gets the current score of a player. Returns 0 if player has no recorded score.
        /// </summary>
        public int GetScore(int playerId)
        {
            return playerScores.TryGetValue(playerId, out int score) ? score : 0;
        }

        /// <summary>
        /// Gets the current score of a player.
        /// </summary>
        public int GetScore(DominoPlayer player)
        {
            if (player == null) return 0;
            return GetScore(player.Id);
        }

        /// <summary>
        /// Adds points to a player. Prevents accidental negative point additions.
        /// </summary>
        /// <param name="playerId">ID of the player.</param>
        /// <param name="points">Points to add (must be >= 0).</param>
        /// <returns>The player's new total score.</returns>
        public int AddPoints(int playerId, int points)
        {
            if (points < 0)
            {
                Debug.LogWarning($"[DominoScoringManager] Rejected negative point addition ({points}) for Player ID {playerId}.");
                return GetScore(playerId);
            }

            int current = GetScore(playerId);
            int newScore = current + points;
            playerScores[playerId] = newScore;
            return newScore;
        }

        /// <summary>
        /// Adds points to a player.
        /// </summary>
        public int AddPoints(DominoPlayer player, int points)
        {
            if (player == null)
            {
                Debug.LogWarning("[DominoScoringManager] Cannot add points to a null player.");
                return 0;
            }

            return AddPoints(player.Id, points);
        }

        /// <summary>
        /// Sets a player's score directly to a specific non-negative value.
        /// </summary>
        public void SetScore(int playerId, int score)
        {
            playerScores[playerId] = Math.Max(0, score);
        }

        /// <summary>
        /// Resets an individual player's score to 0.
        /// </summary>
        public void ResetScore(int playerId)
        {
            playerScores[playerId] = 0;
        }

        /// <summary>
        /// Resets an individual player's score to 0.
        /// </summary>
        public void ResetScore(DominoPlayer player)
        {
            if (player != null)
            {
                ResetScore(player.Id);
            }
        }

        /// <summary>
        /// Resets all player scores to 0.
        /// </summary>
        public void ResetAllScores()
        {
            playerScores.Clear();
        }

        /// <summary>
        /// Computes points earned for a round.
        /// IMPORTANT: This calculation is a PLACEHOLDER and will be swapped once client rules are confirmed.
        /// </summary>
        /// <param name="roundWinner">The player who won the round.</param>
        /// <param name="allPlayers">All players in the match (2, 3, or 4 players).</param>
        /// <returns>Points calculated according to the active FormulaType.</returns>
        public int CalculateRoundScore(DominoPlayer roundWinner, IReadOnlyList<DominoPlayer> allPlayers)
        {
            if (roundWinner == null || allPlayers == null || allPlayers.Count == 0)
            {
                return 0;
            }

            switch (formulaType)
            {
                case ScoringFormulaType.RemainingOpponentPipsSum:
                    // PLACEHOLDER RULE: Sum of all pips in all opponents' remaining hand tiles
                    int opponentPipsTotal = 0;
                    foreach (var player in allPlayers)
                    {
                        if (player == null || player.Id == roundWinner.Id) continue;

                        foreach (var tile in player.Hand)
                        {
                            if (tile != null)
                            {
                                opponentPipsTotal += (tile.Left + tile.Right);
                            }
                        }
                    }
                    return opponentPipsTotal;

                case ScoringFormulaType.FixedPerRoundWin:
                    return pointsPerRoundWin;

                case ScoringFormulaType.Custom:
                default:
                    return 0;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify score tracking, point additions, resets, and placeholder calculations.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Scoring Manager")]
        public static void TestScoringManagerInEditor()
        {
            Debug.Log("================ STARTING SCORING MANAGER TEST ================");
            Debug.Log("<color=yellow>[NOTE] Current round scoring formulas are PLACEHOLDERS and will be updated upon client confirmation.</color>");

            var scoring = new DominoScoringManager();

            // 1. Test 2-Player scoring and point addition
            Debug.Log("\n--- Test 1: 2 Players Score Tracking ---");
            var p1 = new DominoPlayer(1, "Alice");
            var p2 = new DominoPlayer(2, "Bob");
            var players2 = new List<DominoPlayer> { p1, p2 };

            scoring.AddPoints(p1, 25);
            scoring.AddPoints(p2, 10);
            Debug.Log($"Player 1 Score: {scoring.GetScore(p1)} (Expected: 25)");
            Debug.Log($"Player 2 Score: {scoring.GetScore(p2)} (Expected: 10)");

            // 2. Test Reset single player vs Reset All
            Debug.Log("\n--- Test 2: Reset Functions ---");
            scoring.ResetScore(p2);
            Debug.Log($"After Resetting Player 2: P1={scoring.GetScore(p1)}, P2={scoring.GetScore(p2)} (Expected: 25, 0)");

            scoring.ResetAllScores();
            Debug.Log($"After ResetAllScores: P1={scoring.GetScore(p1)}, P2={scoring.GetScore(p2)} (Expected: 0, 0)");

            // 3. Test Negative Score Protection
            Debug.Log("\n--- Test 3: Negative Score Rejection ---");
            scoring.AddPoints(p1, -50);
            Debug.Log($"Score after attempting negative addition: {scoring.GetScore(p1)} (Expected: 0)");

            // 4. Test 3 and 4 Player Placeholder Round Calculation
            Debug.Log("\n--- Test 4: 4 Players Placeholder Round Score Calculation ---");
            var p3 = new DominoPlayer(3, "Charlie");
            var p4 = new DominoPlayer(4, "Dave");
            var players4 = new List<DominoPlayer> { p1, p2, p3, p4 };

            // Give opponents tiles:
            // Bob has [6|6] (12 pips)
            p2.AddTile(new DominoTile(6, 6));
            // Charlie has [4|3] (7 pips)
            p3.AddTile(new DominoTile(4, 3));
            // Dave has [1|2] (3 pips)
            p4.AddTile(new DominoTile(1, 2));

            // Alice is winner (0 tiles in hand)
            p1.ClearHand();

            scoring.FormulaType = ScoringFormulaType.RemainingOpponentPipsSum;
            int calculatedPoints = scoring.CalculateRoundScore(p1, players4);
            int expectedPips = 12 + 7 + 3; // 22 pips
            Debug.Log($"Placeholder calculated points for Winner (Alice): {calculatedPoints} (Expected: {expectedPips})");
            scoring.AddPoints(p1, calculatedPoints);
            Debug.Log($"Alice new score: {scoring.GetScore(p1)}");

            if (calculatedPoints == expectedPips && scoring.GetScore(p1) == expectedPips)
            {
                Debug.Log("<color=green>✓ Scoring manager tests PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Scoring manager tests FAILED.</color>");
            }

            Debug.Log("================ SCORING MANAGER TEST COMPLETED ================");
        }
#endif
    }
}
