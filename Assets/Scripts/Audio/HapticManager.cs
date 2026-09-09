using UnityEngine;

/// <summary>
/// HapticManager - Android haptic feedback system
/// Provides vibration patterns for different game actions
/// </summary>
public static class HapticManager
{
    private static AndroidJavaObject vibrator = null;
    private static bool hapticsEnabled = true;

    public static bool IsHapticsEnabled => hapticsEnabled;

    public static void ToggleHaptics() => hapticsEnabled = !hapticsEnabled;

    /// <summary>
    /// Light tap - for button presses, tile pickup
    /// </summary>
    public static void Light()
    {
        if (!hapticsEnabled) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var vibratorService = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibratorService != null) {
                    vibratorService.Call("vibrate", 20L);
                }
            }
        } catch { }
        #elif UNITY_IOS && !UNITY_EDITOR
        iOSHaptic.iOSHapticFeedback.Trigger(iOSHaptic.iOSHapticFeedback.iOSHapticType.LightImpact);
        #endif
    }

    /// <summary>
    /// Medium impact - for tile placement, valid moves
    /// </summary>
    public static void Medium()
    {
        if (!hapticsEnabled) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var vibratorService = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibratorService != null) {
                    vibratorService.Call("vibrate", 50L);
                }
            }
        } catch { }
        #elif UNITY_IOS && !UNITY_EDITOR
        iOSHaptic.iOSHapticFeedback.Trigger(iOSHaptic.iOSHapticFeedback.iOSHapticType.MediumImpact);
        #endif
    }

    /// <summary>
    /// Heavy impact - for invalid moves, errors
    /// </summary>
    public static void Heavy()
    {
        if (!hapticsEnabled) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var vibratorService = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibratorService != null) {
                    long[] pattern = { 0, 50, 30, 50 };
                    vibratorService.Call("vibrate", pattern, -1);
                }
            }
        } catch { }
        #elif UNITY_IOS && !UNITY_EDITOR
        iOSHaptic.iOSHapticFeedback.Trigger(iOSHaptic.iOSHapticFeedback.iOSHapticType.HeavyImpact);
        #endif
    }

    /// <summary>
    /// Success pattern - for valid placements, wins
    /// </summary>
    public static void Success()
    {
        if (!hapticsEnabled) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var vibratorService = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibratorService != null) {
                    long[] pattern = { 0, 30, 50, 30, 50, 30 };
                    vibratorService.Call("vibrate", pattern, -1);
                }
            }
        } catch { }
        #endif
    }

    /// <summary>
    /// Warning buzz - for invalid moves, last countdown
    /// </summary>
    public static void WarningBuzz()
    {
        if (!hapticsEnabled) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var vibratorService = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibratorService != null) {
                    long[] pattern = { 0, 100, 50, 100 };
                    vibratorService.Call("vibrate", pattern, -1);
                }
            }
        } catch { }
        #endif
    }

    /// <summary>
    /// Turn start notification
    /// </summary>
    public static void TurnStart()
    {
        if (!hapticsEnabled) return;
        Light();
    }

    /// <summary>
    /// Countdown tick (last 3 seconds)
    /// </summary>
    public static void CountdownTick()
    {
        if (!hapticsEnabled) return;
        Light();
    }
}
