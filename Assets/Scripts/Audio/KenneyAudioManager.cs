using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kenney Audio Manager - Handles all UI sound effects from Kenney UI Pack
/// Provides easy playback for click, tap, switch, and other UI sounds
/// </summary>
public class KenneyAudioManager : MonoBehaviour
{
    public static KenneyAudioManager Instance { get; private set; }
    
    [Header("Audio Source")]
    [SerializeField] private AudioSource _uiAudioSource;
    [SerializeField] private AudioSource _musicAudioSource;
    
    [Header("Kenney UI Sounds")]
    [SerializeField] private AudioClip _clickA;
    [SerializeField] private AudioClip _clickB;
    [SerializeField] private AudioClip _tapA;
    [SerializeField] private AudioClip _tapB;
    [SerializeField] private AudioClip _switchA;
    [SerializeField] private AudioClip _switchB;
    
    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float _uiVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.5f;
    [SerializeField] private bool _muteUI = false;
    [SerializeField] private bool _muteMusic = false;
    
    [Header("Pitch Variation (for natural feel)")]
    [SerializeField] private float _pitchVariation = 0.05f;
    
    // Sound type enum
    public enum UISoundType
    {
        Click,      // Primary button press
        ClickAlt,   // Secondary button press
        Tap,        // Light tap (toggle, checkbox)
        TapAlt,     // Alternate tap
        Switch,     // Toggle switch on/off
        SwitchAlt,  // Alternate switch
        Success,    // Positive action (win, complete)
        Error,      // Negative action (fail, invalid)
        Pop,        // Modal open
        Whoosh,     // Modal close / navigation
    }
    
