using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Responsive UI Manager - Handles dynamic scaling, safe areas, and breakpoint detection
/// Integrates with Kenney assets and applies responsive classes to root VisualElements
/// </summary>
[ExecuteAlways]
public class ResponsiveUIManager : MonoBehaviour
{
    public static ResponsiveUIManager Instance { get; private set; }
    
    [Header("Scaling Configuration")]
    [SerializeField] private float baseFontSize = 16f;
    [SerializeField] private float minScale = 0.75f;
    [SerializeField] private float maxScale = 1.5f;
    
    [Header("Breakpoints (width in dp)")]
    [SerializeField] private int compactMaxWidth = 359;
    [SerializeField] private int standardMaxWidth = 389;
    [SerializeField] private int largeMaxWidth = 429;
    
    [Header("Safe Area")]
    [SerializeField] private bool useSafeArea = true;
    [SerializeField] private bool simulateNotch = false;
    
    [Header("Debug")]
    [SerializeField] private bool logScaleChanges = true;
    
    // Current state
    private VisualElement _rootElement;
    private float _currentScale = 1f;
    private ScreenSizeCategory _currentCategory = ScreenSizeCategory.Standard;
    private ScreenOrientation _lastOrientation;
    private Rect _lastSafeArea;
    
    // Breakpoint categories
    public enum ScreenSizeCategory
    {
        Compact,      // < 360dp (e.g., 320x568)
        Standard,     // 360-389dp (most Android)
        Large,        // 390-429dp (iPhone, large Android)
        XLarge        // 430+dp (tablets, foldables)
    }
    
