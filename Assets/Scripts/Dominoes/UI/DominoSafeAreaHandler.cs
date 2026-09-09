using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Core Safe-Area foundation for calculating and applying pixel-perfect safe-area insets
    /// to UI Toolkit elements across varying mobile devices (notches, punch-holes, gesture bars, status bars, rounded corners) in Unity 6.
    /// </summary>
    public static class DominoSafeAreaHandler
    {
        public const float DefaultBaseLeft = 10f;
        public const float DefaultBaseRight = 10f;
        public const float DefaultBaseTop = 6f;
        public const float DefaultBaseBottom = 6f;

        /// <summary>
        /// Registers an automatic safe-area handler on the target root that dynamically updates insets
        /// whenever layout geometry or screen orientation changes.
        /// </summary>
        /// <param name="rootElement">The screen root element listening for geometry changes.</param>
        /// <param name="safeTarget">The inner container (safe-content) receiving safe-area padding.</param>
        /// <param name="baseLeft">Base left padding in points.</param>
        /// <param name="baseRight">Base right padding in points.</param>
        /// <param name="baseTop">Base top padding in points.</param>
        /// <param name="baseBottom">Base bottom padding in points.</param>
        public static EventCallback<GeometryChangedEvent> RegisterAutoSafeArea(
            VisualElement rootElement,
            VisualElement safeTarget,
            float baseLeft = DefaultBaseLeft,
            float baseRight = DefaultBaseRight,
            float baseTop = DefaultBaseTop,
            float baseBottom = DefaultBaseBottom)
        {
            if (safeTarget == null) return null;

            ApplySafeArea(safeTarget, baseLeft, baseRight, baseTop, baseBottom);

            if (rootElement != null)
            {
                EventCallback<GeometryChangedEvent> callback = evt =>
                {
                    ApplySafeArea(safeTarget, baseLeft, baseRight, baseTop, baseBottom);
                };
                rootElement.RegisterCallback(callback);
                return callback;
            }

            return null;
        }

        /// <summary>
        /// Applies safe-area insets in panel coordinates to the given target VisualElement,
        /// combining physical hardware insets with authored base design padding.
        /// </summary>
        public static void ApplySafeArea(
            VisualElement target,
            float baseLeft = DefaultBaseLeft,
            float baseRight = DefaultBaseRight,
            float baseTop = DefaultBaseTop,
            float baseBottom = DefaultBaseBottom)
        {
            if (target == null) return;

            IPanel panel = target.panel;
            if (panel == null) return;

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width;
            float screenH = Screen.height;

            if (screenW <= 0 || screenH <= 0) return;

            // In Unity screen space, (0,0) is bottom-left and (screenW, screenH) is top-right.
            // In UI Toolkit panel space, (0,0) is top-left.
            Vector2 screenTopLeft = new Vector2(0f, screenH);
            Vector2 screenBottomRight = new Vector2(screenW, 0f);

            Vector2 safeTopLeft = new Vector2(safeArea.xMin, safeArea.yMax);
            Vector2 safeBottomRight = new Vector2(safeArea.xMax, safeArea.yMin);

            Vector2 panelTopLeft = RuntimePanelUtils.ScreenToPanel(panel, screenTopLeft);
            Vector2 panelBottomRight = RuntimePanelUtils.ScreenToPanel(panel, screenBottomRight);

            Vector2 safePanelTopLeft = RuntimePanelUtils.ScreenToPanel(panel, safeTopLeft);
            Vector2 safePanelBottomRight = RuntimePanelUtils.ScreenToPanel(panel, safeBottomRight);

            float leftInset = Mathf.Max(0f, safePanelTopLeft.x - panelTopLeft.x);
            float topInset = Mathf.Max(0f, safePanelTopLeft.y - panelTopLeft.y);
            float rightInset = Mathf.Max(0f, panelBottomRight.x - safePanelBottomRight.x);
            float bottomInset = Mathf.Max(0f, panelBottomRight.y - safePanelBottomRight.y);

            target.style.paddingLeft = leftInset + baseLeft;
            target.style.paddingRight = rightInset + baseRight;
            target.style.paddingTop = topInset + baseTop;
            target.style.paddingBottom = bottomInset + baseBottom;
        }

        /// <summary>
        /// Returns current safe area insets in UI Toolkit panel coordinates.
        /// </summary>
        public static RectOffset GetSafeAreaInsets(IPanel panel)
        {
            if (panel == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return new RectOffset(0, 0, 0, 0);
            }

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width;
            float screenH = Screen.height;

            Vector2 screenTopLeft = new Vector2(0f, screenH);
            Vector2 screenBottomRight = new Vector2(screenW, 0f);

            Vector2 safeTopLeft = new Vector2(safeArea.xMin, safeArea.yMax);
            Vector2 safeBottomRight = new Vector2(safeArea.xMax, safeArea.yMin);

            Vector2 panelTopLeft = RuntimePanelUtils.ScreenToPanel(panel, screenTopLeft);
            Vector2 panelBottomRight = RuntimePanelUtils.ScreenToPanel(panel, screenBottomRight);

            Vector2 safePanelTopLeft = RuntimePanelUtils.ScreenToPanel(panel, safeTopLeft);
            Vector2 safePanelBottomRight = RuntimePanelUtils.ScreenToPanel(panel, safeBottomRight);

            int left = Mathf.RoundToInt(Mathf.Max(0f, safePanelTopLeft.x - panelTopLeft.x));
            int top = Mathf.RoundToInt(Mathf.Max(0f, safePanelTopLeft.y - panelTopLeft.y));
            int right = Mathf.RoundToInt(Mathf.Max(0f, panelBottomRight.x - safePanelBottomRight.x));
            int bottom = Mathf.RoundToInt(Mathf.Max(0f, panelBottomRight.y - safePanelBottomRight.y));

            return new RectOffset(left, right, top, bottom);
        }
    }
}