    // Clip mapping
    private Dictionary<UISoundType, AudioClip[]> _soundClips = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeAudioSources();
        LoadKenneySounds();
        BuildSoundMapping();
    }
    
    private void InitializeAudioSources()
    {
        // Create UI audio source if missing
        if (_uiAudioSource == null)
        {
            _uiAudioSource = gameObject.AddComponent<AudioSource>();
            _uiAudioSource.playOnAwake = false;
            _uiAudioSource.spatialBlend = 0f; // 2D
            _uiAudioSource.volume = _uiVolume;
        }
        
        // Create music audio source if missing
        if (_musicAudioSource == null)
        {
            _musicAudioSource = gameObject.AddComponent<AudioSource>();
            _musicAudioSource.playOnAwake = false;
            _musicAudioSource.loop = true;
            _musicAudioSource.spatialBlend = 0f;
            _musicAudioSource.volume = _musicVolume;
        }
    }
    
    private void LoadKenneySounds()
    {
        // Load from Resources folder (where we copied Kenney sounds)
        _clickA = Resources.Load<AudioClip>("Sounds/click-a");
        _clickB = Resources.Load<AudioClip>("Sounds/click-b");
        _tapA = Resources.Load<AudioClip>("Sounds/tap-a");
        _tapB = Resources.Load<AudioClip>("Sounds/tap-b");
        _switchA = Resources.Load<AudioClip>("Sounds/switch-a");
        _switchB = Resources.Load<AudioClip>("Sounds/switch-b");
        
        // Log warnings for missing clips
        if (_clickA == null) Debug.LogWarning("[KenneyAudio] click-a.ogg not found in Resources/Sounds/");
        if (_clickB == null) Debug.LogWarning("[KenneyAudio] click-b.ogg not found in Resources/Sounds/");
        if (_tapA == null) Debug.LogWarning("[KenneyAudio] tap-a.ogg not found in Resources/Sounds/");
        if (_tapB == null) Debug.LogWarning("[KenneyAudio] tap-b.ogg not found in Resources/Sounds/");
        if (_switchA == null) Debug.LogWarning("[KenneyAudio] switch-a.ogg not found in Resources/Sounds/");
        if (_switchB == null) Debug.LogWarning("[KenneyAudio] switch-b.ogg not found in Resources/Sounds/");
    }
    
    private void BuildSoundMapping()
    {
        _soundClips[UISoundType.Click] = new[] { _clickA }.FilterNull();
        _soundClips[UISoundType.ClickAlt] = new[] { _clickB }.FilterNull();
        _soundClips[UISoundType.Tap] = new[] { _tapA }.FilterNull();
        _soundClips[UISoundType.TapAlt] = new[] { _tapB }.FilterNull();
        _soundClips[UISoundType.Switch] = new[] { _switchA }.FilterNull();
        _soundClips[UISoundType.SwitchAlt] = new[] { _switchB }.FilterNull();
        
        // Fallback: if specific sound missing, use click
        var fallback = new[] { _clickA, _clickB }.FilterNull();
        foreach (UISoundType type in System.Enum.GetValues(typeof(UISoundType)))
        {
            if (!_soundClips.ContainsKey(type) || _soundClips[type].Length == 0)
            {
                _soundClips[type] = fallback;
            }
        }
    }
    
    /// <summary>Play a UI sound by type</summary>
    public void Play(UISoundType type, float volumeScale = 1f)
    {
        if (_muteUI || _uiAudioSource == null) return;
        
        if (_soundClips.TryGetValue(type, out var clips) && clips.Length > 0)
        {
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
            {
                // Add slight pitch variation for natural feel
                _uiAudioSource.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
                _uiAudioSource.PlayOneShot(clip, _uiVolume * volumeScale);
            }
        }
    }
    
    /// <summary>Play a specific clip directly</summary>
    public void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        if (_muteUI || _uiAudioSource == null || clip == null) return;
        
        _uiAudioSource.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
        _uiAudioSource.PlayOneShot(clip, _uiVolume * volumeScale);
    }
    
    // Convenience methods for common UI interactions
    public void PlayButtonClick() => Play(UISoundType.Click);
    public void PlayButtonClickAlt() => Play(UISoundType.ClickAlt);
    public void PlayToggle() => Play(UISoundType.Switch);
    public void PlayToggleAlt() => Play(UISoundType.SwitchAlt);
    public void PlayTap() => Play(UISoundType.Tap);
    public void PlayTapAlt() => Play(UISoundType.TapAlt);
    public void PlaySuccess() => Play(UISoundType.Success);
    public void PlayError() => Play(UISoundType.Error);
    public void PlayModalOpen() => Play(UISoundType.Pop);
    public void PlayModalClose() => Play(UISoundType.Whoosh);
    public void PlayNavigation() => Play(UISoundType.Whoosh);
    
    // Volume controls
    public void SetUIVolume(float volume)
    {
        _uiVolume = Mathf.Clamp01(volume);
        if (_uiAudioSource != null) _uiAudioSource.volume = _uiVolume;
    }
    
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        if (_musicAudioSource != null) _musicAudioSource.volume = _musicVolume;
    }
    
    public void MuteUI(bool mute) => _muteUI = mute;
    public void MuteMusic(bool mute) => _muteMusic = mute;
    
    public float UIVolume => _uiVolume;
    public float MusicVolume => _musicVolume;
    public bool IsUIMuted => _muteUI;
    public bool IsMusicMuted => _muteMusic;
    
    // Music playback
    public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 1f)
    {
        if (_musicAudioSource == null || clip == null) return;
        
        if (_musicAudioSource.isPlaying)
        {
            StartCoroutine(CrossFadeMusic(clip, loop, fadeDuration));
        }
        else
        {
            _musicAudioSource.clip = clip;
            _musicAudioSource.loop = loop;
            _musicAudioSource.volume = _musicVolume;
            _musicAudioSource.Play();
        }
    }
    
    public void StopMusic(float fadeDuration = 1f)
    {
        if (_musicAudioSource == null || !_musicAudioSource.isPlaying) return;
        StartCoroutine(FadeOutMusic(fadeDuration));
    }
    
    private System.Collections.IEnumerator CrossFadeMusic(AudioClip newClip, bool loop, float duration)
    {
        float startVolume = _musicAudioSource.volume;
        float elapsed = 0f;
        
        // Fade out
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        // Switch clip
        _musicAudioSource.clip = newClip;
        _musicAudioSource.loop = loop;
        _musicAudioSource.Play();
        
        // Fade in
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _musicAudioSource.volume = Mathf.Lerp(0f, _musicVolume, elapsed / duration);
            yield return null;
        }
        _musicAudioSource.volume = _musicVolume;
    }
    
    private System.Collections.IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = _musicAudioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        _musicAudioSource.Stop();
        _musicAudioSource.volume = _musicVolume;
    }
}

// Extension for filtering null from arrays
public static class ArrayExtensions
{
    public static T[] FilterNull<T>(this T[] array) where T : class
    {
        if (array == null) return System.Array.Empty<T>();
        var list = new System.Collections.Generic.List<T>();
        foreach (var item in array)
            if (item != null) list.Add(item);
        return list.ToArray();
    }
}