    // Events
    public System.Action<float> OnScaleChanged;
    public System.Action<ScreenSizeCategory> OnCategoryChanged;
    public System.Action<Rect> OnSafeAreaChanged;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
            return;
        }
        Instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private void Start()
    {
        _lastOrientation = Screen.orientation;
        _lastSafeArea = Screen.safeArea;
        
        // Find UIDocument and hook up
        var uiDoc = FindFirstObjectByType<UIDocument>();
        if (uiDoc != null)
        {
            Initialize(uiDoc.rootVisualElement);
        }
        else
        {
            // Try again after a frame
            StartCoroutine(DelayedInit());
        }
    }
    
    private System.Collections.IEnumerator DelayedInit()
    {
        yield return null;
        var uiDoc = FindFirstObjectByType<UIDocument>();
        if (uiDoc != null)
        {
            Initialize(uiDoc.rootVisualElement);
        }
    }
    
    public void Initialize(VisualElement root)
    {
        _rootElement = root;
        ApplyResponsiveClasses();
        RegisterOrientationAndSafeAreaCallbacks();
    }
    
    private void RegisterOrientationAndSafeAreaCallbacks()
    {
        // Unity 2022+ has Screen.orientation change events
        // For older versions, we poll in Update
    }
    
    private void Update()
    {
        CheckForChanges();
    }
    
    private void CheckForChanges()
    {
        bool changed = false;
        
        // Check orientation
        if (Screen.orientation != _lastOrientation)
        {
            _lastOrientation = Screen.orientation;
            changed = true;
        }
        
        // Check safe area
        if (useSafeArea && Screen.safeArea != _lastSafeArea)
        {
            _lastSafeArea = Screen.safeArea;
            changed = true;
            OnSafeAreaChanged?.Invoke(_lastSafeArea);
        }
        
        if (changed || _rootElement == null)
        {
            ApplyResponsiveClasses();
        }
    }
    
    public void ApplyResponsiveClasses()
    {
        if (_rootElement == null) return;
        
        // Calculate scale factor based on screen width
        float screenWidthDp = GetScreenWidthDp();
        float screenHeightDp = GetScreenHeightDp();
        
        ScreenSizeCategory newCategory = CategorizeScreenWidth(screenWidthDp);
        bool isLandscape = screenWidthDp > screenHeightDp;
        
        // Calculate scale
        float targetScale = CalculateScale(screenWidthDp, newCategory, isLandscape);
        targetScale = Mathf.Clamp(targetScale, minScale, maxScale);
        
        // Apply if changed
        if (Mathf.Abs(targetScale - _currentScale) > 0.01f || newCategory != _currentCategory)
        {
            _currentScale = targetScale;
            _currentCategory = newCategory;
            
            UpdateRootClasses(isLandscape);
            UpdateCSSVariables();
            
            OnScaleChanged?.Invoke(_currentScale);
            OnCategoryChanged?.Invoke(_currentCategory);
            
            if (logScaleChanges)
            {
                Debug.Log($"[ResponsiveUI] Scale: {_currentScale:F2}, Category: {_currentCategory}, " +
                          $"Screen: {screenWidthDp}x{screenHeightDp}dp, SafeArea: {Screen.safeArea}, " +
                          $"Orientation: {Screen.orientation}");
            }
        }
    }
    
    private float GetScreenWidthDp()
    {
        // Convert pixels to density-independent pixels
        float dpi = Screen.dpi <= 0 ? 160f : Screen.dpi;
        return Screen.width / (dpi / 160f);
    }
    
    private float GetScreenHeightDp()
    {
        float dpi = Screen.dpi <= 0 ? 160f : Screen.dpi;
        return Screen.height / (dpi / 160f);
    }
    
    private ScreenSizeCategory CategorizeScreenWidth(float widthDp)
    {
        if (widthDp <= compactMaxWidth) return ScreenSizeCategory.Compact;
        if (widthDp <= standardMaxWidth) return ScreenSizeCategory.Standard;
        if (widthDp <= largeMaxWidth) return ScreenSizeCategory.Large;
        return ScreenSizeCategory.XLarge;
    }
    
    private float CalculateScale(float widthDp, ScreenSizeCategory category, bool isLandscape)
    {
        float scale = category switch
        {
            ScreenSizeCategory.Compact => 0.875f,   // 14px base
            ScreenSizeCategory.Standard => 1.0f,    // 16px base
            ScreenSizeCategory.Large => 1.125f,     // 18px base
            ScreenSizeCategory.XLarge => 1.25f,     // 20px base
            _ => 1.0f
        };
        
        // Reduce scale in landscape to fit more content
        if (isLandscape)
        {
            scale *= 0.85f;
        }
        
        return scale;
    }
    
    private void UpdateRootClasses(bool isLandscape)
    {
        // Remove all category classes
        _rootElement.RemoveFromClassList("root--compact");
        _rootElement.RemoveFromClassList("root--standard");
        _rootElement.RemoveFromClassList("root--large");
        _rootElement.RemoveFromClassList("root--xlarge");
        _rootElement.RemoveFromClassList("root--landscape");
        _rootElement.RemoveFromClassList("root--portrait");
        
        // Add current category
        _rootElement.AddToClassList($"root--{_currentCategory.ToString().ToLower()}");
        
        if (isLandscape)
            _rootElement.AddToClassList("root--landscape");
        else
            _rootElement.AddToClassList("root--portrait");
    }
    
    private void UpdateCSSVariables()
    {
        if (_rootElement == null) return;
        
        // Safe area insets applied directly to root padding
        if (useSafeArea && !simulateNotch)
        {
            if (_rootElement.panel != null)
            {
                Dominoes.DominoSafeAreaHandler.ApplySafeArea(_rootElement, 0f, 0f, 0f, 0f);
            }
            else
            {
                var safeArea = Screen.safeArea;
                float dpi = Screen.dpi <= 0 ? 160f : Screen.dpi;
                float dpScale = 160f / dpi;
                
                _rootElement.style.paddingTop = (Screen.height - safeArea.yMax) * dpScale;
                _rootElement.style.paddingBottom = safeArea.yMin * dpScale;
                _rootElement.style.paddingLeft = safeArea.xMin * dpScale;
                _rootElement.style.paddingRight = (Screen.width - safeArea.xMax) * dpScale;
            }
        }
        else if (simulateNotch)
        {
            // Simulate iPhone-style notch for testing
            _rootElement.style.paddingTop = 44f;
            _rootElement.style.paddingBottom = 34f;
            _rootElement.style.paddingLeft = 0f;
            _rootElement.style.paddingRight = 0f;
        }
        else
        {
            _rootElement.style.paddingTop = 0f;
            _rootElement.style.paddingBottom = 0f;
            _rootElement.style.paddingLeft = 0f;
            _rootElement.style.paddingRight = 0f;
        }
    }
    
    // Public API for other scripts
    public float CurrentScale => _currentScale;
    public ScreenSizeCategory CurrentCategory => _currentCategory;
    public Rect CurrentSafeArea => _lastSafeArea;
    public bool IsLandscape => _lastOrientation == ScreenOrientation.LandscapeLeft || 
                                _lastOrientation == ScreenOrientation.LandscapeRight;
    
    // Helper to get scaled size
    public float GetScaledSize(float baseSize)
    {
        return baseSize * _currentScale;
    }
    
    public Vector2 GetScaledSize(Vector2 baseSize)
    {
        return baseSize * _currentScale;
    }
    
    // Helper to get responsive font size
    public int GetResponsiveFontSize(int baseSize)
    {
        return Mathf.RoundToInt(baseSize * _currentScale);
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && _rootElement != null)
        {
            ApplyResponsiveClasses();
        }
    }
#endif
}