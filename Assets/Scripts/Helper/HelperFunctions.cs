using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helper {

    /// <summary>
    /// Convert degree float value to Vector2.
    /// </summary>
    public static Vector2 DegToVector(this float deg) => RadToVector(deg * Mathf.Deg2Rad);
    /// <summary>
    /// Convert radian float value to Vector2
    /// </summary>
    public static Vector2 RadToVector(this float rad) => new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

    /// <summary>
    /// Get the sign of the float value. Evaluates 0 at 0.
    /// </summary>
    public static int Sign0(this float f) => f > 0 ? 1 : f < 0 ? -1 : 0;

    /// <summary>
    /// Round the float value to demical place p.
    /// </summary>
    public static float RoundTo(this float i, int p) {
        float ten = Mathf.Pow(10, p);
        return Mathf.Round(i * ten) / ten;
    }

    private static float ogFixedDeltaTime;
    /// <summary>
    /// Set the regular and fixed time scale.
    /// </summary>
    public static void SetTimeScale(float i) {
        if (ogFixedDeltaTime == 0) ogFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = i;
        Time.fixedDeltaTime = ogFixedDeltaTime * i;
    }
}
