using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Kenney Domino Tile Manager - Handles loading and rendering of domino tiles
/// from the Kenney Domino Pack (PNG tilesheets and individual PNGs)
/// Supports multiple themes: Stars, Light, Dark, Hearts, Gingerbread
/// </summary>
public class KenneyDominoTiles : MonoBehaviour
{
    public static KenneyDominoTiles Instance { get; private set; }
    
    [Header("Tile Themes")]
    [SerializeField] private TileTheme _defaultTheme = TileTheme.Stars;
    
    [Header("Tile Sheets (for 9-slice / atlas usage)")]
    [SerializeField] private Texture2D _starsSheet;
    [SerializeField] private Texture2D _lightSheet;
    [SerializeField] private Texture2D _darkSheet;
    [SerializeField] private Texture2D _heartsSheet;
    [SerializeField] private Texture2D _gingerbreadSheet;
    
    // Individual tile cache
    private Dictionary<TileTheme, Dictionary<DominoValue, Texture2D>> _tileCache = new();
    private Dictionary<TileTheme, Texture2D> _emptyTileCache = new();
    private Dictionary<TileTheme, Texture2D> _tileBackCache = new();
    
    // Tile dimensions (Kenney standard)
    public const int TILE_WIDTH = 88;
    public const int TILE_HEIGHT = 44;  // Half height for one side
    public const int FULL_TILE_HEIGHT = 88; // Full domino (2x)
    
    public enum TileTheme
    {
        Stars,          // Clean white with colored pips (best for UI)
        Light,          // Light wood texture
        Dark,           // Dark wood texture
        Hearts,         // Valentine/hearts theme
        Gingerbread     // Cookie theme
    }
    
    public struct DominoValue
    {
        public int Left;
        public int Right;
        
        public DominoValue(int left, int right)
        {
            Left = Mathf.Clamp(left, 0, 6);
            Right = Mathf.Clamp(right, 0, 6);
        }
        
        public override string ToString() => $"tile_{Left}_{Right}";
        
        public string ToStringNormalized() => $"tile_{Mathf.Min(Left, Right)}_{Mathf.Max(Left, Right)}";
        
        public bool IsDouble => Left == Right;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadAllTiles();
    }
    
    private void LoadAllTiles()
    {
        foreach (TileTheme theme in System.Enum.GetValues(typeof(TileTheme)))
        {
            _tileCache[theme] = new Dictionary<DominoValue, Texture2D>();
            LoadThemeTiles(theme);
        }
    }
    
    private void LoadThemeTiles(TileTheme theme)
    {
        string themeFolder = theme.ToString().ToLower();
        
        // Load empty tile
        string emptyPath = $"Textures/Kenney/Dominoes/{themeFolder}/tile_empty";
        _emptyTileCache[theme] = Resources.Load<Texture2D>(emptyPath);
        
        // Load all 28 domino combinations (0-6)
        for (int i = 0; i <= 6; i++)
        {
            for (int j = i; j <= 6; j++)
            {
                var value = new DominoValue(i, j);
                string tilePath = $"Textures/Kenney/Dominoes/{themeFolder}/{value.ToStringNormalized()}";
                var texture = Resources.Load<Texture2D>(tilePath);
                
                if (texture != null)
                {
                    _tileCache[theme][value] = texture;
                    // Also store reverse for easy lookup
                    if (i != j)
                    {
                        _tileCache[theme][new DominoValue(j, i)] = texture;
                    }
                }
            }
        }
        
        // Load tile back (use empty or first tile as back)
        _tileBackCache[theme] = _emptyTileCache[theme] ?? GetFirstTile(theme);
        
        Debug.Log($"[KenneyDomino] Loaded theme: {theme}, Tiles: {_tileCache[theme].Count}, Empty: {_emptyTileCache[theme] != null}");
    }
    
    private Texture2D GetFirstTile(TileTheme theme)
    {
        foreach (var kvp in _tileCache[theme])
            return kvp.Value;
        return null;
    }
    
    // Public API
    public Texture2D GetTile(TileTheme theme, int left, int right)
    {
        var value = new DominoValue(left, right);
        if (_tileCache.TryGetValue(theme, out var themeDict) && 
            themeDict.TryGetValue(value, out var texture))
        {
            return texture;
        }
        return _emptyTileCache.GetValueOrDefault(theme);
    }
    
