using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Manages shuffling and dealing of domino tiles from a standard 28-tile DominoSet to players.
    /// Tracks remaining undealt tiles (the boneyard/stock).
    /// </summary>
    [Serializable]
    public class DominoDealer
    {
        public const int MinPlayersToDeal = 2;
        public const int MaxPlayersToDeal = 4;

        [SerializeField] private DominoSet dominoSet = new DominoSet();
        [SerializeField] private List<DominoTile> remainingTiles = new List<DominoTile>();

        /// <summary>
        /// Gets the list of remaining undealt tiles.
        /// </summary>
        public IReadOnlyList<DominoTile> RemainingTiles => remainingTiles;

        /// <summary>
        /// Gets the count of remaining undealt tiles.
        /// </summary>
        public int RemainingCount => remainingTiles.Count;

        /// <summary>
        /// Gets the underlying DominoSet.
        /// </summary>
        public DominoSet DominoSet => dominoSet;

        /// <summary>
        /// Constructor for DominoDealer.
        /// </summary>
        public DominoDealer()
        {
            dominoSet = new DominoSet();
            remainingTiles = new List<DominoTile>();
        }

        /// <summary>
        /// Shuffles the domino set, clears player hands, and deals a specified number of tiles to each player.
        /// </summary>
        /// <param name="players">The collection of players in the match (must be 2 to 4 players).</param>
        /// <param name="tilesPerPlayer">Configurable number of tiles to deal to each player.</param>
        /// <param name="errorMessage">Output error message if dealing fails.</param>
        /// <returns>True if deal succeeded; false otherwise.</returns>
        public bool Deal(IReadOnlyList<DominoPlayer> players, int tilesPerPlayer, out string errorMessage)
        {
            if (players == null || players.Count < MinPlayersToDeal || players.Count > MaxPlayersToDeal)
            {
                int count = players?.Count ?? 0;
                errorMessage = $"Invalid player count: {count}. Dealing requires {MinPlayersToDeal} to {MaxPlayersToDeal} players.";
                Debug.LogWarning($"[DominoDealer] {errorMessage}");
                return false;
            }

            if (tilesPerPlayer <= 0)
            {
                errorMessage = $"Invalid tilesPerPlayer: {tilesPerPlayer}. Must be at least 1.";
                Debug.LogWarning($"[DominoDealer] {errorMessage}");
                return false;
            }

            int totalRequiredTiles = players.Count * tilesPerPlayer;
            if (totalRequiredTiles > DominoSet.StandardSetCount)
            {
                errorMessage = $"Cannot deal {tilesPerPlayer} tiles to {players.Count} players. Requires {totalRequiredTiles} tiles, but standard set only has {DominoSet.StandardSetCount}.";
                Debug.LogWarning($"[DominoDealer] {errorMessage}");
                return false;
            }

            // 1. Reset and shuffle the complete 28-tile set
            dominoSet.Reset();
            dominoSet.Shuffle();

            // 2. Clear existing hands for all players
            foreach (var player in players)
            {
                player.ClearHand();
            }

            // 3. Prepare remaining tiles pool
            remainingTiles.Clear();
            remainingTiles.AddRange(dominoSet.Tiles);

            // 4. Deal tiles to each player round-robin
            for (int t = 0; t < tilesPerPlayer; t++)
            {
                foreach (var player in players)
                {
                    if (remainingTiles.Count == 0)
                    {
                        errorMessage = "Ran out of tiles during deal.";
                        return false;
                    }

                    DominoTile tileToDeal = remainingTiles[0];
                    remainingTiles.RemoveAt(0);
                    player.AddTile(tileToDeal);
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Convenience overload to deal tiles directly to players in a DominoGameState.
        /// </summary>
        public bool Deal(DominoGameState gameState, int tilesPerPlayer, out string errorMessage)
        {
            if (gameState == null)
            {
                errorMessage = "GameState cannot be null.";
                return false;
            }

            return Deal(gameState.Players, tilesPerPlayer, out errorMessage);
        }

        /// <summary>
        /// Draws a single tile from the remaining undealt tiles pool if available.
        /// </summary>
        /// <returns>The drawn tile, or null if the remaining pool is empty.</returns>
        public DominoTile DrawTile()
        {
            if (remainingTiles.Count == 0)
            {
                return null;
            }

            DominoTile tile = remainingTiles[0];
            remainingTiles.RemoveAt(0);
            return tile;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify dealing across 2, 3, and 4 players, no duplicate tiles, and remaining counts.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Dealer")]
        public static void TestDealerInEditor()
        {
            Debug.Log("================ STARTING DEALER TEST ================");

            var dealer = new DominoDealer();

            // Test 1-Player rejection
            var singlePlayerList = new List<DominoPlayer> { new DominoPlayer(1, "Solo") };
            bool singleDealResult = dealer.Deal(singlePlayerList, 7, out string singleErr);
            Debug.Log($"1 Player deal rejected as expected: {!singleDealResult} (Reason: {singleErr})");

            // Test 2 Players (e.g. 7 tiles each)
            TestPlayerCountDeal(dealer, 2, 7);

            // Test 3 Players (e.g. 6 tiles each)
            TestPlayerCountDeal(dealer, 3, 6);

            // Test 4 Players (e.g. 7 tiles each)
            TestPlayerCountDeal(dealer, 4, 7);

            Debug.Log("================ DEALER TEST COMPLETED ================");
        }

        private static void TestPlayerCountDeal(DominoDealer dealer, int playerCount, int tilesPerPlayer)
        {
            Debug.Log($"\n--- Testing Deal: {playerCount} Players, {tilesPerPlayer} Tiles Each ---");

            var players = new List<DominoPlayer>();
            for (int i = 1; i <= playerCount; i++)
            {
                players.Add(new DominoPlayer(i, $"Player {i}", isHuman: (i == 1)));
            }

            bool success = dealer.Deal(players, tilesPerPlayer, out string err);
            if (!success)
            {
                Debug.LogError($"<color=red>Deal failed: {err}</color>");
                return;
            }

            int totalDealt = playerCount * tilesPerPlayer;
            int expectedRemaining = DominoSet.StandardSetCount - totalDealt;

            Debug.Log($"Deal success: {success}");
            Debug.Log($"Total tiles dealt: {totalDealt}, Remaining pool: {dealer.RemainingCount} (Expected: {expectedRemaining})");

            // Verify tile uniqueness across all hands and remaining pool
            var allDealtAndRemaining = new HashSet<DominoTile>();
            bool hasDuplicates = false;

            foreach (var player in players)
            {
                Debug.Log($"{player.PlayerName} hand ({player.HandCount} tiles): {string.Join(", ", player.Hand)}");
                foreach (var tile in player.Hand)
                {
                    if (!allDealtAndRemaining.Add(tile))
                    {
                        hasDuplicates = true;
                        Debug.LogError($"<color=red>Duplicate tile detected: {tile}</color>");
                    }
                }
            }

            foreach (var tile in dealer.RemainingTiles)
            {
                if (!allDealtAndRemaining.Add(tile))
                {
                    hasDuplicates = true;
                    Debug.LogError($"<color=red>Remaining pool has duplicate tile: {tile}</color>");
                }
            }

            if (!hasDuplicates && allDealtAndRemaining.Count == DominoSet.StandardSetCount && dealer.RemainingCount == expectedRemaining)
            {
                Debug.Log($"<color=green>✓ Validation PASSED for {playerCount} players: All 28 tiles accounted for with zero duplicates!</color>");
            }
            else
            {
                Debug.LogError($"<color=red>✗ Validation FAILED for {playerCount} players.</color>");
            }
        }
#endif
    }
}
