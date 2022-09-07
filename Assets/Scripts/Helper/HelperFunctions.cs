using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helper {

    /// <summary> Convert degree float value to Vector2. </summary>
    public static Vector2 DegToVector(this float deg) => RadToVector(deg * Mathf.Deg2Rad);
    /// <summary> Convert radian float value to Vector2 </summary>
    public static Vector2 RadToVector(this float rad) => new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

    /// <summary> Get the sign of the float value. Evaluates 0 at 0. </summary>
    public static int Sign0(this float f) => f > 0 ? 1 : f < 0 ? -1 : 0;

    /// <summary> Round the float value to demical place p. </summary>
    public static float RoundTo(this float i, int p) {
        float ten = Mathf.Pow(10, p);
        return Mathf.Round(i * ten) / ten;
    }

    private static float ogFixedDeltaTime;
    /// <summary> Set the regular and fixed time scale. </summary>
    public static void SetTimeScale(float i) {
        if (ogFixedDeltaTime == 0) ogFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = i;
        Time.fixedDeltaTime = ogFixedDeltaTime * i;
    }

    /// <summary> Returns the absolute value of the Vector2. </summary>
    public static Vector2 Abs(this Vector2 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));

    #region Modify Vector3/2 Components

    /// <summary> Set the x component of a Vector3. </summary>
    public static void X(this ref Vector3 v, float x) => v = new Vector3(x, v.y, v.z);
    /// <summary> Set the y component of a Vector3. </summary>
    public static void Y(this ref Vector3 v, float y) => v = new Vector3(v.x, y, v.z);
    /// <summary> Set the z component of a Vector3. </summary>
    public static void Z(this ref Vector3 v, float z) => v = new Vector3(v.x, v.y, z);

    /// <summary> Set the x and y component of a Vector3. </summary>
    public static void XY(this ref Vector3 v, float x, float y) => v = new Vector3(x, y, v.z);
    /// <summary> Set the x and y component of a Vector3 using a Vector2. </summary>
    public static void XY(this ref Vector3 v, Vector2 xy) => v.XY(xy.x, xy.y);
    /// <summary> Set the x and z component of a Vector3. </summary>
    public static void XZ(this ref Vector3 v, float x, float z) => v = new Vector3(x, v.y, z);
    /// <summary> Set the x and z components of a Vector3 using a Vector2. </summary>
    public static void XZ(this ref Vector3 v, Vector2 xz) => v.XZ(xz.x, xz.y);
    /// <summary> Set the y and z components of a Vector3. </summary>
    public static void YZ(this ref Vector3 v, float y, float z) => v = new Vector3(v.x, y, z);
    /// <summary> Set the y and z components of a Vector3 using a Vector2. </summary>
    public static void YZ(this ref Vector3 v, Vector2 yz) => v.XZ(yz.x, yz.y);

    /// <summary> Set the x component of a Vector2. </summary>
    public static void X(this ref Vector2 v, float x) => v = new Vector2(x, v.y);
    /// <summary> Set the y component of a Vector2. </summary>
    public static void Y(this ref Vector2 v, float y) => v = new Vector2(v.x, y);

    #endregion
}
