using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Game.Platform
{
    public enum HapticKind { Light = 0, Medium = 1, Heavy = 2, Rigid = 3 }

    /// <summary>
    /// One-shot impact haptics. iOS: UIImpactFeedbackGenerator (Assets/Plugins/iOS/AppHaptics.mm).
    /// Android: VibrationEffect one-shots with amplitude (API 26+). Editor and other platforms: no-op.
    /// Native bridges and SDK calls live only in this assembly (docs/rules/platform.md).
    /// </summary>
    public static class Haptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void AppHaptic(int kind);
        public static void Play(HapticKind kind) => AppHaptic((int)kind);
#elif UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;

        // Never called: referencing Handheld.Vibrate makes the Android build add the VIBRATE permission.
        static void DeclareVibratePermission() => Handheld.Vibrate();

        public static void Play(HapticKind kind)
        {
            try
            {
                if (_vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                long ms = kind == HapticKind.Light ? 12 : kind == HapticKind.Medium ? 20 : kind == HapticKind.Rigid ? 18 : 35;
                int amp = kind == HapticKind.Light ? 70 : kind == HapticKind.Medium ? 140 : kind == HapticKind.Rigid ? 200 : 255;
                using var effect = new AndroidJavaClass("android.os.VibrationEffect");
                _vibrator.Call("vibrate", effect.CallStatic<AndroidJavaObject>("createOneShot", ms, amp));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Game] haptics unavailable: {e.Message}");
            }
        }
#else
        public static void Play(HapticKind kind) { }
#endif
    }
}
