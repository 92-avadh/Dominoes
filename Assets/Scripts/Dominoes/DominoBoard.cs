using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Represents the playable open ends of the domino board chain.
    /// </summary>
    public enum BoardSide
    {
        Left,
        Right
    }

    /// <summary>
    /// Pure C# data model representing the domino tiles placed on the table and tracking the active open endpoints.
    /// </summary>
    [Serializable]
    public class DominoBoard
    {
        public const int NoEndpoint = -1;

        [SerializeField] private List<DominoTile> placedTiles = new List<DominoTile>();
        [SerializeField] private int leftEndpoint = NoEndpoint;
        [SerializeField] private int rightEndpoint = NoEndpoint;

        /// <summary>
        /// Gets the list of placed tiles in order from the left-most end to the right-most end.
        /// </summary>
        public IReadOnlyList<DominoTile> PlacedTiles => placedTiles;

        /// <summary>
        /// Gets the total number of tiles currently placed on the board.
        /// </summary>
        public int TileCount => placedTiles.Count;

        /// <summary>
        /// Returns true if no tiles have been placed on the board yet.
        /// </summary>
        public bool IsEmpty => placedTiles.Count == 0;

        /// <summary>
        /// Gets the current open value on the left end of the domino chain (or -1 if empty).
        /// </summary>
        public int LeftEndpoint => leftEndpoint;

        /// <summary>
        /// Gets the current open value on the right end of the domino chain (or -1 if empty).
        /// </summary>
        public int RightEndpoint => rightEndpoint;

        /// <summary>
        /// Initializes a new, empty DominoBoard.
        /// </summary>
        public DominoBoard()
        {
            Clear();
        }

        /// <summary>
        /// Clears all tiles from the board and resets endpoints to NoEndpoint (-1).
        /// </summary>
        public void Clear()
        {
            placedTiles.Clear();
            leftEndpoint = NoEndpoint;
            rightEndpoint = NoEndpoint;
        }

        /// <summary>
        /// Checks if a tile can legally connect to the open left endpoint.
        /// (If the board is empty, any non-null tile can connect).
        /// </summary>
        public bool CanConnectLeft(DominoTile tile)
        {
            if (tile == null) return false;
            if (IsEmpty) return true;
            return tile.CanConnect(leftEndpoint);
        }

        /// <summary>
        /// Checks if a tile can legally connect to the open right endpoint.
        /// (If the board is empty, any non-null tile can connect).
        /// </summary>
        public bool CanConnectRight(DominoTile tile)
        {
            if (tile == null) return false;
            if (IsEmpty) return true;
            return tile.CanConnect(rightEndpoint);
        }

        /// <summary>
        /// Checks if a tile can connect to the specified board side.
        /// </summary>
        public bool CanConnect(DominoTile tile, BoardSide side)
        {
            return side == BoardSide.Left ? CanConnectLeft(tile) : CanConnectRight(tile);
        }

        /// <summary>
        /// Checks if a tile can connect to either open end of the board.
        /// </summary>
        public bool CanConnectAnywhere(DominoTile tile)
        {
            return CanConnectLeft(tile) || CanConnectRight(tile);
        }

        /// <summary>
        /// Places a tile onto the board at the specified side (Left or Right).
        /// When placing on an empty board, both endpoints are initialized to the tile's left and right values.
        /// </summary>
        /// <param name="tile">The tile to place.</param>
        /// <param name="side">The side to connect to (Left or Right).</param>
        /// <param name="errorMessage">Error description if placement fails.</param>
        /// <returns>True if placed successfully, false otherwise.</returns>
        public bool PlaceTile(DominoTile tile, BoardSide side, out string errorMessage)
        {
            if (tile == null)
            {
                errorMessage = "Cannot place a null tile on the board.";
                Debug.LogWarning($"[DominoBoard] {errorMessage}");
                return false;
            }

            // Case 1: First tile on the board
            if (IsEmpty)
            {
                placedTiles.Add(tile);
                leftEndpoint = tile.Left;
                rightEndpoint = tile.Right;
                errorMessage = string.Empty;
                return true;
            }

            // Case 2: Placing on the Left side
            if (side == BoardSide.Left)
            {
                if (!tile.CanConnect(leftEndpoint))
                {
                    errorMessage = $"Tile {tile} cannot connect to Left endpoint ({leftEndpoint}).";
                    Debug.LogWarning($"[DominoBoard] {errorMessage}");
                    return false;
                }

                // The matching end connects to leftEndpoint; the opposite end becomes the new leftEndpoint
                leftEndpoint = tile.GetOppositeValue(leftEndpoint);
                placedTiles.Insert(0, tile);
                errorMessage = string.Empty;
                return true;
            }

            // Case 3: Placing on the Right side
            if (side == BoardSide.Right)
            {
                if (!tile.CanConnect(rightEndpoint))
                {
                    errorMessage = $"Tile {tile} cannot connect to Right endpoint ({rightEndpoint}).";
                    Debug.LogWarning($"[DominoBoard] {errorMessage}");
                    return false;
                }

                // The matching end connects to rightEndpoint; the opposite end becomes the new rightEndpoint
                rightEndpoint = tile.GetOppositeValue(rightEndpoint);
                placedTiles.Add(tile);
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = $"Unknown board side: {side}.";
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify empty board state, initial placement, endpoint updates, and invalid placement rejection.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Board")]
        public static void TestBoardInEditor()
        {
            Debug.Log("================ STARTING DOMINO BOARD TEST ================");

            var board = new DominoBoard();

            // 1. Test Empty Board
            Debug.Log("--- Test 1: Empty Board ---");
            Debug.Log($"IsEmpty: {board.IsEmpty}, Count: {board.TileCount}, LeftEndpoint: {board.LeftEndpoint}, RightEndpoint: {board.RightEndpoint}");
            if (board.IsEmpty && board.LeftEndpoint == DominoBoard.NoEndpoint && board.RightEndpoint == DominoBoard.NoEndpoint)
            {
                Debug.Log("<color=green>✓ Empty board test PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Empty board test FAILED.</color>");
                return;
            }

            // 2. Test Placing First Tile [6|4]
            Debug.Log("\n--- Test 2: Placing First Tile [6|4] ---");
            var firstTile = new DominoTile(6, 4);
            bool firstPlaced = board.PlaceTile(firstTile, BoardSide.Left, out string err1);
            Debug.Log($"Placed {firstTile}: {firstPlaced}. LeftEndpoint = {board.LeftEndpoint}, RightEndpoint = {board.RightEndpoint}");
            if (firstPlaced && board.LeftEndpoint == 6 && board.RightEndpoint == 4 && board.TileCount == 1)
            {
                Debug.Log("<color=green>✓ First tile placement PASSED.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>✗ First tile placement FAILED: {err1}</color>");
                return;
            }

            // 3. Test Connecting to Left Endpoint with [6|2] (Left endpoint should become 2)
            Debug.Log("\n--- Test 3: Connecting to Left with [6|2] ---");
            var leftTile = new DominoTile(6, 2);
            bool leftPlaced = board.PlaceTile(leftTile, BoardSide.Left, out string err2);
            Debug.Log($"Placed {leftTile} on Left: {leftPlaced}. New LeftEndpoint = {board.LeftEndpoint}, RightEndpoint = {board.RightEndpoint}");
            if (leftPlaced && board.LeftEndpoint == 2 && board.RightEndpoint == 4 && board.TileCount == 2)
            {
                Debug.Log("<color=green>✓ Left placement & endpoint update PASSED.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>✗ Left placement FAILED: {err2}</color>");
                return;
            }

            // 4. Test Connecting to Right Endpoint with [4|5] (Right endpoint should become 5)
            Debug.Log("\n--- Test 4: Connecting to Right with [4|5] ---");
            var rightTile = new DominoTile(4, 5);
            bool rightPlaced = board.PlaceTile(rightTile, BoardSide.Right, out string err3);
            Debug.Log($"Placed {rightTile} on Right: {rightPlaced}. LeftEndpoint = {board.LeftEndpoint}, New RightEndpoint = {board.RightEndpoint}");
            if (rightPlaced && board.LeftEndpoint == 2 && board.RightEndpoint == 5 && board.TileCount == 3)
            {
                Debug.Log("<color=green>✓ Right placement & endpoint update PASSED.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>✗ Right placement FAILED: {err3}</color>");
                return;
            }

            // 5. Test Invalid Placement Rejection: Placing [3|3] on Left (Left endpoint is 2, [3|3] does not match)
            Debug.Log($"\n--- Test 5: Rejection of Invalid Placement [3|3] on Left (Left is {board.LeftEndpoint}) ---");
            var invalidTile = new DominoTile(3, 3);
            bool invalidPlaced = board.PlaceTile(invalidTile, BoardSide.Left, out string err4);
            Debug.Log($"Placement rejected as expected: {!invalidPlaced} (Error: '{err4}')");
            if (!invalidPlaced)
            {
                Debug.Log("<color=green>✓ Invalid placement rejection PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Invalid placement was incorrectly accepted!</color>");
                return;
            }

            // 6. Test Board Chain Summary
            Debug.Log($"\nBoard Placed Tiles Chain ({board.TileCount} tiles): {string.Join(" - ", board.PlacedTiles)}");
            Debug.Log($"Active Open Ends -> Left: {board.LeftEndpoint}, Right: {board.RightEndpoint}");

            Debug.Log("================ DOMINO BOARD TEST COMPLETED ================");
        }
#endif
    }
}
