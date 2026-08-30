using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Pure C# data model representing a player in a Dominoes match.
    /// Tracks player identity and the tiles currently held in their hand.
    /// </summary>
    [Serializable]
    public class DominoPlayer
    {
        [SerializeField] private int id;
        [SerializeField] private string playerName;
        [SerializeField] private bool isHuman;
        [SerializeField] private List<DominoTile> hand = new List<DominoTile>();

        /// <summary>
        /// Gets the unique identifier for this player.
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Gets the display name of this player.
        /// </summary>
        public string PlayerName => playerName;

        /// <summary>
        /// Gets whether this player is a human/local player (true) or an AI/opponent (false).
        /// </summary>
        public bool IsHuman => isHuman;

        /// <summary>
        /// Gets a read-only view of the tiles currently in the player's hand.
        /// </summary>
        public IReadOnlyList<DominoTile> Hand => hand;

        /// <summary>
        /// Gets the number of tiles currently in the player's hand.
        /// </summary>
        public int HandCount => hand.Count;

        /// <summary>
        /// Constructor for creating a player.
        /// </summary>
        /// <param name="id">Unique player ID.</param>
        /// <param name="playerName">Display name for the player.</param>
        /// <param name="isHuman">True if local human player; false if AI/opponent.</param>
        public DominoPlayer(int id, string playerName, bool isHuman = true)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));
            }

            this.id = id;
            this.playerName = playerName;
            this.isHuman = isHuman;
            this.hand = new List<DominoTile>();
        }

        /// <summary>
        /// Adds a domino tile to the player's hand.
        /// </summary>
        /// <param name="tile">The tile to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if tile is null.</exception>
        public void AddTile(DominoTile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile), "Cannot add a null tile to player's hand.");
            }

            hand.Add(tile);
        }

        /// <summary>
        /// Adds multiple domino tiles to the player's hand.
        /// </summary>
        /// <param name="tiles">Collection of tiles to add.</param>
        public void AddTiles(IEnumerable<DominoTile> tiles)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles), "Tiles collection cannot be null.");
            }

            foreach (var tile in tiles)
            {
                AddTile(tile);
            }
        }

        /// <summary>
        /// Removes a specific domino tile from the player's hand.
        /// </summary>
        /// <param name="tile">The tile instance to remove.</param>
        /// <returns>True if the tile was found and removed; false otherwise.</returns>
        public bool RemoveTile(DominoTile tile)
        {
            if (tile == null)
            {
                return false;
            }

            return hand.Remove(tile);
        }

        /// <summary>
        /// Checks whether the player's hand contains a specific tile.
        /// </summary>
        /// <param name="tile">The tile to check for.</param>
        /// <returns>True if the tile is in hand, false otherwise.</returns>
        public bool HasTile(DominoTile tile)
        {
            if (tile == null)
            {
                return false;
            }

            return hand.Contains(tile);
        }

        /// <summary>
        /// Clears all tiles from the player's hand.
        /// </summary>
        public void ClearHand()
        {
            hand.Clear();
        }

        /// <summary>
        /// Formats player summary for debugging (e.g., "Player 1 (Human) - 7 tiles").
        /// </summary>
        public override string ToString()
        {
            string type = isHuman ? "Human" : "Opponent";
            return $"{playerName} (ID: {id}, {type}) - {HandCount} tiles";
        }
    }
}
