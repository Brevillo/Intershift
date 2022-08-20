using UnityEngine;

/// <summary>
/// Animation Curve with with built in timer and easily accesible duration and scale.
/// </summary>
[System.Serializable]
public class SmartCurve {

    /* Animation curve but...
    - built in timer
    - can auto scale time and value
    - start, stop, and peak functions
    - has function to say if the curve is finished or peaked
    - can return the derivative of the curve
    - can easily switch between scaled and unscaled delatime
    */

    public AnimationCurve curve;
    public float
        timeScale = 1,
        valueScale = 1;
    public bool unscaledTime = false,
                fixedTime = false;

    private float timer = 0;
    public SmartCurve() => new SmartCurve(null);
    public SmartCurve(AnimationCurve curve, float timeScale = 1, float valueScale = 1, bool unscaledTime = false) {
        this.curve        = curve;
        this.unscaledTime = unscaledTime;
        this.valueScale   = valueScale;
        this.timeScale    = timeScale;
        timer = 0;
    }

    /// <summary>
    /// Increments the timer and evalutes the curve at that time. 
    /// </summary>
    public float Evaluate(bool derivative = false) {
        if (curve == null) return 0;
        float deltaTime = fixedTime ? unscaledTime ? Time.fixedUnscaledDeltaTime : Time.fixedDeltaTime
                                    : unscaledTime ? Time.unscaledDeltaTime      : Time.deltaTime;
        timer += deltaTime / timeScale;
        return (derivative ? Derivative(timer) / timeScale : curve.Evaluate(timer)) * valueScale;
    }

    /// <summary>
    /// Duplicates and returns the curve.
    /// </summary>
    public SmartCurve Copy() => new SmartCurve(curve, timeScale, valueScale, unscaledTime);

    /// <summary>
    /// Starts the curve's timer.
    /// </summary>
    public void Start() => timer = 0;
    /// <summary>
    /// Stops the curve's timer.
    /// </summary>
    public void Stop() => timer = Mathf.Infinity;

    /// <summary>
    /// Specifies whether curve's timer has finished.
    /// </summary>
    public bool Done() {
        if (curve == null) return true;
        var keys = curve.keys;
        return timer > keys[keys.Length - 1].time;
    }

    const float delta = 0.000001f;
    private float Derivative(float time) {
        float x1 = time - delta,
              x2 = time + delta,
              y1 = curve.Evaluate(x1),
              y2 = curve.Evaluate(x2);
        return (y2 - y1) / (x2 - x1);
    }
}

/// <summary>
/// Allows for stringing multiple smart curves together, each with separate characteristics.
/// </summary>
[System.Serializable]
public class SmartCurveComposite {

    public SmartCurve[] curves;
    private int currentCurve;

    public SmartCurveComposite(SmartCurve[] curves) => this.curves = curves;

    /// <summary>
    /// Evaluates for the current curve and progresses to the next if the current is finished.
    /// </summary>
    public float Evaluate() {
        if (curves[currentCurve].Done()) currentCurve++;
        return curves[currentCurve].Evaluate();
    }

    /// <summary>
    /// Duplicates and returns the smart curve composite
    /// </summary>
    public SmartCurveComposite Copy() => new SmartCurveComposite(curves);

    /// <summary>
    /// Starts the smart curve composite.
    /// </summary>
    public void Start() {
        currentCurve = 0;
        foreach (SmartCurve c in curves) c.Start();
    }

    /// <summary>
    /// Stops the smart curve composite.
    /// </summary>
    public void Stop() {
        currentCurve = curves.Length - 1;
        foreach (SmartCurve c in curves) c.Stop();
    }

    /// <summary>
    /// Specifies whether the smart curve composite has finished.
    /// </summary>
    public bool Done() => curves[curves.Length - 1].Done();
}