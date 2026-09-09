using System;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Pure C# immutable data model representing a single double-six Domino tile (values 0 to 6 on each end).
    /// Implements IEquatable and canonical value equality for deterministic state synchronization.
    /// </summary>
    [Serializable]
    public class DominoTile : IEquatable<DominoTile>
    {
        public const int MinValue = 0;
        public const int MaxValue = 6;

        [SerializeField] private int left;
        [SerializeField] private int right;

        /// <summary>
        /// Gets the left end value of the domino (0 to 6).
        /// </summary>
        public int Left => left;

        /// <summary>
        /// Gets the right end value of the domino (0 to 6).
        /// </summary>
        public int Right => right;

        /// <summary>
        /// Returns true if both ends of the domino have the same value (e.g., 6|6, 0|0).
        /// </summary>
        public bool IsDouble => left == right;

        /// <summary>
        /// Gets the sum of both ends of the domino tile (0 to 12).
        /// </summary>
        public int TotalPips => left + right;

        /// <summary>
        /// Safe constructor that validates tile values are within the valid 0-6 range.
        /// </summary>
        /// <param name="left">Left end value (0 to 6).</param>
        /// <param name="right">Right end value (0 to 6).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if left or right is outside 0 to 6.</exception>
        public DominoTile(int left, int right)
        {
            if (!IsValidValue(left))
            {
                throw new ArgumentOutOfRangeException(nameof(left), left, $"Tile value must be between {MinValue} and {MaxValue}.");
            }

            if (!IsValidValue(right))
            {
                throw new ArgumentOutOfRangeException(nameof(right), right, $"Tile value must be between {MinValue} and {MaxValue}.");
            }

            this.left = left;
            this.right = right;
        }

        /// <summary>
        /// Validates whether a given integer is a valid domino face value (0 to 6).
        /// </summary>
        public static bool IsValidValue(int value)
        {
            return value >= MinValue && value <= MaxValue;
        }

        /// <summary>
        /// Checks whether this tile can connect to a specified open end value.
        /// </summary>
        /// <param name="value">The open board end value to match against.</param>
        /// <returns>True if either the left or right value matches the specified value.</returns>
        public bool CanConnect(int value)
        {
            return left == value || right == value;
        }

        /// <summary>
        /// Given a value that connects to one end of this tile, returns the opposite end's value.
        /// </summary>
        /// <param name="connectingValue">The value connecting to this tile.</param>
        /// <returns>The opposite end value.</returns>
        /// <exception cref="ArgumentException">Thrown if the tile does not match the connecting value.</exception>
        public int GetOppositeValue(int connectingValue)
        {
            if (left == connectingValue)
            {
                return right;
            }

            if (right == connectingValue)
            {
                return left;
            }

            throw new ArgumentException($"Tile [{left}|{right}] cannot connect to value {connectingValue}.", nameof(connectingValue));
        }

        public bool Equals(DominoTile other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            int thisMin = Math.Min(left, right);
            int thisMax = Math.Max(left, right);
            int otherMin = Math.Min(other.left, other.right);
            int otherMax = Math.Max(other.left, other.right);

            return thisMin == otherMin && thisMax == otherMax;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DominoTile)obj);
        }

        public override int GetHashCode()
        {
            int min = Math.Min(left, right);
            int max = Math.Max(left, right);
            return HashCode.Combine(min, max);
        }

        public static bool operator ==(DominoTile left, DominoTile right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(DominoTile left, DominoTile right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Formats the tile for easy debugging and logging (e.g. "[6|4]").
        /// </summary>
        public override string ToString()
        {
            return $"[{left}|{right}]";
        }
    }
}
