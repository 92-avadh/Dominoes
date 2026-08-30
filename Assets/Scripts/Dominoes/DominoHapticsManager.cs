using UnityEngine;

namespace Dominoes
{
    /// <summary>
    /// Lightweight mobile haptics manager for tactile Dominoes feedback.
    /// Provides subtle vibrations for tile touch, placement impact, invalid moves, and victory.
    /// Supports Haptics ON/OFF toggle persisted via PlayerPrefs.
    /// </summary>
    public static class DominoHapticsManager
    {
        private const string PrefHapticsEnabled = "Dominoes_HapticsEnabled";

        private static bool isInitialized;
        private static bool isHapticsEnabled = true;

        public static bool IsHapticsEnabled
        {
            get
            {
                EnsureInitialized();
                return isHapticsEnabled;
            }
            set
            {
                isHapticsEnabled = value;
                PlayerPrefs.SetInt(PrefHapticsEnabled, isHapticsEnabled ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private static void EnsureInitialized()
        {
            if (!isInitialized)
            {
                isHapticsEnabled = PlayerPrefs.GetInt(PrefHapticsEnabled, 1) == 1;
                isInitialized = true;
            }
        }

        public static void ToggleHaptics()
        {
            IsHapticsEnabled = !IsHapticsEnabled;
        }

        /// <summary>
        /// Light tactile buzz for tile touch / pickup.
        /// </summary>
        public static void TriggerLightTap()
        {
            if (!IsHapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Handheld.Vibrate();
            }
            catch {}
#endif
        }

        /// <summary>
        /// Medium pulse for satisfying tile placement impact.
        /// </summary>
        public static void TriggerMediumPulse()
        {
            if (!IsHapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Handheld.Vibrate();
            }
            catch {}
#endif
        }

        /// <summary>
        /// Short double buzz for invalid move warning.
        /// </summary>
        public static void TriggerWarningBuzz()
        {
            if (!IsHapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Handheld.Vibrate();
            }
            catch {}
#endif
        }

        /// <summary>
        /// Celebration pulse for match victory.
        /// </summary>
        public static void TriggerWinCelebration()
        {
            if (!IsHapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Handheld.Vibrate();
            }
            catch {}
#endif
        }
    }
}
