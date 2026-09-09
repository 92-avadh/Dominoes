using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Manages loading and applying authentic Kenney Domino tile textures to UI Toolkit VisualElements.
    /// Uses pre-packaged PNG assets from Resources/Textures/Kenney/Dominoes/Light and Dark.
    /// Accurately handles orientation (vertical, horizontal, flipped) for hand, board, and boneyard.
    /// </summary>
    public static class DominoTileTextureManager
    {
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static Texture2D emptyTileTexture;
        private static Texture2D darkEmptyTileTexture;
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            for (int a = 0; a <= 6; a++)
            {
                for (int b = a; b <= 6; b++)
                {
                    string path = $"Textures/Kenney/Dominoes/Light/tile_{a}_{b}";
                    var tex = Resources.Load<Texture2D>(path);
                    if (tex != null)
                    {
                        TextureCache[$"{a}_{b}"] = tex;
                    }
                }
            }

            emptyTileTexture = Resources.Load<Texture2D>("Textures/Kenney/Dominoes/Light/tile_empty");
            darkEmptyTileTexture = Resources.Load<Texture2D>("Textures/Kenney/Dominoes/Dark/tile_empty");

            isInitialized = true;
        }

        public static Texture2D GetTileTexture(int faceA, int faceB)
        {
            if (!isInitialized) Initialize();

            int min = Math.Min(faceA, faceB);
            int max = Math.Max(faceA, faceB);
            string key = $"{min}_{max}";

            if (TextureCache.TryGetValue(key, out var tex) && tex != null)
            {
                return tex;
            }

            var fallback = Resources.Load<Texture2D>($"Textures/Kenney/Dominoes/Light/tile_{min}_{max}");
            if (fallback != null)
            {
                TextureCache[key] = fallback;
                return fallback;
            }

            return emptyTileTexture;
        }

        public static Texture2D GetFaceDownTexture(bool dark = true)
        {
            if (!isInitialized) Initialize();
            return dark ? (darkEmptyTileTexture ?? emptyTileTexture) : emptyTileTexture;
        }

        /// <summary>
        /// Applies the domino image directly to an existing VisualElement.
        /// Handles rotation and positioning for vertical and horizontal orientations.
        /// </summary>
        public static void ApplyDominoTexture(
            VisualElement container,
            int firstFace,
            int secondFace,
            bool isVertical,
            float width,
            float height)
        {
            if (container == null) return;

            container.Clear();
            container.style.overflow = Overflow.Hidden;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;

            var texture = GetTileTexture(firstFace, secondFace);
            if (texture == null) return;

            var inner = new VisualElement();
            inner.name = "domino-texture-inner";
            inner.style.backgroundImage = new StyleBackground(texture);
            inner.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            inner.pickingMode = PickingMode.Ignore;

            if (isVertical)
            {
                inner.style.width = Length.Percent(100);
                inner.style.height = Length.Percent(100);
                inner.style.position = Position.Relative;
                inner.style.left = 0;
                inner.style.top = 0;

                if (firstFace > secondFace)
                {
                    inner.style.rotate = new Rotate(Angle.Degrees(180));
                }
                else
                {
                    inner.style.rotate = new Rotate(Angle.Degrees(0));
                }
            }
            else
            {
                // Horizontal tile: width is larger than height (e.g. 54 x 27)
                inner.style.position = Position.Absolute;
                inner.style.width = height;
                inner.style.height = width;
                inner.style.left = (width - height) / 2f;
                inner.style.top = (height - width) / 2f;

                float angle = (firstFace <= secondFace) ? -90f : 90f;
                inner.style.rotate = new Rotate(Angle.Degrees(angle));
            }

            container.Add(inner);
        }

        /// <summary>
        /// Applies the face-down tile texture (e.g. for boneyard tiles).
        /// </summary>
        public static void ApplyFaceDownTexture(VisualElement container, bool dark = true)
        {
            if (container == null) return;

            container.Clear();
            var texture = GetFaceDownTexture(dark);
            if (texture != null)
            {
                container.style.backgroundImage = new StyleBackground(texture);
                container.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }

            // Decorative brass center spinner pin for luxury authentic domino appearance
            var spinner = new VisualElement();
            spinner.name = "boneyard-tile-spinner";
            spinner.style.width = 6;
            spinner.style.height = 6;
            spinner.style.borderTopLeftRadius = 3;
            spinner.style.borderTopRightRadius = 3;
            spinner.style.borderBottomLeftRadius = 3;
            spinner.style.borderBottomRightRadius = 3;
            spinner.style.backgroundColor = new StyleColor(new Color(1.0f, 0.84f, 0.0f, 0.95f));
            spinner.style.borderTopWidth = 1;
            spinner.style.borderRightWidth = 1;
            spinner.style.borderBottomWidth = 1;
            spinner.style.borderLeftWidth = 1;
            spinner.style.borderTopColor = new StyleColor(new Color(0.55f, 0.25f, 0.05f, 1f));
            spinner.style.borderRightColor = new StyleColor(new Color(0.55f, 0.25f, 0.05f, 1f));
            spinner.style.borderBottomColor = new StyleColor(new Color(0.55f, 0.25f, 0.05f, 1f));
            spinner.style.borderLeftColor = new StyleColor(new Color(0.55f, 0.25f, 0.05f, 1f));
            spinner.style.position = Position.Absolute;
            spinner.pickingMode = PickingMode.Ignore;
            container.Add(spinner);
        }
    }
}
