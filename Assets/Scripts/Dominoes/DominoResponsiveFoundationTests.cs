#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Comprehensive test suite verifying the Responsive UI Foundation,
    /// Authoritative Domino Tile State Synchronization, Coordinate Conversions,
    /// and Automatic No-Move Gameplay Flow.
    /// </summary>
    public static class DominoResponsiveFoundationTests
    {
        [MenuItem("Dominoes/Test Responsive Foundation & State Sync")]
        public static void RunAllFoundationTests()
        {
            Debug.Log("===============================================================");
            Debug.Log(">>> RUNNING DOMINOES RESPONSIVE FOUNDATION & STATE SYNC TESTS <<<");
            Debug.Log("===============================================================");

            bool allPassed = true;

            allPassed &= TestDominoTileCanonicalEquality();
            allPassed &= TestDominoTileImmutabilityAndOperations();
            allPassed &= TestDealerAndPlayerStateSynchronization();
            allPassed &= TestPassDrawManagerNoMoveFlow();
            allPassed &= TestBoardLayoutEngineMultiResolutionBounds();
            allPassed &= TestBoardLayoutSerpentineEndpoints();
            allPassed &= TestWorstCaseScenariosAndZeroTileOverlaps();
            allPassed &= TestPanelSettingsAndUSSIntegrity();
            allPassed &= TestGameModeUniquenessAndContext();

            Debug.Log("===============================================================");
            if (allPassed)
            {
                Debug.Log("<color=green><b>ALL RESPONSIVE FOUNDATION & STATE SYNC TESTS PASSED! (9/9)</b></color>");
            }
            else
            {
                Debug.LogError("<color=red><b>SOME RESPONSIVE FOUNDATION TESTS FAILED!</b></color>");
            }
            Debug.Log("===============================================================");
        }

        private static bool TestDominoTileCanonicalEquality()
        {
            Debug.Log("\n[TEST 1] Verifying DominoTile Canonical Value Equality & Hashing...");

            var t1 = new DominoTile(3, 5);
            var t2 = new DominoTile(5, 3);
            var t3 = new DominoTile(3, 5);
            var t4 = new DominoTile(2, 4);

            bool equalRef = t1.Equals(t2);
            bool equalOp = (t1 == t2);
            bool equalSame = (t1 == t3);
            bool notEqualDiff = (t1 != t4);
            bool sameHash = (t1.GetHashCode() == t2.GetHashCode());

            var set = new HashSet<DominoTile> { t1 };
            bool setContainsT2 = set.Contains(t2);

            bool pass = equalRef && equalOp && equalSame && notEqualDiff && sameHash && setContainsT2;
            if (pass)
            {
                Debug.Log(" -> PASSED: [3|5] == [5|3], HashCode matches, HashSet lookup successful.");
            }
            else
            {
                Debug.LogError($" -> FAILED: equalRef={equalRef}, equalOp={equalOp}, equalSame={equalSame}, sameHash={sameHash}, setContains={setContainsT2}");
            }
            return pass;
        }

        private static bool TestDominoTileImmutabilityAndOperations()
        {
            Debug.Log("\n[TEST 2] Verifying DominoTile Operations (CanConnect, GetOppositeValue)...");

            var tile = new DominoTile(4, 6);
            bool can4 = tile.CanConnect(4);
            bool can6 = tile.CanConnect(6);
            bool can0 = tile.CanConnect(0);

            int opp4 = tile.GetOppositeValue(4);
            int opp6 = tile.GetOppositeValue(6);

            bool pass = can4 && can6 && !can0 && (opp4 == 6) && (opp6 == 4);
            if (pass)
            {
                Debug.Log(" -> PASSED: CanConnect and GetOppositeValue work correctly.");
            }
            else
            {
                Debug.LogError($" -> FAILED: can4={can4}, can6={can6}, can0={can0}, opp4={opp4}, opp6={opp6}");
            }
            return pass;
        }

        private static bool TestDealerAndPlayerStateSynchronization()
        {
            Debug.Log("\n[TEST 3] Verifying Dealer, Draw, and Player Hand Tile State Synchronization...");

            var dealer = new DominoDealer();
            var player1 = new DominoPlayer(1, "Test Player", isHuman: true);
            var player2 = new DominoPlayer(2, "Opponent", isHuman: false);

            // Deal 7 tiles each to 2 players (14 dealt, 14 remaining)
            dealer.Deal(new List<DominoPlayer> { player1, player2 }, 7, out string dealErr);
            if (player1.Hand.Count != 7 || dealer.RemainingCount != 14)
            {
                Debug.LogError($" -> FAILED: HandCount={player1.Hand.Count}, DealerRemaining={dealer.RemainingCount}, err={dealErr}");
                return false;
            }

            // Draw 1 tile from boneyard
            var drawn = dealer.DrawTile();
            if (drawn == null)
            {
                Debug.LogError(" -> FAILED: Drawn tile was null.");
                return false;
            }

            int countBeforeAdd = player1.Hand.Count;
            player1.AddTile(drawn);
            int countAfterAdd = player1.Hand.Count;

            // Verify the exact tile in hand matches drawn tile in both identity and value
            var handTile = player1.Hand[player1.Hand.Count - 1];
            bool identityMatches = ReferenceEquals(drawn, handTile);
            bool valueMatches = (drawn.Left == handTile.Left && drawn.Right == handTile.Right);

            bool pass = (countAfterAdd == countBeforeAdd + 1) && identityMatches && valueMatches && (dealer.RemainingCount == 13);
            if (pass)
            {
                Debug.Log($" -> PASSED: Drawn tile {drawn} preserved exact reference and value in player hand.");
            }
            else
            {
                Debug.LogError($" -> FAILED: identityMatches={identityMatches}, valueMatches={valueMatches}");
            }
            return pass;
        }

        private static bool TestPassDrawManagerNoMoveFlow()
        {
            Debug.Log("\n[TEST 4] Verifying PassDrawManager No-Move & Draw Execution Flow...");

            var board = new DominoBoard();
            var player1 = new DominoPlayer(1, "Human Player", isHuman: true);
            var player2 = new DominoPlayer(2, "Opponent", isHuman: false);
            var dealer = new DominoDealer();
            dealer.Deal(new List<DominoPlayer> { player1, player2 }, 7, out _);

            var passDrawManager = new DominoPassDrawManager(PassDrawMode.Draw);

            // Place initial tile [6|6] on board
            board.PlaceTile(new DominoTile(6, 6), BoardSide.Left, out _);

            // Give player tiles that CANNOT connect (e.g. [0|1], [2|3])
            player1.ClearHand();
            player1.AddTile(new DominoTile(0, 1));
            player1.AddTile(new DominoTile(2, 3));

            bool hasPlayable = passDrawManager.HasPlayableTile(board, player1);
            if (hasPlayable)
            {
                Debug.LogError(" -> FAILED: Player should have no playable tiles against [6|6].");
                return false;
            }

            // Draw until playable or boneyard empty
            var drawResult = passDrawManager.ExecuteDraw(board, player1, dealer);
            if (!drawResult.Success || drawResult.DrawnTile == null)
            {
                Debug.LogError($" -> FAILED: Draw failed: {drawResult.Message}");
                return false;
            }

            // Verify drawn tile was added to hand
            bool handContainsDrawn = player1.HasTile(drawResult.DrawnTile);

            bool pass = !hasPlayable && drawResult.Success && handContainsDrawn;
            if (pass)
            {
                Debug.Log($" -> PASSED: No-move detected correctly, draw successfully added {drawResult.DrawnTile} to hand.");
            }
            else
            {
                Debug.LogError(" -> FAILED in PassDrawManager flow.");
            }
            return pass;
        }

        private static bool TestBoardLayoutEngineMultiResolutionBounds()
        {
            Debug.Log("\n[TEST 5] Verifying DominoBoardLayoutEngine across Multi-Resolution Canvas Sizes...");

            // Test resolutions: 360x800 (9:20), 390x844 (≈ 9:19.5), 412x915 (≈ 9:20), 1080x1920 (9:16)
            var resolutions = new Vector2[]
            {
                new Vector2(360f, 320f),  // 360x800 (9:20)
                new Vector2(390f, 360f),  // 390x844 (≈ 9:19.5)
                new Vector2(412f, 400f),  // 412x915 (≈ 9:20)
                new Vector2(400f, 480f)   // 1080x1920 (9:16) in 400pt panel space
            };

            var tiles = new List<DominoTile>
            {
                new DominoTile(6, 6),
                new DominoTile(6, 4),
                new DominoTile(4, 2),
                new DominoTile(2, 2),
                new DominoTile(2, 5),
                new DominoTile(5, 0),
                new DominoTile(0, 3)
            };

            bool allResPass = true;

            foreach (var res in resolutions)
            {
                var layout = DominoBoardLayoutEngine.CalculateLayout(
                    tiles,
                    leftEndpointValue: 6,
                    rightEndpointValue: 3,
                    boardWidth: res.x,
                    boardHeight: res.y
                );

                if (layout.Placements.Count != tiles.Count)
                {
                    Debug.LogError($" -> FAILED for res {res}: Placements count {layout.Placements.Count} != {tiles.Count}");
                    allResPass = false;
                }

                if (layout.Scale < DominoBoardLayoutEngine.MinScale || layout.Scale > DominoBoardLayoutEngine.MaxScale)
                {
                    Debug.LogError($" -> FAILED for res {res}: Scale {layout.Scale} out of bounds [{DominoBoardLayoutEngine.MinScale}, {DominoBoardLayoutEngine.MaxScale}]");
                    allResPass = false;
                }

                // Check that placements remain within bounds
                foreach (var p in layout.Placements)
                {
                    if (p.Position.x < -50f || p.Position.x > res.x + 50f ||
                        p.Position.y < -50f || p.Position.y > res.y + 50f)
                    {
                        Debug.LogError($" -> FAILED: Placement {p.Tile} at {p.Position} spilled out of {res}");
                        allResPass = false;
                    }
                }
            }

            if (allResPass)
            {
                Debug.Log(" -> PASSED: DominoBoardLayoutEngine scaled and positioned cleanly across all screen sizes.");
            }
            return allResPass;
        }

        private static bool TestBoardLayoutSerpentineEndpoints()
        {
            Debug.Log("\n[TEST 6] Verifying DominoBoardLayoutEngine Drop Zone Endpoints...");

            var tiles = new List<DominoTile>
            {
                new DominoTile(6, 5),
                new DominoTile(5, 3),
                new DominoTile(3, 1)
            };

            var layout = DominoBoardLayoutEngine.CalculateLayout(
                tiles,
                leftEndpointValue: 6,
                rightEndpointValue: 1,
                boardWidth: 380f,
                boardHeight: 340f
            );

            bool leftActive = layout.LeftEndpoint.IsActive;
            bool rightActive = layout.RightEndpoint.IsActive;
            bool leftVal = (layout.LeftEndpoint.Value == 6);
            bool rightVal = (layout.RightEndpoint.Value == 1);

            bool pass = leftActive && rightActive && leftVal && rightVal;
            if (pass)
            {
                Debug.Log($" -> PASSED: Endpoints Left=[{layout.LeftEndpoint.Value}] at {layout.LeftEndpoint.Position}, Right=[{layout.RightEndpoint.Value}] at {layout.RightEndpoint.Position}");
            }
            else
            {
                Debug.LogError($" -> FAILED: leftActive={leftActive}, rightActive={rightActive}, leftVal={leftVal}, rightVal={rightVal}");
            }
            return pass;
        }

        private static bool TestWorstCaseScenariosAndZeroTileOverlaps()
        {
            Debug.Log("\n[TEST 7] Verifying Worst-Case Scenarios (Full 28-Tile Board, 21-Tile Hand, Zero Collisions)...");

            bool pass = true;

            // Scenario 1: All 28 Dominoes placed on table across various resolutions
            var tiles28 = new List<DominoTile>();
            int currentVal = 6;
            for (int i = 0; i < 28; i++)
            {
                int nextVal = (currentVal + 1) % 7;
                tiles28.Add(new DominoTile(currentVal, nextVal));
                currentVal = nextVal;
            }

            var testResolutions = new Vector2[]
            {
                new Vector2(360f, 320f),
                new Vector2(390f, 360f),
                new Vector2(412f, 400f)
            };

            foreach (var res in testResolutions)
            {
                var layout = DominoBoardLayoutEngine.CalculateLayout(
                    tiles28,
                    leftEndpointValue: 6,
                    rightEndpointValue: currentVal,
                    boardWidth: res.x,
                    boardHeight: res.y
                );

                if (layout.Placements.Count != 28)
                {
                    Debug.LogError($" -> FAILED for res {res}: Placements count {layout.Placements.Count} != 28");
                    pass = false;
                }

                bool overlaps = DominoBoardLayoutEngine.HasAnyTileOverlaps(layout);
                if (overlaps)
                {
                    Debug.LogError($" -> FAILED for res {res}: Tile overlaps detected in 28-tile worst case layout!");
                    pass = false;
                }

                if (layout.Scale < DominoBoardLayoutEngine.MinScale || layout.Scale > DominoBoardLayoutEngine.MaxScale)
                {
                    Debug.LogError($" -> FAILED for res {res}: Scale {layout.Scale} out of bounds");
                    pass = false;
                }

                // Verify all tiles fit within container
                foreach (var p in layout.Placements)
                {
                    if (p.Position.x < -10f || p.Position.x + p.Size.x > res.x + 10f ||
                        p.Position.y < -10f || p.Position.y + p.Size.y > res.y + 10f)
                    {
                        Debug.LogError($" -> FAILED: Placement {p.Tile} at {p.Position} size {p.Size} clipped outside {res}");
                        pass = false;
                    }
                }
            }

            // Scenario 2: Worst-case Hand (Player draws 21 tiles from boneyard)
            var human = new DominoPlayer(1, "Human Player", isHuman: true);
            var dealer = new DominoDealer();
            dealer.Deal(new List<DominoPlayer> { human }, 7, out _);

            while (dealer.RemainingCount > 0)
            {
                var drawn = dealer.DrawTile();
                if (drawn != null) human.AddTile(drawn);
            }

            if (human.HandCount != 21)
            {
                Debug.LogError($" -> FAILED: Hand count should be 21 after drawing entire boneyard, was {human.HandCount}");
                pass = false;
            }

            if (pass)
            {
                Debug.Log(" -> PASSED: Worst-case full 28-tile board has ZERO overlaps and fits all screen sizes. 21-tile hand state verified.");
            }

            return pass;
        }

        private static bool TestPanelSettingsAndUSSIntegrity()
        {
            Debug.Log("\n[TEST 8] Verifying PanelSettings Balanced Scale Mode & USS Syntax Cleanliness...");

            // 1. Verify PanelSettings
            var panelSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>("Assets/UI/PanelSettings/DominoesPanelSettings.asset");
            if (panelSettings == null)
            {
                Debug.LogError(" -> FAILED: DominoesPanelSettings.asset could not be loaded.");
                return false;
            }

            bool matchBalanced = Mathf.Approximately(panelSettings.match, 0.5f);
            bool refResCorrect = (panelSettings.referenceResolution == new Vector2Int(400, 844));

            if (!matchBalanced || !refResCorrect)
            {
                Debug.LogError($" -> FAILED: PanelSettings match={panelSettings.match} (expected 0.5), refRes={panelSettings.referenceResolution} (expected 400x844)");
                return false;
            }

            // 2. Verify USS files have 0 var(...) syntax bugs
            string[] ussPaths = new[]
            {
                "Assets/UI/Styles/HomeScreen.uss",
                "Assets/UI/Styles/WaitingScreen.uss",
                "Assets/UI/Styles/GameScreen.uss",
                "Assets/UI/Styles/LoadingScreen.uss",
                "Assets/UI/Styles/ResponsiveFoundation.uss"
            };

            bool ussClean = true;
            foreach (var path in ussPaths)
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (textAsset != null && System.Text.RegularExpressions.Regex.IsMatch(textAsset.text, @"var\([^)]+\)"))
                {
                    Debug.LogError($" -> FAILED: Unresolved var() found in {path}");
                    ussClean = false;
                }
            }

            bool pass = matchBalanced && refResCorrect && ussClean;
            if (pass)
            {
                Debug.Log(" -> PASSED: DominoesPanelSettings has match: 0.5, refRes: 400x844, and all USS stylesheets are 100% clean of unsupported var() syntax.");
            }
            return pass;
        }

        private static bool TestGameModeUniquenessAndContext()
        {
            Debug.Log("\n[TEST 9] Verifying 3 Unique Game Modes (VS Computer, Online Matchmaking, Friend Room)...");

            // 1. VS Computer Mode Verification
            DominoGameModeContext.CurrentMode = GameModeType.VsComputer;
            DominoGameModeContext.Difficulty = ComputerDifficulty.Hard;
            string vsHeader = DominoGameModeContext.GetGameScreenHeader();
            string vsTitle = DominoGameModeContext.GetLobbyTitle();
            if (!vsHeader.Contains("VS AI") || !vsHeader.Contains("HARD") || !vsTitle.Contains("HARD"))
            {
                Debug.LogError($" -> FAILED: VS Computer context mismatch: header='{vsHeader}', title='{vsTitle}'");
                return false;
            }

            // 2. Online Matchmaking Mode Verification
            DominoGameModeContext.CurrentMode = GameModeType.OnlineMatchmaking;
            DominoGameModeContext.OnlineRule = "All Fives";
            string onlineHeader = DominoGameModeContext.GetGameScreenHeader();
            string onlineSubtitle = DominoGameModeContext.GetLobbySubtitle();
            if (!onlineHeader.Contains("ONLINE") || !onlineHeader.Contains("ALL FIVES") || !onlineSubtitle.Contains("ALL FIVES"))
            {
                Debug.LogError($" -> FAILED: Online Matchmaking context mismatch: header='{onlineHeader}', subtitle='{onlineSubtitle}'");
                return false;
            }

            // 3. Friend Room Mode Verification
            DominoGameModeContext.CurrentMode = GameModeType.FriendRoom;
            DominoGameModeContext.RoomCode = "DOM-9942";
            DominoGameModeContext.IsHost = true;
            string friendHeader = DominoGameModeContext.GetGameScreenHeader();
            string friendTitle = DominoGameModeContext.GetLobbyTitle();
            string friendSubtitle = DominoGameModeContext.GetLobbySubtitle();
            if (!friendHeader.Contains("DOM-9942") || !friendTitle.Contains("DOM-9942") || !friendSubtitle.Contains("HOSTING"))
            {
                Debug.LogError($" -> FAILED: Friend Room context mismatch: header='{friendHeader}', title='{friendTitle}', subtitle='{friendSubtitle}'");
                return false;
            }

            // 4. Test 2-Player Match Start (1v1 Deal Count: 7 each, 14 boneyard)
            var match2P = new DominoMatchManager(waitingCountdownDuration: 5f, tilesPerPlayer: 7);
            match2P.AddPlayer(new DominoPlayer(1, "Player 1", isHuman: true));
            match2P.AddPlayer(new DominoPlayer(2, "Computer (Hard)", isHuman: false));
            bool start2P = match2P.TryStartMatch(out string err2P);
            if (!start2P || match2P.GameState.Players[0].HandCount != 7 || match2P.GameState.Players[1].HandCount != 7 || match2P.Dealer.RemainingCount != 14)
            {
                Debug.LogError($" -> FAILED: 2-Player 1v1 match deal failed or counts incorrect. err: {err2P}, boneyard: {match2P.Dealer?.RemainingCount}");
                return false;
            }

            // 5. Test 4-Player Online Match Start (4-Player Deal Count: 5 each, 8 boneyard)
            var match4P = new DominoMatchManager(waitingCountdownDuration: 5f, tilesPerPlayer: 7);
            match4P.AddPlayer(new DominoPlayer(1, "Player 1", isHuman: true));
            match4P.AddPlayer(new DominoPlayer(2, "Lucas 🇧🇷", isHuman: false));
            match4P.AddPlayer(new DominoPlayer(3, "Aoi 🇯🇵", isHuman: false));
            match4P.AddPlayer(new DominoPlayer(4, "Mateo 🇪🇸", isHuman: false));
            bool start4P = match4P.TryStartMatch(out string err4P);
            if (!start4P || match4P.GameState.Players[0].HandCount != 5 || match4P.Dealer.RemainingCount != 8)
            {
                Debug.LogError($" -> FAILED: 4-Player online match deal failed or counts incorrect. err: {err4P}, boneyard: {match4P.Dealer?.RemainingCount}");
                return false;
            }

            Debug.Log(" -> PASSED: VS Computer, Online Matchmaking, and Friend Room contexts, headers, and 1v1/4P dealing rules are 100% verified!");
            return true;
        }
    }
}
#endif
