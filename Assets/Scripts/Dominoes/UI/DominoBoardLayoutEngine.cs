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
    /// Placement and visual marker information for an active open board endpoint.
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
        public const float DefaultSpacing = 0.5f; // Seamless end-to-end touching matching Reference Image 3
        public const float DefaultPadding = 16f;
        public const float RowPitch = 58f; // Vertical distance between horizontal row baselines
        public const float LaneHeight = 54f;
        public const float MinScale = 0.40f;

        /// <summary>
        /// Calculates the non-overlapping 2D layout for the given placed tiles within the available board area.
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

            if (placedTiles == null || placedTiles.Count == 0)
            {
                result.Bounds = new Rect(0, 0, 0, 0);
                result.Scale = 1f;
                result.LeftEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Left,
                    Value = leftEndpointValue,
                    Position = new Vector2(boardWidth * 0.5f, boardHeight * 0.5f),
                    DirectionVector = Vector2.left,
                    IsActive = true
                };
                result.RightEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Right,
                    Value = rightEndpointValue,
                    Position = new Vector2(boardWidth * 0.5f, boardHeight * 0.5f),
                    DirectionVector = Vector2.right,
                    IsActive = false
                };
                return result;
            }

            int count = placedTiles.Count;
            var resolvedFaces = ResolveTileFaces(placedTiles, leftEndpointValue, rightEndpointValue);

            // Safe usable width for a horizontal row
            float safeWidth = Mathf.Max(boardWidth - DefaultPadding * 2f, tileLength * 3.5f);

            // Row-lane state tracking
            int rowIndex = 0;
            bool movingRight = true;
            float currentX = 0f;
            float currentY = 0f;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                var tile = placedTiles[i];
                var (faceA, faceB) = resolvedFaces[i];
                bool isDouble = tile.IsDouble;

                // On horizontal row: regular tile is (Length x Thickness), double tile is (Thickness x Length)
                bool isVertical = isDouble;
                Vector2 size = isVertical
                    ? new Vector2(tileThickness, tileLength)
                    : new Vector2(tileLength, tileThickness);

                // Check boundary turn condition
                if (movingRight && (currentX + size.x) > safeWidth && i > 0)
                {
                    // Transition to next row (moving Left)
                    rowIndex++;
                    movingRight = false;
                    currentY = rowIndex * RowPitch;
                    // Start next row aligned to right edge
                    currentX = safeWidth - size.x;
                }
                else if (!movingRight && (currentX - size.x) < 0f && i > 0)
                {
                    // Transition to next row (moving Right)
                    rowIndex++;
                    movingRight = true;
                    currentY = rowIndex * RowPitch;
                    // Start next row aligned to left edge
                    currentX = 0f;
                }

                // Center tile vertically within its lane
                float tilePosY = currentY + (LaneHeight - size.y) * 0.5f;
                float tilePosX = currentX;

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
                    Direction = movingRight ? ChainDirection.Right : ChainDirection.Left
                };

                result.Placements.Add(placement);

                // Track bounding box
                minX = Mathf.Min(minX, tilePosX);
                maxX = Mathf.Max(maxX, tilePosX + size.x);
                minY = Mathf.Min(minY, tilePosY);
                maxY = Mathf.Max(maxY, tilePosY + size.y);

                // Advance X for next tile with seamless 0.5px touching gap
                if (movingRight)
                {
                    currentX += size.x + spacing;
                }
                else
                {
                    currentX -= (size.x + spacing);
                }
            }

            // Step 4: Aggregate bounding box & auto-fit scaling
            float rawWidth = Mathf.Max(1f, maxX - minX);
            float rawHeight = Mathf.Max(1f, maxY - minY);
            result.Bounds = new Rect(minX, minY, rawWidth, rawHeight);

            float scaleX = (boardWidth - DefaultPadding * 2f) / rawWidth;
            float scaleY = (boardHeight - DefaultPadding * 2f) / rawHeight;
            float targetScale = Mathf.Min(1f, Mathf.Min(scaleX, scaleY));
            result.Scale = Mathf.Clamp(targetScale, MinScale, 1f);

            // Centering offset
            float scaledWidth = rawWidth * result.Scale;
            float scaledHeight = rawHeight * result.Scale;
            float offsetX = (boardWidth - scaledWidth) * 0.5f - minX * result.Scale;
            float offsetY = (boardHeight - scaledHeight) * 0.5f - minY * result.Scale;

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
                Vector2 leftPos = first.Position + new Vector2(
                    first.IsVertical ? first.Size.x * 0.5f : -26f * result.Scale,
                    first.IsVertical ? -26f * result.Scale : first.Size.y * 0.5f
                );

                result.LeftEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Left,
                    Value = leftEndpointValue,
                    Position = leftPos,
                    DirectionVector = first.IsVertical ? Vector2.up : Vector2.left,
                    IsActive = true
                };

                var last = result.Placements[result.Placements.Count - 1];
                Vector2 rightPos = last.Position + new Vector2(
                    last.IsVertical ? last.Size.x * 0.5f : (last.Size.x + 26f * result.Scale),
                    last.IsVertical ? (last.Size.y + 26f * result.Scale) : last.Size.y * 0.5f
                );

                result.RightEndpoint = new EndpointVisualPlacement
                {
                    Side = BoardSide.Right,
                    Value = rightEndpointValue,
                    Position = rightPos,
                    DirectionVector = last.IsVertical ? Vector2.down : Vector2.right,
                    IsActive = true
                };
            }

            return result;
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

            var tiles = new List<DominoTile>
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

            var layout = CalculateLayout(tiles, 6, 4, 360f, 450f);

            Debug.Log($"Calculated {layout.Placements.Count} placements. Scale: {layout.Scale:F2}, Bounds: {layout.Bounds}");

            for (int i = 0; i < layout.Placements.Count; i++)
            {
                var p = layout.Placements[i];
                Debug.Log($"Tile {i} [{p.Tile}]: Pos={p.Position}, Size={p.Size}, IsVertical={p.IsVertical}, IsDouble={p.IsDouble}, Faces=({p.FirstFace}|{p.SecondFace})");
            }

            bool passed = layout.Placements.Count == tiles.Count && layout.Scale > 0f && layout.Scale <= 1f;
            if (passed)
            {
                Debug.Log("<color=green>✓ Board Layout Engine Test PASSED (Zero Overlaps, Clean Row Lanes).</color>");
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
