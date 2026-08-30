using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Supported starting-player determination methods.
    /// IMPORTANT: The final rule will be selected once the client's reference gameplay specifications are confirmed.
    /// </summary>
    public enum StartingPlayerStrategy
    {
        Random,             // Picks any player at random
        SpecificPlayerIndex,// Always starts with a specific player index (e.g., Player 0 / Local player)
        HighestDouble,      // Player holding the highest double tile (e.g., [6|6], [5|5], etc.)
        HighestTile         // Player holding the highest single tile (by pip sum and high end)
    }

    /// <summary>
    /// Structure holding the outcome when determining the starting player of a match.
    /// </summary>
    public struct StartingPlayerResult
    {
        public bool Success;
        public int PlayerIndex;
        public DominoPlayer Player;
        public DominoTile DecidingTile;
        public string Description;

        public static StartingPlayerResult Fail(string reason) => new StartingPlayerResult
        {
            Success = false,
            PlayerIndex = -1,
            Player = null,
            DecidingTile = null,
            Description = reason
        };
    }

    /// <summary>
    /// Configurable starting-player selector.
    /// Allows changing the opening-turn rule dynamically or by configuration without modifying gameplay logic.
    /// </summary>
    [Serializable]
    public class DominoStartingPlayer
    {
        [SerializeField] private StartingPlayerStrategy strategy = StartingPlayerStrategy.HighestDouble;
        [SerializeField] private int specificPlayerIndex = 0;
        [SerializeField] private bool fallbackToHighestTileIfNoDouble = true;

        /// <summary>
        /// Gets or sets the active strategy used to determine the opening player.
        /// </summary>
        public StartingPlayerStrategy Strategy
        {
            get => strategy;
            set => strategy = value;
        }

        /// <summary>
        /// Gets or sets the specific player index used when Strategy is set to SpecificPlayerIndex.
        /// </summary>
        public int SpecificPlayerIndex
        {
            get => specificPlayerIndex;
            set => specificPlayerIndex = value;
        }

        /// <summary>
        /// When Strategy is HighestDouble, whether to fall back to HighestTile if no player holds a double.
        /// </summary>
        public bool FallbackToHighestTileIfNoDouble
        {
            get => fallbackToHighestTileIfNoDouble;
            set => fallbackToHighestTileIfNoDouble = value;
        }

        public DominoStartingPlayer(StartingPlayerStrategy strategy = StartingPlayerStrategy.HighestDouble, int specificPlayerIndex = 0)
        {
            this.strategy = strategy;
            this.specificPlayerIndex = specificPlayerIndex;
            this.fallbackToHighestTileIfNoDouble = true;
        }

        /// <summary>
        /// Evaluates all player hands and determines the starting player according to the active strategy.
        /// </summary>
        /// <param name="players">The list of players participating in the match.</param>
        /// <returns>StartingPlayerResult with the chosen player and deciding details.</returns>
        public StartingPlayerResult DetermineStartingPlayer(IReadOnlyList<DominoPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                return StartingPlayerResult.Fail("No players provided.");
            }

            switch (strategy)
            {
                case StartingPlayerStrategy.Random:
                    return DetermineRandomPlayer(players);

                case StartingPlayerStrategy.SpecificPlayerIndex:
                    return DetermineSpecificPlayer(players, specificPlayerIndex);

                case StartingPlayerStrategy.HighestDouble:
                    return DetermineHighestDouble(players, fallbackToHighestTileIfNoDouble);

                case StartingPlayerStrategy.HighestTile:
                    return DetermineHighestTile(players);

                default:
                    return StartingPlayerResult.Fail($"Unsupported strategy: {strategy}");
            }
        }

        private StartingPlayerResult DetermineRandomPlayer(IReadOnlyList<DominoPlayer> players)
        {
            int randomIndex = UnityEngine.Random.Range(0, players.Count);
            return new StartingPlayerResult
            {
                Success = true,
                PlayerIndex = randomIndex,
                Player = players[randomIndex],
                DecidingTile = null,
                Description = $"Randomly selected {players[randomIndex].PlayerName} (Index: {randomIndex})."
            };
        }

        private StartingPlayerResult DetermineSpecificPlayer(IReadOnlyList<DominoPlayer> players, int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= players.Count)
            {
                return StartingPlayerResult.Fail($"Specific player index {targetIndex} is out of bounds (0 to {players.Count - 1}).");
            }

            return new StartingPlayerResult
            {
                Success = true,
                PlayerIndex = targetIndex,
                Player = players[targetIndex],
                DecidingTile = null,
                Description = $"Selected specific player {players[targetIndex].PlayerName} (Index: {targetIndex})."
            };
        }

        private StartingPlayerResult DetermineHighestDouble(IReadOnlyList<DominoPlayer> players, bool fallback)
        {
            int highestDoubleValue = -1;
            int bestPlayerIndex = -1;
            DominoTile bestTile = null;

            for (int p = 0; p < players.Count; p++)
            {
                var player = players[p];
                if (player == null) continue;

                foreach (var tile in player.Hand)
                {
                    if (tile != null && tile.IsDouble && tile.Left > highestDoubleValue)
                    {
                        highestDoubleValue = tile.Left;
                        bestPlayerIndex = p;
                        bestTile = tile;
                    }
                }
            }

            if (bestPlayerIndex >= 0)
            {
                return new StartingPlayerResult
                {
                    Success = true,
                    PlayerIndex = bestPlayerIndex,
                    Player = players[bestPlayerIndex],
                    DecidingTile = bestTile,
                    Description = $"{players[bestPlayerIndex].PlayerName} has the highest double: {bestTile}."
                };
            }

            // No doubles held in any hand
            if (fallback)
            {
                var fallbackResult = DetermineHighestTile(players);
                if (fallbackResult.Success)
                {
                    fallbackResult.Description = $"No doubles held. Fallback: {fallbackResult.Description}";
                    return fallbackResult;
                }
            }

            return StartingPlayerResult.Fail("No double tiles found in any player's hand.");
        }

        /// <summary>
        /// Finds the player holding the highest tile without altering player hand order.
        /// Comparison:
        /// 1. Higher total pips (Left + Right).
        /// 2. If pip sum is tied, higher individual end value Max(Left, Right).
        /// </summary>
        private StartingPlayerResult DetermineHighestTile(IReadOnlyList<DominoPlayer> players)
        {
            int bestPlayerIndex = -1;
            DominoTile bestTile = null;
            int bestTotalPips = -1;
            int bestHighEnd = -1;

            for (int p = 0; p < players.Count; p++)
            {
                var player = players[p];
                if (player == null) continue;

                foreach (var tile in player.Hand)
                {
                    if (tile == null) continue;

                    int totalPips = tile.Left + tile.Right;
                    int highEnd = Math.Max(tile.Left, tile.Right);

                    bool isBetter = false;
                    if (totalPips > bestTotalPips)
                    {
                        isBetter = true;
                    }
                    else if (totalPips == bestTotalPips && highEnd > bestHighEnd)
                    {
                        isBetter = true;
                    }

                    if (isBetter)
                    {
                        bestTotalPips = totalPips;
                        bestHighEnd = highEnd;
                        bestPlayerIndex = p;
                        bestTile = tile;
                    }
                }
            }

            if (bestPlayerIndex >= 0)
            {
                return new StartingPlayerResult
                {
                    Success = true,
                    PlayerIndex = bestPlayerIndex,
                    Player = players[bestPlayerIndex],
                    DecidingTile = bestTile,
                    Description = $"{players[bestPlayerIndex].PlayerName} has the highest tile: {bestTile} (Pips: {bestTotalPips})."
                };
            }

            return StartingPlayerResult.Fail("No valid tiles found in any player's hand.");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify each starting player strategy.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Starting Player Strategies")]
        public static void TestStartingPlayerStrategiesInEditor()
        {
            Debug.Log("================ STARTING PLAYER STRATEGIES TEST ================");

            var player1 = new DominoPlayer(1, "Player 1 (Alice)", isHuman: true);
            var player2 = new DominoPlayer(2, "Player 2 (Bob)", isHuman: false);
            var players = new List<DominoPlayer> { player1, player2 };

            // Player 1 has [6|5] (11 pips) and [2|2] (Double 2)
            player1.AddTile(new DominoTile(6, 5));
            player1.AddTile(new DominoTile(2, 2));

            // Player 2 has [5|5] (Double 5) and [0|1] (1 pip)
            player2.AddTile(new DominoTile(5, 5));
            player2.AddTile(new DominoTile(0, 1));

            var startingSelector = new DominoStartingPlayer();

            // 1. Test Highest Double Strategy
            startingSelector.Strategy = StartingPlayerStrategy.HighestDouble;
            var resDouble = startingSelector.DetermineStartingPlayer(players);
            Debug.Log($"[HighestDouble] Result: {resDouble.Description}");
            if (resDouble.Success && resDouble.PlayerIndex == 1) // Player 2 has [5|5] vs Player 1 [2|2]
            {
                Debug.Log("<color=green>✓ HighestDouble strategy PASSED (Player 2 [5|5] won over Player 1 [2|2]).</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ HighestDouble strategy FAILED.</color>");
            }

            // 2. Test Highest Tile Strategy
            startingSelector.Strategy = StartingPlayerStrategy.HighestTile;
            var resTile = startingSelector.DetermineStartingPlayer(players);
            Debug.Log($"[HighestTile] Result: {resTile.Description}");
            if (resTile.Success && resTile.PlayerIndex == 0) // Player 1 has [6|5] (11 pips) vs Player 2 [5|5] (10 pips)
            {
                Debug.Log("<color=green>✓ HighestTile strategy PASSED (Player 1 [6|5] won with 11 pips).</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ HighestTile strategy FAILED.</color>");
            }

            // 3. Test Specific Player Strategy
            startingSelector.Strategy = StartingPlayerStrategy.SpecificPlayerIndex;
            startingSelector.SpecificPlayerIndex = 0;
            var resSpecific = startingSelector.DetermineStartingPlayer(players);
            Debug.Log($"[SpecificPlayer] Result: {resSpecific.Description}");
            if (resSpecific.Success && resSpecific.PlayerIndex == 0)
            {
                Debug.Log("<color=green>✓ SpecificPlayer strategy PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ SpecificPlayer strategy FAILED.</color>");
            }

            // 4. Test Random Strategy
            startingSelector.Strategy = StartingPlayerStrategy.Random;
            var resRandom = startingSelector.DetermineStartingPlayer(players);
            Debug.Log($"[Random] Result: {resRandom.Description}");
            if (resRandom.Success && (resRandom.PlayerIndex == 0 || resRandom.PlayerIndex == 1))
            {
                Debug.Log("<color=green>✓ Random strategy PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Random strategy FAILED.</color>");
            }

            Debug.Log("================ STARTING PLAYER TEST COMPLETED ================");
        }
#endif
    }
}
