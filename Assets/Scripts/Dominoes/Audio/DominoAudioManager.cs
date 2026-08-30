using System;
using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Professional singleton Audio Manager for the Dominoes game.
    /// Handles background music, tactile UI sound effects, tile placement "clack",
    /// boneyard draw shuffles, turn chimes, and win/loss feedback.
    /// Supports volume controls, mute toggles, and PlayerPrefs persistence.
    /// </summary>
    [DisallowMultipleComponent]
    public class DominoAudioManager : MonoBehaviour
    {
        private const string PrefMusicVol = "Dominoes_MusicVolume";
        private const string PrefSfxVol = "Dominoes_SfxVolume";
        private const string PrefMusicMute = "Dominoes_MusicMuted";
        private const string PrefSfxMute = "Dominoes_SfxMuted";

        public static DominoAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private AudioClip sfxClick;
        [SerializeField] private AudioClip sfxTilePickup;
        [SerializeField] private AudioClip sfxTileClack;
        [SerializeField] private AudioClip sfxTileDraw;
        [SerializeField] private AudioClip sfxTurnChime;
        [SerializeField] private AudioClip sfxPass;
        [SerializeField] private AudioClip sfxWin;
        [SerializeField] private AudioClip sfxLoss;

        private float musicVolume = 0.6f;
        private float sfxVolume = 0.85f;
        private bool isMusicMuted = false;
        private bool isSfxMuted = false;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                ApplyMusicVolume();
                SaveSettings();
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                ApplySfxVolume();
                SaveSettings();
            }
        }

        public bool IsMusicMuted
        {
            get => isMusicMuted;
            set
            {
                isMusicMuted = value;
                ApplyMusicVolume();
                SaveSettings();
            }
        }

        public bool IsSfxMuted
        {
            get => isSfxMuted;
            set
            {
                isSfxMuted = value;
                ApplySfxVolume();
                SaveSettings();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
                LoadSettings();
                LoadAudioClipsIfMissing();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            PlayMusic();
        }

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void LoadAudioClipsIfMissing()
        {
#if UNITY_EDITOR
            if (musicClip == null) musicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/music_ambient_loop.wav");
            if (sfxClick == null) sfxClick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_click.wav");
            if (sfxTilePickup == null) sfxTilePickup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_tile_pickup.wav");
            if (sfxTileClack == null) sfxTileClack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_tile_clack.wav");
            if (sfxTileDraw == null) sfxTileDraw = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_tile_draw.wav");
            if (sfxTurnChime == null) sfxTurnChime = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_turn_chime.wav");
            if (sfxPass == null) sfxPass = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_pass.wav");
            if (sfxWin == null) sfxWin = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_win.wav");
            if (sfxLoss == null) sfxLoss = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_loss.wav");
#endif
        }

        public void PlayMusic()
        {
            if (musicSource == null || musicClip == null) return;
            if (musicSource.isPlaying && musicSource.clip == musicClip) return;

            musicSource.clip = musicClip;
            ApplyMusicVolume();
            if (!isMusicMuted)
            {
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PlayClick()
        {
            PlaySFX(sfxClick, 0.7f);
        }

        public void PlayTilePickup()
        {
            PlaySFX(sfxTilePickup, 0.8f);
        }

        public void PlayTileClack()
        {
            PlaySFX(sfxTileClack, 1.0f);
        }

        public void PlayTileDraw()
        {
            PlaySFX(sfxTileDraw, 0.85f);
        }

        public void PlayTurnChime()
        {
            PlaySFX(sfxTurnChime, 0.9f);
        }

        public void PlayPass()
        {
            PlaySFX(sfxPass, 0.8f);
        }

        public void PlayWin()
        {
            PlaySFX(sfxWin, 1.0f);
        }

        public void PlayLoss()
        {
            PlaySFX(sfxLoss, 0.85f);
        }

        private void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (isSfxMuted || sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        public void ToggleMusic()
        {
            IsMusicMuted = !IsMusicMuted;
        }

        public void ToggleSfx()
        {
            IsSfxMuted = !IsSfxMuted;
        }

        private void ApplyMusicVolume()
        {
            if (musicSource == null) return;
            musicSource.volume = isMusicMuted ? 0f : musicVolume;
            if (isMusicMuted)
            {
                musicSource.Pause();
            }
            else if (!musicSource.isPlaying && musicClip != null)
            {
                musicSource.UnPause();
                if (!musicSource.isPlaying) musicSource.Play();
            }
        }

        private void ApplySfxVolume()
        {
            if (sfxSource == null) return;
            sfxSource.volume = isSfxMuted ? 0f : sfxVolume;
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefMusicVol, musicVolume);
            PlayerPrefs.SetFloat(PrefSfxVol, sfxVolume);
            PlayerPrefs.SetInt(PrefMusicMute, isMusicMuted ? 1 : 0);
            PlayerPrefs.SetInt(PrefSfxMute, isSfxMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            musicVolume = PlayerPrefs.GetFloat(PrefMusicVol, 0.6f);
            sfxVolume = PlayerPrefs.GetFloat(PrefSfxVol, 0.85f);
            isMusicMuted = PlayerPrefs.GetInt(PrefMusicMute, 0) == 1;
            isSfxMuted = PlayerPrefs.GetInt(PrefSfxMute, 0) == 1;

            ApplyMusicVolume();
            ApplySfxVolume();
        }
    }
}
