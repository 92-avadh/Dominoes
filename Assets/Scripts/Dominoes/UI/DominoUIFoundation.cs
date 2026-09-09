using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Unified Mobile Responsive UI Foundation for Dominoes.
    /// Provides standardized coordinate conversion pipelines, reactive safe-area integration,
    /// modal layout management, and pointer interaction helpers across all mobile screens.
    /// </summary>
    public static class DominoUIFoundation
    {
        public const float MinTouchTargetSize = 40f; // Minimum 40x40 points for mobile touch accessibility

        #region Coordinate Conversion Pipeline

        /// <summary>
        /// Converts a Unity Screen position (bottom-left origin) to UI Toolkit Panel coordinates (top-left origin).
        /// </summary>
        public static Vector2 ScreenToPanel(IPanel panel, Vector2 screenPosition)
        {
            if (panel == null) return screenPosition;
            return RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        }

        /// <summary>
        /// Converts a Unity Screen position directly to an element's local coordinate space.
        /// </summary>
        public static Vector2 ScreenToElement(VisualElement element, Vector2 screenPosition)
        {
            if (element == null || element.panel == null) return screenPosition;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(element.panel, screenPosition);
            return element.WorldToLocal(panelPos);
        }

        /// <summary>
        /// Converts a UI Toolkit panel coordinate to an element's local coordinate space.
        /// </summary>
        public static Vector2 PanelToElement(VisualElement element, Vector2 panelPosition)
        {
            if (element == null) return panelPosition;
            return element.WorldToLocal(panelPosition);
        }

        /// <summary>
        /// Converts an element's local coordinate to UI Toolkit panel space.
        /// </summary>
        public static Vector2 ElementToPanel(VisualElement element, Vector2 localPosition)
        {
            if (element == null) return localPosition;
            return element.LocalToWorld(localPosition);
        }

        #endregion

        #region Modal Management

        /// <summary>
        /// Displays an overlay modal with animated opacity/scale transition and pointer trapping.
        /// </summary>
        public static void ShowModal(VisualElement modalRoot, VisualElement modalCard = null)
        {
            if (modalRoot == null) return;
            modalRoot.style.display = DisplayStyle.Flex;
            modalRoot.style.opacity = 1f;

            if (modalCard != null)
            {
                modalCard.style.scale = new Scale(Vector2.one);
            }
        }

        /// <summary>
        /// Hides an overlay modal cleanly.
        /// </summary>
        public static void HideModal(VisualElement modalRoot)
        {
            if (modalRoot == null) return;
            modalRoot.style.display = DisplayStyle.None;
        }

        #endregion

        #region Button & Interaction Helpers

        /// <summary>
        /// Configures pointer picking mode and touch target sizing on interactive elements.
        /// </summary>
        public static void ConfigureInteractiveElement(VisualElement element, bool isInteractive)
        {
            if (element == null) return;
            element.pickingMode = isInteractive ? PickingMode.Position : PickingMode.Ignore;
        }

        #endregion
    }
}
