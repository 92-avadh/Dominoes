using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Configurable modes for handling turns when a player has no valid moves.
    /// </summary>
    public enum PassDrawMode
    {
        Pass, // Player skips turn without drawing
        Draw  // Player draws a tile from the remaining boneyard pool
    }

    /// <summary>
    /// Result structure detailing the outcome of a pass or draw attempt.
    /// </summary>
    public struct PassDrawResult
    {
        public bool Success;
        public PassDrawMode Mode;
        public DominoPlayer Player;
        public DominoTile DrawnTile;
        public bool HadPlayableMove;
        public string Message;

        public static PassDrawResult Passed(DominoPlayer player, string message = "") => new PassDrawResult
        {
            Success = true,
            Mode = PassDrawMode.Pass,
            Player = player,
            DrawnTile = null,
            HadPlayableMove = false,
            Message = string.IsNullOrEmpty(message) ? $"Player '{player.PlayerName}' passed." : message
        };

        public static PassDrawResult Drew(DominoPlayer player, DominoTile tile, string message = "") => new PassDrawResult
        {
            Success = true,
            Mode = PassDrawMode.Draw,
            Player = player,
            DrawnTile = tile,
            HadPlayableMove = false,
            Message = string.IsNullOrEmpty(message) ? $"Player '{player.PlayerName}' drew tile {tile}." : message
        };

        public static PassDrawResult Fail(DominoPlayer player, PassDrawMode mode, bool hadPlayableMove, string reason) => new PassDrawResult
        {
            Success = false,
            Mode = mode,
            Player = player,
            DrawnTile = null,
            HadPlayableMove = hadPlayableMove,
            Message = reason
        };
    }

    /// <summary>
    /// Configurable coordinator for handling situations where a player cannot make a valid move on the board.
    /// Supports both Pass and Draw game rules without hardcoding unconfirmed game variants.
    /// </summary>
    [Serializable]
    public class DominoPassDrawManager
    {
        [SerializeField] private PassDrawMode mode = PassDrawMode.Draw;

        /// <summary>
        /// Gets or sets the active pass/draw mode (Pass or Draw).
        /// </summary>
        public PassDrawMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public DominoPassDrawManager(PassDrawMode mode = PassDrawMode.Draw)
        {
            this.mode = mode;
        }

        /// <summary>
        /// Checks whether the player currently holds at least one tile that can be legally placed on the board.
        /// </summary>
        public bool HasPlayableTile(DominoBoard board, DominoPlayer player)
        {
            if (board == null || player == null || player.HandCount == 0)
            {
                return false;
            }

            if (board.IsEmpty)
            {
                return true; // Any tile can be played on an empty board
            }

            foreach (var tile in player.Hand)
            {
                if (DominoMoveValidator.CanPlaceAnywhere(board, tile, out _))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the player is legally allowed to Draw from the boneyard.
        /// Rule: Allowed ONLY when the player holds no playable tiles and boneyard has tiles available.
        /// </summary>
        public bool CanDraw(DominoBoard board, DominoPlayer player, DominoDealer dealer)
        {
            if (player == null || board == null || dealer == null) return false;
            return !HasPlayableTile(board, player) && dealer.RemainingCount > 0;
        }

        /// <summary>
        /// Overload taking int boneyardCount for UI button queries.
        /// </summary>
        public bool CanDraw(DominoBoard board, DominoPlayer player, int boneyardCount)
        {
            if (player == null || board == null) return false;
            return !HasPlayableTile(board, player) && boneyardCount > 0;
        }

        /// <summary>
        /// Determines if the player is legally allowed to Pass their turn.
        /// Rule: Allowed ONLY when the player holds no playable tiles AND the boneyard is empty (or in Pass/Block game mode).
        /// </summary>
        public bool CanPass(DominoBoard board, DominoPlayer player, DominoDealer dealer)
        {
            if (player == null || board == null) return false;
            if (HasPlayableTile(board, player)) return false;
            return (mode == PassDrawMode.Pass) || (dealer == null || dealer.RemainingCount == 0);
        }

        /// <summary>
        /// Overload taking int boneyardCount for UI button queries.
        /// </summary>
        public bool CanPass(DominoBoard board, DominoPlayer player, int boneyardCount)
        {
            if (player == null || board == null) return false;
            if (HasPlayableTile(board, player)) return false;
            return (mode == PassDrawMode.Pass) || (boneyardCount == 0);
        }

        /// <summary>
        /// Executes a single tile draw for the active player from the boneyard pool.
        /// </summary>
        public PassDrawResult ExecuteDraw(DominoBoard board, DominoPlayer player, DominoDealer dealer)
        {
            if (player == null)
            {
                return PassDrawResult.Fail(null, PassDrawMode.Draw, false, "Player reference cannot be null.");
            }

            if (board == null)
            {
                return PassDrawResult.Fail(player, PassDrawMode.Draw, false, "Board reference cannot be null.");
            }

            if (HasPlayableTile(board, player))
            {
                return PassDrawResult.Fail(
                    player,
                    PassDrawMode.Draw,
                    true,
                    "Cannot draw: You already hold a playable domino in your hand!"
                );
            }

            if (dealer == null || dealer.RemainingCount == 0)
            {
                return PassDrawResult.Fail(
                    player,
                    PassDrawMode.Draw,
                    false,
                    "Cannot draw: The boneyard pool is empty. You must Pass your turn."
                );
            }

            DominoTile drawnTile = dealer.DrawTile();
            if (drawnTile == null)
            {
                return PassDrawResult.Fail(player, PassDrawMode.Draw, false, "Boneyard is empty.");
            }

            player.AddTile(drawnTile);
            return PassDrawResult.Drew(player, drawnTile, $"Player '{player.PlayerName}' drew tile [{drawnTile.Left}|{drawnTile.Right}].");
        }

        /// <summary>
        /// Executes a Pass action skipping the active player's turn.
        /// </summary>
        public PassDrawResult ExecutePass(DominoBoard board, DominoPlayer player, DominoDealer dealer = null)
        {
            if (player == null)
            {
                return PassDrawResult.Fail(null, PassDrawMode.Pass, false, "Player reference cannot be null.");
            }

            if (board == null)
            {
                return PassDrawResult.Fail(player, PassDrawMode.Pass, false, "Board reference cannot be null.");
            }

            if (HasPlayableTile(board, player))
            {
                return PassDrawResult.Fail(
                    player,
                    PassDrawMode.Pass,
                    true,
                    "Cannot pass: You hold a playable domino in your hand!"
                );
            }

            if (mode == PassDrawMode.Draw && dealer != null && dealer.RemainingCount > 0)
            {
                return PassDrawResult.Fail(
                    player,
                    PassDrawMode.Pass,
                    false,
                    "Cannot pass: You must draw from the boneyard first!"
                );
            }

            return PassDrawResult.Passed(player, $"Player '{player.PlayerName}' passed turn.");
        }

        /// <summary>
        /// Attempts to execute the configured Pass or Draw action for the specified player.
        /// Rejects the action if the player holds any playable tile.
        /// In Draw mode, automatically falls back to Pass when boneyard is empty.
        /// </summary>
        public PassDrawResult ExecutePassOrDraw(DominoBoard board, DominoPlayer player, DominoDealer dealer = null)
        {
            if (player == null)
            {
                return PassDrawResult.Fail(null, mode, false, "Player reference cannot be null.");
            }

            if (board == null)
            {
                return PassDrawResult.Fail(player, mode, false, "Board reference cannot be null.");
            }

            if (HasPlayableTile(board, player))
            {
                return PassDrawResult.Fail(
                    player,
                    mode,
                    true,
                    $"Action rejected: Player '{player.PlayerName}' holds at least one playable tile."
                );
            }

            if (mode == PassDrawMode.Pass)
            {
                return ExecutePass(board, player, dealer);
            }

            if (mode == PassDrawMode.Draw)
            {
                if (dealer != null && dealer.RemainingCount > 0)
                {
                    return ExecuteDraw(board, player, dealer);
                }
                else
                {
                    // Boneyard empty -> Execute Pass
                    return ExecutePass(board, player, dealer);
                }
            }

            return PassDrawResult.Fail(player, mode, false, $"Unknown mode: {mode}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor test menu item to verify Pass/Draw behavior, playable tile blocking, and empty boneyard safety.
        /// </summary>
        [UnityEditor.MenuItem("Dominoes/Test Pass Draw Manager")]
        public static void TestPassDrawManagerInEditor()
        {
            Debug.Log("================ STARTING PASS/DRAW MANAGER TEST ================");

            var manager = new DominoPassDrawManager();
            var board = new DominoBoard();
            var player = new DominoPlayer(1, "Alice", isHuman: true);
            var dealer = new DominoDealer();

            // Setup Board: [5|4] placed -> endpoints are 5 and 4
            board.PlaceTile(new DominoTile(5, 4), BoardSide.Left, out _);
            Debug.Log($"Board endpoints -> Left: {board.LeftEndpoint}, Right: {board.RightEndpoint}");

            // 1. Test Player HAS a playable tile [5|2] -> Pass/Draw should be BLOCKED
            Debug.Log("--- Test 1: Player has playable tile [5|2] ---");
            player.ClearHand();
            player.AddTile(new DominoTile(5, 2)); // Can connect to 5
            bool hasPlayable = manager.HasPlayableTile(board, player);
            var resBlocked = manager.ExecutePassOrDraw(board, player, dealer);
            Debug.Log($"HasPlayable: {hasPlayable}. Action executed: {resBlocked.Success} (Message: '{resBlocked.Message}')");
            if (hasPlayable && !resBlocked.Success && resBlocked.HadPlayableMove)
            {
                Debug.Log("<color=green>✓ Blocked unnecessary pass/draw PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Blocked unnecessary pass/draw FAILED.</color>");
            }

            // 2. Test Player has NO playable tile [0|0] in PASS Mode
            Debug.Log("\n--- Test 2: Player has [0|0] in PASS Mode ---");
            manager.Mode = PassDrawMode.Pass;
            player.ClearHand();
            player.AddTile(new DominoTile(0, 0)); // Cannot connect to 5 or 4
            int handBeforePass = player.HandCount;
            int boardBeforePass = board.TileCount;
            var resPass = manager.ExecutePassOrDraw(board, player, dealer);
            Debug.Log($"Pass result: Success={resPass.Success}, Hand count={player.HandCount}, Board count={board.TileCount}");
            if (resPass.Success && resPass.Mode == PassDrawMode.Pass && player.HandCount == handBeforePass && board.TileCount == boardBeforePass)
            {
                Debug.Log("<color=green>✓ PASS mode PASSED without mutating hand or board.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ PASS mode FAILED.</color>");
            }

            // 3. Test Player has NO playable tile [0|0] in DRAW Mode
            Debug.Log("\n--- Test 3: Player has [0|0] in DRAW Mode ---");
            manager.Mode = PassDrawMode.Draw;
            dealer.Deal(new List<DominoPlayer> { player, new DominoPlayer(2, "Bob") }, 2, out _); // Leaves 24 tiles in dealer
            player.ClearHand();
            player.AddTile(new DominoTile(0, 0));
            int remainingBefore = dealer.RemainingCount;
            int handBeforeDraw = player.HandCount;

            var resDraw = manager.ExecutePassOrDraw(board, player, dealer);
            Debug.Log($"Draw result: Success={resDraw.Success}, Drew tile={resDraw.DrawnTile}, New hand count={player.HandCount}, Remaining boneyard={dealer.RemainingCount}");
            if (resDraw.Success && player.HandCount == handBeforeDraw + 1 && dealer.RemainingCount == remainingBefore - 1)
            {
                Debug.Log("<color=green>✓ DRAW mode PASSED: 1 tile drawn from dealer into hand.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ DRAW mode FAILED.</color>");
            }

            // 4. Test DRAW Mode when boneyard is empty
            Debug.Log("\n--- Test 4: DRAW Mode when boneyard is empty ---");
            while (dealer.RemainingCount > 0)
            {
                dealer.DrawTile(); // Drain boneyard
            }
            player.ClearHand();
            player.AddTile(new DominoTile(0, 0));
            var resEmptyBoneyard = manager.ExecutePassOrDraw(board, player, dealer);
            Debug.Log($"Empty boneyard result: Success={resEmptyBoneyard.Success} (Message: '{resEmptyBoneyard.Message}')");
            if (!resEmptyBoneyard.Success && dealer.RemainingCount == 0)
            {
                Debug.Log("<color=green>✓ Empty boneyard handling PASSED.</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Empty boneyard test FAILED.</color>");
            }

            Debug.Log("================ PASS/DRAW MANAGER TEST COMPLETED ================");
        }
#endif
    }
}
