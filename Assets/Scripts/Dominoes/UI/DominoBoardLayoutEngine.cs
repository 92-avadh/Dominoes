using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Direction that the domino chain is currently extending.
    /// </summary>
    public enum ChainDirection
    {
        Right,
        Down,
        Left,
        Up
    }

    /// <summary>
    /// Visual placement data for a single placed domino tile on the board canvas.
    /// </summary>
    [Serializable]
    public struct DominoVisualPlacement
    {
        public DominoTile Tile;
        public int ChainIndex;
        public Vector2 Position;       // Top-Left position in board container pixels
        public Vector2 Size;           // Width and Height in board pixels
        public bool IsVertical;        // True if tile is oriented vertically
        public bool IsDouble;          // True if double tile (rotated 90° to chain)
        public int FirstFace;          // Face on top/left
        public int SecondFace;         // Face on bottom/right
        public ChainDirection Direction; // Direction the chain was moving across this tile
    }

    /// <summary>
    
    /// </summary>
    [Serializable]
    public struct EndpointVisualPlacement
    {
        public BoardSide Side;
        public int Value;
        public Vector2 Position;       // Center position for drop zone / marker on board canvas
        public Vector2 DirectionVector;// Outward facing direction for arrows / glow
        public bool IsActive;
    }

    /// <summary>
    /// Layout result containing all visual tile placements, aggregate bounding box, scale factor, and endpoint markers.
    /// </summary>
    public class DominoBoardLayoutResult
    {
        public List<DominoVisualPlacement> Placements = new List<DominoVisualPlacement>();
        public Rect Bounds;
        public float Scale = 1f;
        public EndpointVisualPlacement LeftEndpoint;
        public EndpointVisualPlacement RightEndpoint;
    }

    /// <summary>
    /// Pure, deterministic 2D Board Layout Engine for Dominoes.
    /// Converts a logical list of placed dominoes into a beautiful, non-overlapping 2D serpentine chain.
    /// Rules:
    /// - Regular tiles extend along the active chain direction.
    /// - Double tiles are automatically rotated 90° perpendicular to the chain direction.
    /// - Strict row-lane layout prevents any vertical or horizontal tile overlap.
    /// - Auto-centers the composition and smoothly scales if the chain expands.
    /// - Calculates exact drop-zone coordinates for interactive open endpoint markers.
    /// </summary>
    public static class DominoBoardLayoutEngine
    {
        public const float DefaultTileLength = 54f;
        public const float DefaultTileThickness = 27f;
        public const float DefaultSpacing = 0.0f; // Flush 0-gap seamless touching matching real dominoes
        public const float DefaultPadding = 16f;
        public const float RowPitch = 81f; // 3 * DefaultTileThickness: guarantees >=27px gap even between adjacent double dominoes
        public const float LaneHeight = 54f;
        public const float MinScale = 0.20f; // Dynamic zoom out ensuring all 28 tiles fit cleanly on any screen
        public const float MaxScale = 1.35f; // Zoomed in when few tiles on table

        /// <summary>
        /// Calculates the non-overlapping 2D layout for the given placed tiles within the available board area.
        /// Uses a strict downward row-lane serpentine model that mathematically prevents any tile overlap.
        /// </summary>
        public static DominoBoardLayoutResult CalculateLayout(
            IReadOnlyList<DominoTile> placedTiles,
            int leftEndpointValue,
            int rightEndpointValue,
            float boardWidth,
            float boardHeight,
            float tileLength = DefaultTileLength,
            float tileThickness = DefaultTileThickness,
            float spacing = DefaultSpacing)
        {
            var result = new DominoBoardLayoutResult();

            if (boardWidth <= 0f) boardWidth = 360f;
            if (boardHeight <= 0f) boardHeight = 320f;

            const float TopReserved = 48f;
            const float BottomReserved = 44f;
            float usableHeight = Mathf.Max(100f, boardHeight - TopReserved - BottomReserved);
            float centerY = TopReserved + usableHeight * 0.5f;

            if (placedTiles == null || placedTiles.Count == 0)
            {
                result.Bounds = new Rect(0, 0, 0, 0);
                result.Scale = MaxScale;
                result.LeftEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Left,
                    Value = leftEndpointValue,
                    Position = new Vector2(boardWidth * 0.5f, centerY),
                    DirectionVector = Vector2.left,
                    IsActive = true
                };
                result.RightEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Right,
                    Value = rightEndpointValue,
                    Position = new Vector2(boardWidth * 0.5f, centerY),
                    DirectionVector = Vector2.right,
                    IsActive = false
                };
                return result;
            }

            int count = placedTiles.Count;
            var resolvedFaces = ResolveTileFaces(placedTiles, leftEndpointValue, rightEndpointValue);

            int dirX = 1;
            float currentX = 0f;
            float currentY = 0f;
            int tilesInRow = 0;
            int maxTilesPerRow = Mathf.Clamp(Mathf.FloorToInt(boardWidth / (tileLength + 12f)), 4, 6);
            int cornerStep = 0; // 0: horizontal row, 1: corner step 1 (down), 2: corner step 2 (down)
            float lastRowEndX = 0f;
            int lastRowDirX = 1;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                var tile = placedTiles[i];
                var (faceA, faceB) = resolvedFaces[i];
                bool isDouble = tile.IsDouble;

                // Check if current row should turn into a downward corner
                if (cornerStep == 0 && i > 0 && tilesInRow >= maxTilesPerRow)
                {
                    cornerStep = 1;
                    lastRowEndX = currentX;
                    lastRowDirX = dirX;
                }

                bool isVertical;
                Vector2 size;
                float tilePosX;
                float tilePosY;
                ChainDirection chainDir;

                if (cornerStep == 1)
                {
                    // First step of downward corner (vertical down)
                    chainDir = ChainDirection.Down;
                    isVertical = true;
                    size = new Vector2(tileThickness, tileLength);
                    tilePosX = (lastRowDirX > 0) ? lastRowEndX : (lastRowEndX - tileThickness);
                    tilePosY = currentY - tileThickness * 0.5f;

                    cornerStep = 2;
                }
                else if (cornerStep == 2)
                {
                    // Second step of downward corner (vertical down)
                    chainDir = ChainDirection.Down;
                    isVertical = true;
                    size = new Vector2(tileThickness, tileLength);
                    tilePosX = (lastRowDirX > 0) ? lastRowEndX : (lastRowEndX - tileThickness);
                    tilePosY = currentY + tileThickness * 1.5f; // currentY + 40.5f

                    // Complete corner: advance to next row baseline and flip horizontal direction
                    currentY += RowPitch; // 81f
                    dirX = -lastRowDirX;
                    currentX = lastRowEndX; // New row starts flush against the corner column
                    tilesInRow = 0;
                    cornerStep = 0;
                }
                else
                {
                    // Normal horizontal row placement
                    chainDir = (dirX > 0) ? ChainDirection.Right : ChainDirection.Left;
                    if (isDouble)
                    {
                        isVertical = true;
                        size = new Vector2(tileThickness, tileLength);
                        tilePosX = dirX > 0 ? currentX : (currentX - tileThickness);
                        tilePosY = currentY - tileLength * 0.5f;
                        currentX += dirX * tileThickness;
                    }
                    else
                    {
                        isVertical = false;
                        size = new Vector2(tileLength, tileThickness);
                        tilePosX = dirX > 0 ? currentX : (currentX - tileLength);
                        tilePosY = currentY - tileThickness * 0.5f;
                        currentX += dirX * tileLength;
                    }

                    tilesInRow++;
                }

                var placement = new DominoVisualPlacement
                {
                    Tile = tile,
                    ChainIndex = i,
                    Position = new Vector2(tilePosX, tilePosY),
                    Size = size,
                    IsVertical = isVertical,
                    IsDouble = isDouble,
                    FirstFace = faceA,
                    SecondFace = faceB,
                    Direction = chainDir
                };

                result.Placements.Add(placement);

                minX = Mathf.Min(minX, tilePosX);
                maxX = Mathf.Max(maxX, tilePosX + size.x);
                minY = Mathf.Min(minY, tilePosY);
                maxY = Mathf.Max(maxY, tilePosY + size.y);
            }

            // Step 4: Aggregate bounding box & auto-fit scaling (Dynamic Zoom Out as tiles increase)
            float rawWidth = Mathf.Max(1f, maxX - minX);
            float rawHeight = Mathf.Max(1f, maxY - minY);
            result.Bounds = new Rect(minX, minY, rawWidth, rawHeight);

            float usableWidth = Mathf.Max(100f, boardWidth - DefaultPadding * 2f);
            float scaleX = usableWidth / rawWidth;
            float scaleY = usableHeight / rawHeight;
            float targetScale = Mathf.Min(scaleX, scaleY);
            result.Scale = Mathf.Clamp(targetScale, MinScale, MaxScale);

            // Centering offset within usable felt area (safe from boneyard and turn guidance)
            float scaledWidth = rawWidth * result.Scale;
            float scaledHeight = rawHeight * result.Scale;
            float offsetX = (boardWidth - scaledWidth) * 0.5f - minX * result.Scale;
            float offsetY = TopReserved + (usableHeight - scaledHeight) * 0.5f - minY * result.Scale;

            for (int i = 0; i < result.Placements.Count; i++)
            {
                var p = result.Placements[i];
                p.Position = new Vector2(
                    p.Position.x * result.Scale + offsetX,
                    p.Position.y * result.Scale + offsetY
                );
                p.Size *= result.Scale;
                result.Placements[i] = p;
            }

            // Step 5: Compute endpoint drop zone positions
            if (result.Placements.Count > 0)
            {
                var first = result.Placements[0];
                Vector2 leftPos = first.Position + (first.Direction switch
                {
                    ChainDirection.Right => new Vector2(-26f * result.Scale, first.Size.y * 0.5f),
                    ChainDirection.Left => new Vector2(first.Size.x + 26f * result.Scale, first.Size.y * 0.5f),
                    ChainDirection.Down => new Vector2(first.Size.x * 0.5f, -26f * result.Scale),
                    _ => new Vector2(first.Size.x * 0.5f, first.Size.y + 26f * result.Scale)
                });

                Vector2 leftDir = first.Direction switch
                {
                    ChainDirection.Right => Vector2.left,
                    ChainDirection.Left => Vector2.right,
                    ChainDirection.Down => Vector2.up,
                    _ => Vector2.down
                };

                result.LeftEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Left,
                    Value = leftEndpointValue,
                    Position = leftPos,
                    DirectionVector = leftDir,
                    IsActive = true
                };

                var last = result.Placements[result.Placements.Count - 1];
                Vector2 rightPos = last.Position + (last.Direction switch
                {
                    ChainDirection.Right => new Vector2(last.Size.x + 26f * result.Scale, last.Size.y * 0.5f),
                    ChainDirection.Left => new Vector2(-26f * result.Scale, last.Size.y * 0.5f),
                    ChainDirection.Down => new Vector2(last.Size.x * 0.5f, last.Size.y + 26f * result.Scale),
                    _ => new Vector2(last.Size.x * 0.5f, -26f * result.Scale)
                });

                Vector2 rightDir = last.Direction switch
                {
                    ChainDirection.Right => Vector2.right,
                    ChainDirection.Left => Vector2.left,
                    ChainDirection.Down => Vector2.down,
                    _ => Vector2.up
                };

                result.RightEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Right,
                    Value = rightEndpointValue,
                    Position = rightPos,
                    DirectionVector = rightDir,
                    IsActive = true
                };
            }

            return result;
        }

        /// <summary>
        /// Validates that no two placed domino tiles overlap each other on the 2D board canvas.
        /// Returns true if an overlap area (> 0.5px) is detected.
        /// </summary>
        public static bool HasAnyTileOverlaps(DominoBoardLayoutResult layout)
        {
            if (layout == null || layout.Placements == null || layout.Placements.Count <= 1)
                return false;

            const float tolerance = 0.5f;
            int count = layout.Placements.Count;

            for (int i = 0; i < count; i++)
            {
                var a = layout.Placements[i];
                float ax1 = a.Position.x;
                float ay1 = a.Position.y;
                float ax2 = ax1 + a.Size.x;
                float ay2 = ay1 + a.Size.y;

                for (int j = i + 1; j < count; j++)
                {
                    var b = layout.Placements[j];
                    float bx1 = b.Position.x;
                    float by1 = b.Position.y;
                    float bx2 = bx1 + b.Size.x;
                    float by2 = by1 + b.Size.y;

                    float overlapX = Mathf.Min(ax2, bx2) - Mathf.Max(ax1, bx1);
                    float overlapY = Mathf.Min(ay2, by2) - Mathf.Max(ay1, by1);

                    if (overlapX > tolerance && overlapY > tolerance)
                    {
                        Debug.LogWarning($"[DominoBoardLayoutEngine] Overlap detected between Tile {i} [{a.Tile}] and Tile {j} [{b.Tile}]: overlap=({overlapX:F1}, {overlapY:F1})");
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<(int faceA, int faceB)> ResolveTileFaces(
            IReadOnlyList<DominoTile> placedTiles,
            int leftEndpoint,
            int rightEndpoint)
        {
            int count = placedTiles.Count;
            var faces = new List<(int faceA, int faceB)>(count);

            if (count == 1)
            {
                faces.Add((placedTiles[0].Left, placedTiles[0].Right));
                return faces;
            }

            int currentMatchingVal = leftEndpoint;
            for (int i = 0; i < count; i++)
            {
                var tile = placedTiles[i];
                if (tile.Left == currentMatchingVal)
                {
                    faces.Add((tile.Left, tile.Right));
                    currentMatchingVal = tile.Right;
                }
                else
                {
                    faces.Add((tile.Right, tile.Left));
                    currentMatchingVal = tile.Left;
                }
            }

            return faces;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Dominoes/Test Board Layout Engine")]
        public static void TestBoardLayoutEngineInEditor()
        {
            Debug.Log("================ STARTING BOARD LAYOUT ENGINE TEST ================");

            // Test case 1: 10 tiles with mixed doubles
            var tiles10 = new List<DominoTile>
            {
                new DominoTile(6, 6), // Double 6
                new DominoTile(6, 4),
                new DominoTile(4, 2),
                new DominoTile(2, 2), // Double 2
                new DominoTile(2, 5),
                new DominoTile(5, 1),
                new DominoTile(1, 3),
                new DominoTile(3, 3), // Double 3
                new DominoTile(3, 0),
                new DominoTile(0, 4)
            };

            var layout10 = CalculateLayout(tiles10, 6, 4, 360f, 450f);
            bool overlaps10 = HasAnyTileOverlaps(layout10);

            Debug.Log($"10-Tile Layout: Placements={layout10.Placements.Count}, Scale={layout10.Scale:F2}, Overlaps={overlaps10}");

            // Test case 2: Worst-case full 28 tiles
            var tiles28 = new List<DominoTile>();
            int currentVal = 6;
            for (int i = 0; i < 28; i++)
            {
                int nextVal = (currentVal + 1) % 7;
                tiles28.Add(new DominoTile(currentVal, nextVal));
                currentVal = nextVal;
            }

            var layout28 = CalculateLayout(tiles28, 6, currentVal, 360f, 450f);
            bool overlaps28 = HasAnyTileOverlaps(layout28);

            Debug.Log($"28-Tile Worst-Case Layout: Placements={layout28.Placements.Count}, Scale={layout28.Scale:F2}, Overlaps={overlaps28}");

            bool passed = !overlaps10 && !overlaps28 && layout10.Placements.Count == 10 && layout28.Placements.Count == 28;
            if (passed)
            {
                Debug.Log("<color=green>✓ Board Layout Engine Test PASSED: Zero overlaps across both 10 and 28 tiles!</color>");
            }
            else
            {
                Debug.LogError("<color=red>✗ Board Layout Engine Test FAILED.</color>");
            }

            Debug.Log("================ BOARD LAYOUT ENGINE TEST COMPLETED ================");
        }
#endif
    }
}
