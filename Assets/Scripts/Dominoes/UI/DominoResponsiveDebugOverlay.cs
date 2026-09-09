using UnityEngine;
using UnityEngine.UIElements;

namespace Dominoes
{
    /// <summary>
    /// Temporary development diagnostic overlay for real-time validation of screen resolutions,
    /// safe-area insets, panel dimensions, DPI, orientation, touch coordinates, and VisualElement picking.
    /// Can be toggled in inspector or via GlobalShowDebug.
    /// </summary>
    public class DominoResponsiveDebugOverlay : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Toggle this to show/hide the responsive metrics overlay on screen.")]
        [SerializeField] private bool showDebugOverlay = false;

        public static bool GlobalShowDebug = false;

        private Vector2 lastScreenPos = Vector2.zero;
        private Vector2 lastPanelPos = Vector2.zero;
        private string pickedElementName = "None";
        private Rect pickedElementBounds = Rect.zero;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private UIDocument activeUIDocument;

        private void Start()
        {
            activeUIDocument = GetComponent<UIDocument>();
            if (activeUIDocument == null)
            {
#if UNITY_2023_1_OR_NEWER
                activeUIDocument = FindAnyObjectByType<UIDocument>(FindObjectsInactive.Include);
#else
                activeUIDocument = FindObjectOfType<UIDocument>();
#endif
            }
        }

        private void Update()
        {
            bool hasPointer = false;
            if (Input.touchCount > 0)
            {
                lastScreenPos = Input.GetTouch(0).position;
                hasPointer = true;
            }
            else if (Input.GetMouseButton(0))
            {
                lastScreenPos = Input.mousePosition;
                hasPointer = true;
            }

            if (hasPointer && activeUIDocument != null && activeUIDocument.rootVisualElement != null)
            {
                var panel = activeUIDocument.rootVisualElement.panel;
                if (panel != null)
                {
                    lastPanelPos = RuntimePanelUtils.ScreenToPanel(panel, lastScreenPos);
                    var picked = panel.Pick(lastPanelPos);
                    if (picked != null)
                    {
                        pickedElementName = string.IsNullOrEmpty(picked.name) ? picked.GetType().Name : picked.name;
                        pickedElementBounds = picked.worldBound;
                    }
                    else
                    {
                        pickedElementName = "None (Background)";
                        pickedElementBounds = Rect.zero;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugOverlay && !GlobalShowDebug) return;

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 15,
                    alignment = TextAnchor.UpperLeft
                };
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    normal = { textColor = Color.yellow },
                    fontStyle = FontStyle.Bold
                };
            }

            Rect safe = Screen.safeArea;
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            float panelW = activeUIDocument != null && activeUIDocument.rootVisualElement != null ? activeUIDocument.rootVisualElement.resolvedStyle.width : 400f;
            float panelH = activeUIDocument != null && activeUIDocument.rootVisualElement != null ? activeUIDocument.rootVisualElement.resolvedStyle.height : 844f;

            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 240), boxStyle);
            GUILayout.Label($"<b>[DOMINOES RESPONSIVE RUNTIME DIAGNOSTICS]</b>", labelStyle);
            GUILayout.Label($"Screen: {Screen.width} x {Screen.height} (DPI: {dpi:F0}, Aspect: {GetAspectRatioLabel(Screen.width, Screen.height)})", labelStyle);
            GUILayout.Label($"Safe Area: x={safe.x:F0}, y={safe.y:F0}, w={safe.width:F0}, h={safe.height:F0}", labelStyle);
            GUILayout.Label($"Cutout Insets: Top={Screen.height - safe.yMax:F0}px, Bottom={safe.yMin:F0}px, Left={safe.xMin:F0}px, Right={Screen.width - safe.xMax:F0}px", labelStyle);
            GUILayout.Label($"Panel (UI Points): {panelW:F0} x {panelH:F0} | Scale: {Screen.width / Mathf.Max(1f, panelW):F2}x", labelStyle);
            GUILayout.Label($"Pointer Screen: ({lastScreenPos.x:F0}, {lastScreenPos.y:F0}) -> Panel: ({lastPanelPos.x:F0}, {lastPanelPos.y:F0})", labelStyle);
            GUILayout.Label($"Picked Element: '{pickedElementName}' | Bounds: [{pickedElementBounds.x:F0},{pickedElementBounds.y:F0} {pickedElementBounds.width:F0}x{pickedElementBounds.height:F0}]", labelStyle);
            GUILayout.EndArea();
        }

        private static string GetAspectRatioLabel(int width, int height)
        {
            if (width <= 0 || height <= 0) return "Unknown";
            float ratio = (float)width / height;
            if (Mathf.Approximately(ratio, 360f / 800f) || (width == 360 && height == 800)) return "9:20";
            if (Mathf.Abs(ratio - (390f / 844f)) < 0.005f || (width == 390 && height == 844)) return "≈ 9:19.5";
            if (Mathf.Abs(ratio - (412f / 915f)) < 0.005f || (width == 412 && height == 915)) return "≈ 9:20";
            if (Mathf.Approximately(ratio, 9f / 16f) || (width == 1080 && height == 1920)) return "9:16";
            return $"9:{(height * 9f / width):F1}";
        }
    }
}
