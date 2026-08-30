using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Manages the standard double-six domino set consisting of 28 unique tiles (0|0 to 6|6).
    /// </summary>
    [Serializable]
    public class DominoSet
    {
        public const int StandardSetCount = 28;

        [SerializeField]
        private List<DominoTile> tiles = new List<DominoTile>();

        /// <summary>
        /// Gets the current list of tiles in the set as a read-only collection.
        /// </summary>
        public IReadOnlyList<DominoTile> Tiles => tiles;

        /// <summary>
        /// Gets the number of tiles currently in the set.
        /// </summary>
        public int Count => tiles.Count;

        /// <summary>
        /// Constructor that initializes a fresh, standard double-six set of 28 tiles.
        /// </summary>
        public DominoSet()
        {
            Reset();
        }

        /// <summary>
        /// Resets and regenerates the complete, deterministic 28-tile standard set (unshuffled).
        /// Combinations range from 0|0 to 6|6 with left <= right.
        /// </summary>
        public void Reset()
        {
            tiles.Clear();

            for (int left = DominoTile.MinValue; left <= DominoTile.MaxValue; left++)
            {
                for (int right = left; right <= DominoTile.MaxValue; right++)
                {
                    tiles.Add(new DominoTile(left, right));
                }
            }
        }

        /// <summary>
        /// Shuffles the tiles currently in the set using the Fisher-Yates algorithm.
        /// </summary>
        public void Shuffle()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, tiles.Count);
                (tiles[i], tiles[randomIndex]) = (tiles[randomIndex], tiles[i]);
            }
        }

        /// <summary>
        /// Validates that the current set conforms to standard double-six rules:
        /// exactly 28 tiles, no duplicates, and all values within 0-6.
        /// </summary>
        /// <param name="errorMessage">Details of validation failure if false.</param>
        /// <returns>True if the set is completely valid, false otherwise.</returns>
        public bool Validate(out string errorMessage)
        {
            if (tiles.Count != StandardSetCount)
            {
                errorMessage = $"Invalid tile count: Expected {StandardSetCount}, but found {tiles.Count}.";
                return false;
            }

            var seenPairs = new HashSet<string>();

            foreach (var tile in tiles)
            {
                if (!DominoTile.IsValidValue(tile.Left) || !DominoTile.IsValidValue(tile.Right))
                {
                    errorMessage = $"Invalid tile values in tile {tile}: values must be between {DominoTile.MinValue} and {DominoTile.MaxValue}.";
                    return false;
                }

                // Canonical key so inverted pairs (e.g. 3|6 and 6|3) map to the same key
                int low = Math.Min(tile.Left, tile.Right);
                int high = Math.Max(tile.Left, tile.Right);
                string key = $"{low}|{high}";

                if (!seenPairs.Add(key))
                {
                    errorMessage = $"Duplicate tile combination detected: {tile}.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor menu item for testing and verifying the DominoSet directly from the Unity top menu bar.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Domino Set")]
        public static void TestDominoSetInEditor()
        {
            Debug.Log("--- Starting DominoSet Validation Test ---");

            var set = new DominoSet();
            Debug.Log($"Created new DominoSet. Tile count: {set.Count}");

            if (set.Validate(out string errorBeforeShuffle))
            {
                Debug.Log("<color=green>✓ Unshuffled set validation PASSED (28 unique tiles 0|0 to 6|6).</color>");
            }
            else
            {
                Debug.LogError($"<color=red>✗ Unshuffled set validation FAILED: {errorBeforeShuffle}</color>");
                return;
            }

            // Print all tiles
            var tileListString = string.Join(", ", set.Tiles);
            Debug.Log($"Deterministic Tiles List:\n{tileListString}");

            // Test Shuffling
            set.Shuffle();
            if (set.Validate(out string errorAfterShuffle))
            {
                Debug.Log("<color=green>✓ Shuffled set validation PASSED (28 unique tiles intact after shuffle).</color>");
                Debug.Log($"Shuffled Sample:\n{string.Join(", ", set.Tiles)}");
            }
            else
            {
                Debug.LogError($"<color=red>✗ Shuffled set validation FAILED: {errorAfterShuffle}</color>");
            }

            Debug.Log("--- DominoSet Validation Test Completed ---");
        }
#endif
    }
}
