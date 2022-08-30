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
    public enum TimeType { Regular, Fixed, Unscaled, FixedUnscaled};
    public TimeType timeType = TimeType.Regular;

    private float timer = 0;
    public SmartCurve() => new SmartCurve(null, 1, 1, TimeType.Regular);
    public SmartCurve(AnimationCurve curve, float timeScale, float valueScale, TimeType timeType) {
        this.curve      = curve;
        this.timeType   = timeType;
        this.valueScale = valueScale;
        this.timeScale  = timeScale;
        timer = 0;
    }

    /// <summary>
    /// Increments the timer and evalutes the curve at that time. 
    /// </summary>
    public float Evaluate(bool derivative = false) {
        if (curve == null) return 0;

        float deltaTime = timeType switch {
            TimeType.Fixed => Time.fixedDeltaTime,
            TimeType.Unscaled => Time.unscaledDeltaTime,
            TimeType.FixedUnscaled => Time.fixedUnscaledDeltaTime,
            _ => Time.deltaTime
        };

        timer += deltaTime / timeScale;
        return (derivative ? Derivative(timer) / timeScale : curve.Evaluate(timer)) * valueScale;
    }

    /// <summary>
    /// Duplicates and returns the curve.
    /// </summary>
    public SmartCurve Copy() => new SmartCurve(curve, timeScale, valueScale, timeType);

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

    public bool autoContinue;
    public SmartCurve[] curves;
    private int currentCurve;
    private bool doneWithCurrentCurve;

    public SmartCurveComposite(SmartCurve[] curves) => this.curves = curves;

    /// <summary>
    /// Evaluates for the current curve. Progresses to the next if the current is finished and auto-continue is enabled.
    /// </summary>
    public float Evaluate() {
        if (curves[currentCurve].Done()) {
            if (autoContinue) currentCurve++;
            else doneWithCurrentCurve = true;
        }
        return curves[currentCurve].Evaluate();
    }

    /// <summary>
    /// Duplicates and returns the smart curve composite
    /// </summary>
    public SmartCurveComposite Copy() => new SmartCurveComposite(curves);

    /// <summary>
    /// Starts the smart curve composite.
    /// </summary>
    /// <param name="startAt">
    /// Start at a specific curve.
    /// </param>
    public void Start(int startAt = 0) {
        currentCurve = startAt;
        doneWithCurrentCurve = false;
        for (int i = startAt; i < curves.Length; i++) curves[i].Start();
    }

    /// <summary>
    /// Progress to the next curve.
    /// </summary>
    public void Continue() {
        currentCurve++;
        doneWithCurrentCurve = false;
    }

    /// <summary>
    /// Stops the smart curve composite.
    /// </summary>
    public void Stop() {
        currentCurve = curves.Length - 1;
        foreach (SmartCurve c in curves) c.Stop();
    }

    /// <summary>
    /// Specifies whether the current curve has finished.
    /// </summary>
    public bool Done() => doneWithCurrentCurve;
}