using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pendulum : MonoBehaviour {

    private LineRenderer line;
    private Rigidbody2D rb;

    [SerializeField] private Vector2[] points;
    [SerializeField] private int startPoint;
    [SerializeField] private float shiftVel;
    [SerializeField] private SmartCurve shiftCurve;

    private Vector2 startPos;
    private float currentGravDir;
    private int oldPoint, nextPoint;

    private void Start() {
        rb = GetComponent<Rigidbody2D>();

        startPos = transform.position;
        oldPoint = nextPoint = startPoint;

        SetLine();

        currentGravDir = PlayerManager.movement.gravDir;
    }

    private void OnValidate() {
        startPos = transform.position;
        SetLine();
    }

    private void SetLine() {
        line = GetComponent<LineRenderer>();
        line.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++) line.SetPosition(i, points[i] + startPos);
    }

    private void Update() {

        float newGravDir = PlayerManager.movement.gravDir;

        if (currentGravDir != newGravDir) {
            oldPoint = nextPoint;

            Vector2 vectorGrav = newGravDir.DegToVector();

            float prevDot = oldPoint > 0 ?
                    Vector2.Dot(vectorGrav, (points[oldPoint - 1] - points[oldPoint]).normalized) : 0,
                  nextDot = oldPoint + 1 < points.Length ?
                    Vector2.Dot(vectorGrav, (points[oldPoint + 1] - points[oldPoint]).normalized) : 0;

            bool prev = prevDot > 0, next = nextDot > 0;
            if (next && prev) nextPoint = oldPoint + (nextDot > prevDot ? 1 : -1);
            else if (next) nextPoint = oldPoint + 1;
            else if (prev) nextPoint = oldPoint - 1;

            if (oldPoint != nextPoint) {
                shiftCurve.Start();
                shiftCurve.timeScale = (points[nextPoint] - points[oldPoint]).magnitude / shiftVel;
            }
        }
        currentGravDir = newGravDir;

        float curve = shiftCurve.Evaluate();
        Vector2 p1 = points[oldPoint],
                p2 = points[nextPoint],
                lerp = Vector2.LerpUnclamped(p1, p2, curve);
        rb.MovePosition(lerp + startPos);
    }
}
