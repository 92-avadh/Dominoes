using System;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Result structure for a move validation query.
    /// </summary>
    public struct MoveValidationResult
    {
        public bool IsValid;
        public BoardSide TargetSide;
        public DominoTile Tile;
        public string Reason;

        public static MoveValidationResult Valid(DominoTile tile, BoardSide side, string reason = "") => new MoveValidationResult
        {
            IsValid = true,
            TargetSide = side,
            Tile = tile,
            Reason = string.IsNullOrEmpty(reason) ? $"Tile {tile} can be placed on {side} endpoint." : reason
        };

        public static MoveValidationResult Invalid(DominoTile tile, BoardSide side, string reason) => new MoveValidationResult
        {
            IsValid = false,
            TargetSide = side,
            Tile = tile,
            Reason = reason
        };
    }

    /// <summary>
    /// Pure validation service that checks whether a DominoTile can legally connect to an endpoint of the DominoBoard.
    /// Does not modify board state, hands, or turns.
    /// </summary>
    public static class DominoMoveValidator
    {
        /// <summary>
        /// Validates whether a tile can be placed on a specific side of the board.
        /// </summary>
        /// <param name="board">The current domino board.</param>
        /// <param name="tile">The tile proposed for placement.</param>
        /// <param name="side">The target side (Left or Right).</param>
        /// <returns>MoveValidationResult containing valid status and explanatory reason.</returns>
        public static MoveValidationResult ValidateMove(DominoBoard board, DominoTile tile, BoardSide side)
        {
            if (board == null)
            {
                return MoveValidationResult.Invalid(tile, side, "Board reference is null.");
            }

            if (tile == null)
            {
                return MoveValidationResult.Invalid(null, side, "Tile reference is null.");
            }

            // Empty board rule: Any valid tile is playable as the first domino
            if (board.IsEmpty)
            {
                return MoveValidationResult.Valid(tile, side, $"Board is empty. First tile {tile} can be placed on {side}.");
            }

            int targetEndpoint = (side == BoardSide.Left) ? board.LeftEndpoint : board.RightEndpoint;

            // Check if tile matches the target endpoint in either orientation (Left or Right face)
            if (tile.CanConnect(targetEndpoint))
            {
                return MoveValidationResult.Valid(tile, side, $"Tile {tile} connects to {side} endpoint ({targetEndpoint}).");
            }

            return MoveValidationResult.Invalid(
                tile, 
                side, 
                $"Tile {tile} cannot connect to {side} endpoint ({targetEndpoint}). Neither face matches {targetEndpoint}."
            );
        }

        /// <summary>
        /// Checks if a tile can connect anywhere on the board (either Left or Right side).
        /// </summary>
        public static bool CanPlaceAnywhere(DominoBoard board, DominoTile tile, out BoardSide validSide)
        {
            validSide = BoardSide.Left;

            if (board == null || tile == null)
            {
                return false;
            }

            if (board.IsEmpty)
            {
                validSide = BoardSide.Left;
                return true;
            }

            if (ValidateMove(board, tile, BoardSide.Left).IsValid)
            {
                validSide = BoardSide.Left;
                return true;
            }

            if (ValidateMove(board, tile, BoardSide.Right).IsValid)
            {
                validSide = BoardSide.Right;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify move validation across empty board, left/right endpoints, orientations, and invalid moves.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Move Validator")]
        public static void TestMoveValidatorInEditor()
        {
            Debug.Log("================ STARTING MOVE VALIDATOR TEST ================");

            var board = new DominoBoard();

            // 1. Test Empty Board
            Debug.Log("--- Test 1: Empty Board ---");
            var tileFirst = new DominoTile(5, 3);
            var resEmpty = ValidateMove(board, tileFirst, BoardSide.Left);
            Debug.Log($"Empty board placement result: {resEmpty.IsValid} ({resEmpty.Reason})");
            if (resEmpty.IsValid)
            {
                Debug.Log("<color=green>✓ Empty board validation PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Empty board validation FAILED.</color>");
            }

            // Setup Board for following tests: Place [5|4] on board -> Left Endpoint = 5, Right Endpoint = 4
            board.PlaceTile(new DominoTile(5, 4), BoardSide.Left, out _);
            Debug.Log($"\nBoard initialized -> Left Endpoint: {board.LeftEndpoint}, Right Endpoint: {board.RightEndpoint}");

            // 2. Test Valid Left Move with [5|2] (Left end matches Left endpoint 5)
            Debug.Log("\n--- Test 2: Valid Left Move [5|2] on Left (5) ---");
            var tile52 = new DominoTile(5, 2);
            var resLeft1 = ValidateMove(board, tile52, BoardSide.Left);
            Debug.Log($"Result: IsValid={resLeft1.IsValid}, Reason='{resLeft1.Reason}'");
            if (resLeft1.IsValid)
            {
                Debug.Log("<color=green>✓ Valid Left move [5|2] PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Valid Left move [5|2] FAILED.</color>");
            }

            // 3. Test Inverted Orientation on Left: [2|5] (Right end matches Left endpoint 5)
            Debug.Log("\n--- Test 3: Inverted Orientation [2|5] on Left (5) ---");
            var tile25 = new DominoTile(2, 5);
            var resLeft2 = ValidateMove(board, tile25, BoardSide.Left);
            Debug.Log($"Result: IsValid={resLeft2.IsValid}, Reason='{resLeft2.Reason}'");
            if (resLeft2.IsValid)
            {
                Debug.Log("<color=green>✓ Inverted tile orientation [2|5] PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Inverted tile orientation [2|5] FAILED.</color>");
            }

            // 4. Test Valid Right Move with [4|1] (Left end matches Right endpoint 4)
            Debug.Log("\n--- Test 4: Valid Right Move [4|1] on Right (4) ---");
            var tile41 = new DominoTile(4, 1);
            var resRight = ValidateMove(board, tile41, BoardSide.Right);
            Debug.Log($"Result: IsValid={resRight.IsValid}, Reason='{resRight.Reason}'");
            if (resRight.IsValid)
            {
                Debug.Log("<color=green>✓ Valid Right move [4|1] PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Valid Right move [4|1] FAILED.</color>");
            }

            // 5. Test Invalid Move: [3|1] on Left (5) and Right (4)
            Debug.Log("\n--- Test 5: Invalid Move [3|1] against Endpoints 5 and 4 ---");
            var invalidTile = new DominoTile(3, 1);
            var resInvalidLeft = ValidateMove(board, invalidTile, BoardSide.Left);
            var resInvalidRight = ValidateMove(board, invalidTile, BoardSide.Right);
            Debug.Log($"Left Result: IsValid={resInvalidLeft.IsValid}, Reason='{resInvalidLeft.Reason}'");
            Debug.Log($"Right Result: IsValid={resInvalidRight.IsValid}, Reason='{resInvalidRight.Reason}'");
            if (!resInvalidLeft.IsValid && !resInvalidRight.IsValid)
            {
                Debug.Log("<color=green>✓ Invalid move rejection PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Invalid move rejection FAILED.</color>");
            }

            Debug.Log("================ MOVE VALIDATOR TEST COMPLETED ================");
        }
#endif
    }
}
