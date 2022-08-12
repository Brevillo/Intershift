using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

    [SerializeField] private float trackSpeed;
    [SerializeField] private GameObject screenShakeCover;
    [SerializeField] private SmartCurve bouncyLerpCurve, bouncelessLerpCurve;

    private Transform track;
    private Vector2 trackPos, lowerLeftTrackBound, upperRightTrackBound;

    private Vector2 currentRoom, transitionStart, transitionDir, roomPos;
    private float transitionMag;

    private SmartCurve bounceCurve = new SmartCurve(), lerpCurve = new SmartCurve();
    private Vector2 bouncePos, bounceDir;

    private List<SmartCurve> shakes = new List<SmartCurve>();
    private Vector2 shakePos;
    private System.Predicate<SmartCurve> shakeDone = s => s.Done();

    private void Start() {
        roomPos = transform.position;
        track = FindObjectOfType<PlayerManager>().transform;
    }


    private void Update() {

        // transition
        roomPos = transitionStart + transitionMag * lerpCurve.Evaluate() * transitionDir;

        // camera effects
        if (Settings.i.cameraShake) {

            // shaking
            shakePos = Vector2.zero;
            if (shakes.Count != 0) {
                shakes.RemoveAll(shakeDone);
                float amp = 0;
                foreach (SmartCurve s in shakes) amp = Mathf.Max(s.Evaluate(), amp);
                shakePos = RandomVector(amp);
            }

            // bouncing
            bouncePos = bounceDir * bounceCurve.Evaluate();
        }
        else shakePos = bouncePos = Vector2.zero;

        // shake cover
        screenShakeCover.SetActive(shakePos != Vector2.zero || bouncePos.sqrMagnitude > 0.001f);
        screenShakeCover.transform.position = roomPos + trackPos;

        // set position
        transform.position = (Vector3)(roomPos + trackPos + shakePos + bouncePos) + Vector3.back * 10;
    }

    private void FixedUpdate() {
        // tracking
        trackPos = VectorClamp(Vector2.Lerp(trackPos, (Vector2)track.position - currentRoom, trackSpeed), lowerLeftTrackBound - currentRoom, upperRightTrackBound - currentRoom);
    }

    public void TrackWithin(Vector2 lowerBound, Vector2 upperBound) {
        lowerLeftTrackBound = lowerBound;
        upperRightTrackBound = upperBound;
    }

    public void ChangeRoom(Vector2 newRoom) {
        if (newRoom == currentRoom) return;

        currentRoom = newRoom;
        lerpCurve = Settings.i.cameraBounce ? bouncyLerpCurve : bouncelessLerpCurve;
        lerpCurve.Start();

        transitionStart = transform.position;
        Vector2 lerpVector = newRoom - transitionStart;
        transitionDir = lerpVector.normalized;
        transitionMag = lerpVector.magnitude;
    }

    public void Shake(SmartCurve shake) {
        shakes.Add(shake.Copy());
    }

    public void Bounce(SmartCurve bounce, Vector2 dir) {
        bounceCurve = bounce.Copy();
        bounceDir = dir;
    }

    private Vector2 RandomVector(float r) => new Vector2(Random.Range(-r, r), Random.Range(-r, r));
    private Vector2 VectorClamp(Vector2 v, Vector2 b1, Vector2 b2) => new Vector2(Mathf.Clamp(v.x, b1.x, b2.x), Mathf.Clamp(v.y, b1.y, b2.y));
}
