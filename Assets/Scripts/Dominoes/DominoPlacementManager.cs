using System;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Result returned when attempting to place a domino tile on the board.
    /// </summary>
    public struct PlacementResult
    {
        public bool Success;
        public DominoPlayer Player;
        public DominoTile Tile;
        public BoardSide Side;
        public string Message;

        public static PlacementResult Ok(DominoPlayer player, DominoTile tile, BoardSide side, string message = "") => new PlacementResult
        {
            Success = true,
            Player = player,
            Tile = tile,
            Side = side,
            Message = string.IsNullOrEmpty(message) ? $"Tile {tile} placed successfully on {side} side." : message
        };

        public static PlacementResult Fail(DominoPlayer player, DominoTile tile, BoardSide side, string reason) => new PlacementResult
        {
            Success = false,
            Player = player,
            Tile = tile,
            Side = side,
            Message = reason
        };
    }

    /// <summary>
    /// Coordinates the logical placement of a player's tile onto the domino board.
    /// Validates ownership, checks board legality, and atomically updates hand and board state.
    /// </summary>
    [Serializable]
    public class DominoPlacementManager
    {
        /// <summary>
        /// Attempts to execute a tile placement move for a player onto the board.
        /// </summary>
        /// <param name="board">The active DominoBoard.</param>
        /// <param name="player">The player making the move.</param>
        /// <param name="tile">The domino tile to place.</param>
        /// <param name="side">The side of the board to place on (Left or Right).</param>
        /// <returns>PlacementResult indicating success or failure and reason.</returns>
        public PlacementResult PlaceTile(DominoBoard board, DominoPlayer player, DominoTile tile, BoardSide side)
        {
            if (board == null)
            {
                return PlacementResult.Fail(player, tile, side, "Board reference cannot be null.");
            }

            if (player == null)
            {
                return PlacementResult.Fail(null, tile, side, "Player reference cannot be null.");
            }

            if (tile == null)
            {
                return PlacementResult.Fail(player, null, side, "Tile reference cannot be null.");
            }

            // 1. Verify tile exists in the player's hand
            if (!player.HasTile(tile))
            {
                return PlacementResult.Fail(player, tile, side, $"Player '{player.PlayerName}' does not hold tile {tile} in hand.");
            }

            // 2. Validate move legality with DominoMoveValidator
            var validation = DominoMoveValidator.ValidateMove(board, tile, side);
            if (!validation.IsValid)
            {
                return PlacementResult.Fail(player, tile, side, $"Illegal move: {validation.Reason}");
            }

            // 3. Place tile on the board
            bool boardPlaced = board.PlaceTile(tile, side, out string boardError);
            if (!boardPlaced)
            {
                return PlacementResult.Fail(player, tile, side, $"Board placement failed: {boardError}");
            }

            // 4. Remove tile from the player's hand
            bool handRemoved = player.RemoveTile(tile);
            if (!handRemoved)
            {
                Debug.LogError($"[DominoPlacementManager] Critical error: Tile {tile} placed on board but failed to remove from hand.");
            }

            return PlacementResult.Ok(
                player,
                tile,
                side,
                $"'{player.PlayerName}' played {tile} on {side}. Board ends now -> Left: {board.LeftEndpoint}, Right: {board.RightEndpoint}."
            );
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify first tile placement, left/right placements, orientation matching, and invalid move safety.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Placement Manager")]
        public static void TestPlacementManagerInEditor()
        {
            Debug.Log("================ STARTING PLACEMENT MANAGER TEST ================");

            var placementManager = new DominoPlacementManager();
            var board = new DominoBoard();
            var player = new DominoPlayer(1, "Alice", isHuman: true);

            // Give player specific tiles: [6|4], [6|2], [1|4], [3|3]
            var tileFirst = new DominoTile(6, 4);
            var tileLeft = new DominoTile(6, 2);   // Will test Left orientation
            var tileRight = new DominoTile(1, 4);  // Will test Right inverted orientation ([1|4] against right endpoint 4)
            var tileInvalid = new DominoTile(3, 3); // Unrelated double 3

            player.AddTile(tileFirst);
            player.AddTile(tileLeft);
            player.AddTile(tileRight);
            player.AddTile(tileInvalid);

            int initialHandCount = player.HandCount;
            Debug.Log($"Initial Hand ({initialHandCount} tiles): {string.Join(", ", player.Hand)}");

            // 1. Test First Tile Placement on Empty Board
            Debug.Log("--- Test 1: First Tile Placement [6|4] ---");
            var res1 = placementManager.PlaceTile(board, player, tileFirst, BoardSide.Left);
            Debug.Log($"Result 1: {res1.Success} -> {res1.Message}");
            Debug.Log($"Player Hand Count: {player.HandCount} (Expected: 3), Board Count: {board.TileCount} (Expected: 1)");
            if (res1.Success && player.HandCount == 3 && board.LeftEndpoint == 6 && board.RightEndpoint == 4)
            {
                Debug.Log("<color=green>✓ First tile placement PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ First tile placement FAILED.</color>");
            }

            // 2. Test Left-Side Placement with [6|2] (Connects 6 to 6, Left becomes 2)
            Debug.Log("\n--- Test 2: Left-Side Placement [6|2] ---");
            var res2 = placementManager.PlaceTile(board, player, tileLeft, BoardSide.Left);
            Debug.Log($"Result 2: {res2.Success} -> {res2.Message}");
            Debug.Log($"Player Hand Count: {player.HandCount} (Expected: 2), Left Endpoint: {board.LeftEndpoint} (Expected: 2)");
            if (res2.Success && player.HandCount == 2 && board.LeftEndpoint == 2 && board.RightEndpoint == 4)
            {
                Debug.Log("<color=green>✓ Left placement PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Left placement FAILED.</color>");
            }

            // 3. Test Right-Side Inverted Placement with [1|4] (Connects 4 to 4, Right becomes 1)
            Debug.Log("\n--- Test 3: Right-Side Inverted Placement [1|4] ---");
            var res3 = placementManager.PlaceTile(board, player, tileRight, BoardSide.Right);
            Debug.Log($"Result 3: {res3.Success} -> {res3.Message}");
            Debug.Log($"Player Hand Count: {player.HandCount} (Expected: 1), Right Endpoint: {board.RightEndpoint} (Expected: 1)");
            if (res3.Success && player.HandCount == 1 && board.LeftEndpoint == 2 && board.RightEndpoint == 1)
            {
                Debug.Log("<color=green>✓ Right placement & inverted orientation PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Right placement FAILED.</color>");
            }

            // 4. Test Invalid Move with [3|3] (Board endpoints are 2 and 1)
            Debug.Log("\n--- Test 4: Invalid Move [3|3] against Endpoints 2 and 1 ---");
            int handBeforeInvalid = player.HandCount;
            int boardBeforeInvalid = board.TileCount;
            var res4 = placementManager.PlaceTile(board, player, tileInvalid, BoardSide.Left);
            Debug.Log($"Result 4: Success={res4.Success}, Message='{res4.Message}'");
            Debug.Log($"Hand before={handBeforeInvalid}, Hand after={player.HandCount}. Board before={boardBeforeInvalid}, Board after={board.TileCount}");
            if (!res4.Success && player.HandCount == handBeforeInvalid && board.TileCount == boardBeforeInvalid)
            {
                Debug.Log("<color=green>✓ Invalid move rejected without changing hand or board state PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Invalid move test FAILED.</color>");
            }

            // 5. Test Tile Not in Hand
            Debug.Log("\n--- Test 5: Placing Tile Not in Hand [0|0] ---");
            var unownedTile = new DominoTile(0, 0);
            var res5 = placementManager.PlaceTile(board, player, unownedTile, BoardSide.Left);
            Debug.Log($"Result 5: Success={res5.Success}, Message='{res5.Message}'");
            if (!res5.Success)
            {
                Debug.Log("<color=green>✓ Unowned tile rejection PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Unowned tile test FAILED.</color>");
            }

            Debug.Log($"\nFinal Placed Chain: {string.Join(" - ", board.PlacedTiles)}");
            Debug.Log("================ PLACEMENT MANAGER TEST COMPLETED ================");
        }
#endif
    }
}
