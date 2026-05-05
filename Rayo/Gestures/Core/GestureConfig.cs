namespace Rayo.Gestures.Core;

/// <summary>
/// Global configuration for gesture recognition thresholds and timing.
/// All values can be modified at runtime to customize gesture behavior.
/// </summary>
public static class GestureConfig
{
    // ========== TAP GESTURE ==========

    /// <summary>
    /// Maximum movement distance (in pixels) allowed for a tap.
    /// </summary>
    public static float TapMaxDistance { get; set; } = 5f;

    /// <summary>
    /// Maximum duration (in milliseconds) for a tap.
    /// </summary>
    public static int TapMaxDurationMs { get; set; } = 300;

    // ========== DOUBLE-TAP GESTURE ==========

    /// <summary>
    /// Maximum interval (in milliseconds) between two taps for double-tap recognition.
    /// </summary>
    public static int DoubleTapIntervalMs { get; set; } = 500;

    /// <summary>
    /// Maximum distance (in pixels) between two tap positions for double-tap recognition.
    /// </summary>
    public static float DoubleTapMaxDistance { get; set; } = 20f;

    // ========== LONG-PRESS GESTURE ==========

    /// <summary>
    /// Minimum duration (in milliseconds) required to trigger long-press.
    /// </summary>
    public static int LongPressDurationMs { get; set; } = 500;

    /// <summary>
    /// Maximum movement distance (in pixels) allowed during long-press.
    /// </summary>
    public static float LongPressMaxDistance { get; set; } = 10f;

    // ========== SWIPE GESTURE ==========

    /// <summary>
    /// Minimum velocity (in pixels per second) required to recognize a swipe.
    /// </summary>
    public static float SwipeMinVelocity { get; set; } = 500f;

    /// <summary>
    /// Minimum distance (in pixels) required to recognize a swipe.
    /// </summary>
    public static float SwipeMinDistance { get; set; } = 50f;

    /// <summary>
    /// Maximum angle deviation (in degrees) from cardinal directions for swipe recognition.
    /// For example, 30 degrees means swipe must be within ±30° of up/down/left/right.
    /// </summary>
    public static float SwipeMaxAngleDeviation { get; set; } = 30f;

    // ========== PAN GESTURE ==========

    /// <summary>
    /// Minimum distance (in pixels) before pan gesture is recognized.
    /// </summary>
    public static float PanMinDistance { get; set; } = 5f;
}