    public Texture2D GetTile(TileTheme theme, DominoValue value)
    {
        return GetTile(theme, value.Left, value.Right);
    }
    
    public Texture2D GetEmptyTile(TileTheme theme)
    {
        return _emptyTileCache.GetValueOrDefault(theme);
    }
    
    public Texture2D GetTileBack(TileTheme theme)
    {
        return _tileBackCache.GetValueOrDefault(theme);
    }
    
    public Texture2D GetTileSheet(TileTheme theme)
    {
        return theme switch
        {
            TileTheme.Stars => _starsSheet,
            TileTheme.Light => _lightSheet,
            TileTheme.Dark => _darkSheet,
            TileTheme.Hearts => _heartsSheet,
            TileTheme.Gingerbread => _gingerbreadSheet,
            _ => _starsSheet
        };
    }
    
    /// <summary>
    /// Apply a Kenney domino tile to a VisualElement using USS background-image
    /// </summary>
    public void ApplyTileToElement(VisualElement element, TileTheme theme, int left, int right, bool horizontal = false)
    {
        var texture = GetTile(theme, left, right);
        if (texture != null)
        {
            string resourcePath = GetResourcePath(theme, left, right);
            element.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(resourcePath));
            element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            
            if (horizontal)
            {
                element.style.width = FULL_TILE_HEIGHT;
                element.style.height = TILE_WIDTH;
            }
            else
            {
                element.style.width = TILE_WIDTH;
                element.style.height = FULL_TILE_HEIGHT;
            }
        }
    }
    
    /// <summary>
    /// Apply tile back (face down) to element
    /// </summary>
    public void ApplyTileBackToElement(VisualElement element, TileTheme theme, bool horizontal = false)
    {
        var texture = GetTileBack(theme);
        if (texture != null)
        {
            string resourcePath = GetResourcePath(theme, 0, 0); // Use empty as back
            if (theme == TileTheme.Stars)
                resourcePath = "Textures/Kenney/Dominoes/stars/tile_empty";
            
            element.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(resourcePath));
            element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            
            if (horizontal)
            {
                element.style.width = FULL_TILE_HEIGHT;
                element.style.height = TILE_WIDTH;
            }
            else
            {
                element.style.width = TILE_WIDTH;
                element.style.height = FULL_TILE_HEIGHT;
            }
        }
    }
    
    private string GetResourcePath(TileTheme theme, int left, int right)
    {
        var value = new DominoValue(left, right);
        return $"Textures/Kenney/Dominoes/{theme.ToString().ToLower()}/{value.ToStringNormalized()}";
    }
    
    /// <summary>
    /// Create a VisualElement representing a domino tile (for programmatic UI)
    /// </summary>
    public VisualElement CreateTileElement(TileTheme theme, int left, int right, bool horizontal = false, string elementName = "domino-tile")
    {
        var tileElement = new VisualElement { name = elementName };
        tileElement.AddToClassList("domino-tile-kenney");
        
        if (horizontal)
            tileElement.AddToClassList("domino-tile-kenney-horizontal");
        
        ApplyTileToElement(tileElement, theme, left, right, horizontal);
        return tileElement;
    }
    
    /// <summary>
    /// Create a face-down tile element
    /// </summary>
    public VisualElement CreateTileBackElement(TileTheme theme, bool horizontal = false, string elementName = "domino-tile-back")
    {
        var tileElement = new VisualElement { name = elementName };
        tileElement.AddToClassList("domino-tile-kenney");
        
        if (horizontal)
            tileElement.AddToClassList("domino-tile-kenney-horizontal");
        
        ApplyTileBackToElement(tileElement, theme, horizontal);
        return tileElement;
    }
    
    // Theme switching
    public void SetDefaultTheme(TileTheme theme) => _defaultTheme = theme;
    public TileTheme GetDefaultTheme() => _defaultTheme;
    
    // Validation
    public bool HasTile(TileTheme theme, int left, int right)
    {
        var value = new DominoValue(left, right);
        return _tileCache.TryGetValue(theme, out var dict) && dict.ContainsKey(value);
    }
    
    public int GetLoadedTileCount(TileTheme theme)
    {
        return _tileCache.TryGetValue(theme, out var dict) ? dict.Count : 0;
    }
}