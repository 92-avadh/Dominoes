using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AudioManager - Layered SFX + Background Music system
/// Manages all audio playback with pitch/volume variation
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource secondarySfxSource;

    [Header("Settings")]
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;

    // Cached audio clips
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> musicClips = new Dictionary<string, AudioClip>();

    // Pitch variation settings
    private const float PITCH_VARIATION = 0.05f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create audio sources if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        if (secondarySfxSource == null)
        {
            secondarySfxSource = gameObject.AddComponent<AudioSource>();
            secondarySfxSource.playOnAwake = false;
        }

        LoadAudioClips();
    }

    private void LoadAudioClips()
    {
        // Load all SFX clips from Resources/Audio/SFX/
        var sfxResources = Resources.LoadAll<AudioClip>("Audio/SFX");
        foreach (var clip in sfxResources)
        {
            if (clip != null) sfxClips[clip.name.ToLowerInvariant()] = clip;
        }

        // Load all music clips from Resources/Audio/Music/
        var musicResources = Resources.LoadAll<AudioClip>("Audio/Music");
        foreach (var clip in musicResources)
        {
            if (clip != null) musicClips[clip.name.ToLowerInvariant()] = clip;
        }

        Debug.Log($"[AudioManager] Loaded {sfxClips.Count} SFX clips and {musicClips.Count} music clips");
    }

    #region SFX Playback

    /// <summary>
    /// Play a sound effect by name with optional volume and pitch variation
    /// </summary>
    public void PlaySFX(string clipName, float volume = 1f, bool randomizePitch = true)
    {
        if (!sfxEnabled) return;
        string key = clipName.ToLowerInvariant();
        if (!sfxClips.TryGetValue(key, out var clip))
        {
            Debug.LogWarning($"[AudioManager] SFX clip not found: {clipName}");
            return;
        }
        PlaySFXInternal(clip, volume, randomizePitch);
    }

    /// <summary>
    /// Play a layered SFX (two sounds at once for richer audio)
    /// </summary>
    public void PlayLayeredSFX(string primaryClip, string secondaryClip, float primaryVolume = 1f, float secondaryVolume = 0.5f)
    {
        if (!sfxEnabled) return;
        string key1 = primaryClip.ToLowerInvariant();
        string key2 = secondaryClip.ToLowerInvariant();
        if (sfxClips.TryGetValue(key1, out var primary))
        {
            PlaySFXInternal(primary, primaryVolume, true);
        }
        if (sfxClips.TryGetValue(key2, out var secondary))
        {
            secondarySfxSource.PlayOneShot(secondary, secondaryVolume * sfxVolume * masterVolume);
        }
    }

    private void PlaySFXInternal(AudioClip clip, float volume, bool randomizePitch)
    {
        if (randomizePitch)
        {
            sfxSource.pitch = 1f + Random.Range(-PITCH_VARIATION, PITCH_VARIATION);
        }
        else
        {
            sfxSource.pitch = 1f;
        }
        sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }

    #endregion

    #region Music Playback

    /// <summary>
    /// Play background music by name (stops current music first)
    /// </summary>
    public void PlayMusic(string clipName, float volume = 1f)
    {
        if (!musicEnabled) return;
        string key = clipName.ToLowerInvariant();
        if (!musicClips.TryGetValue(key, out var clip))
        {
            Debug.LogWarning($"[AudioManager] Music clip not found: {clipName}");
            return;
        }
        musicSource.clip = clip;
        musicSource.volume = volume * musicVolume * masterVolume;
        musicSource.Play();
    }

    /// <summary>
    /// Crossfade between music tracks
    /// </summary>
    public void CrossfadeMusic(string newClipName, float duration = 0.5f)
    {
        StartCoroutine(CrossfadeMusicCoroutine(newClipName, duration));
    }

    private System.Collections.IEnumerator CrossfadeMusicCoroutine(string newClipName, float duration)
    {
        string key = newClipName.ToLowerInvariant();
        if (!musicClips.TryGetValue(key, out var newClip)) yield break;

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        // Fade out current
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration * 0.5f));
            yield return null;
        }

        // Switch clip
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume * masterVolume, elapsed / (duration * 0.5f));
            yield return null;
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    #endregion

    #region Convenience Methods

    public void PlayClick() => PlaySFX("Button Press", 0.8f, true);
    public void PlayTilePickup() => PlaySFX("Felt Scrape", 0.6f, true);
    public void PlayTilePlace() => PlayLayeredSFX("Tile Place", "Wood Sounds - Wood Clack", 0.8f, 0.4f);
    public void PlayInvalid() => PlaySFX("Error Buzz - Buzz", 0.7f, false);
    public void PlaySuccess() => PlaySFX("Success Chime", 0.8f, false);
    public void PlayCountdownTick() => PlaySFX("Countdown Tick", 0.5f, false);
    public void PlayShuffle() => PlaySFX("Shuffle", 0.6f, true);
    public void PlayWinFanfare() => PlaySFX("Triumphant Fanfare - Fanfare for Space", 0.9f, false);

    public void PlayMenuMusic() => PlayMusic("Tropical Lounge - Secret of Tiki Island", 0.4f);
    public void PlayGameplayMusic() => PlayMusic("Upbeat Electron - Cloud Dancer", 0.3f);
    public void PlayVictoryMusic() => PlayMusic("Triumphant Fanfare - Fanfare for Space", 0.5f);

    #endregion

    #region Volume Control

    public void SetMasterVolume(float vol) { masterVolume = Mathf.Clamp01(vol); }
    public void SetSFXVolume(float vol) { sfxVolume = Mathf.Clamp01(vol); }
    public void SetMusicVolume(float vol) { musicVolume = Mathf.Clamp01(vol); musicSource.volume = musicVolume * masterVolume; }

    public void ToggleMusic() { musicEnabled = !musicEnabled; if (!musicEnabled) musicSource.Pause(); else musicSource.UnPause(); }
    public void ToggleSFX() { sfxEnabled = !sfxEnabled; }

    public bool IsMusicEnabled => musicEnabled;
    public bool IsSFXEnabled => sfxEnabled;

    #endregion
}
