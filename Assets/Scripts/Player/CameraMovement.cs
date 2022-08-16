using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

    /* Camera Features
    TRANSITION  : transitions between rooms with an animation curve
    TRACK       : tracks the player in larger rooms, and stops at room walls
    TRACK BOX   : an invisble box that the player must push for the camera tracking to mvove
    SHAKE       : camera shakes randomly for a duration and amplitude controlled by animation curve
    BOUNCE      : camera bounces back and forth on a specific axis for a duration and amplitude controlled by animation curve
    SHAKE COVER : a black cover will appear when the camera is shaking to ensure other rooms remain unseen
    */

    [SerializeField] private float trackSpeed;
    [SerializeField] internal Vector2 trackBoxExtents;
    [SerializeField] private GameObject screenShakeCover;
    [SerializeField] private SmartCurve bouncyLerpCurve, bouncelessLerpCurve;

    private Transform track;
    private Vector2 trackPos, roomPos, lowerLeftTrackBound, upperRightTrackBound;

    private Vector2 transitionStart, transitionPos;

    private SmartCurve bounceCurve = new SmartCurve(), lerpCurve = new SmartCurve();
    private Vector2 bouncePos, bounceDir;

    private List<SmartCurve> shakes = new List<SmartCurve>();
    private Vector2 shakePos;
    private System.Predicate<SmartCurve> shakeDone = s => s.Done();

    private PlayerManager m;

    private void Start() {
        m = FindObjectOfType<PlayerManager>();

        transitionPos = transform.position;
        track = FindObjectOfType<PlayerManager>().transform;

        m.rooms.RoomChange.AddListener(ChangeRoom);
        //trackPos = m.rooms.CheckRooms().;
    }

    private void Update() {

        // transition
        transitionPos = Vector2.LerpUnclamped(transitionStart, trackPos, lerpCurve.Done() ? 1 : lerpCurve.Evaluate());

        // camera effects
        if (Settings.instance.cameraShake) {

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

        // set position
        transform.position = (Vector3)(transitionPos + shakePos + bouncePos) + Vector3.back * 10;

        // shake cover
        screenShakeCover.SetActive(shakePos != Vector2.zero || bouncePos.sqrMagnitude > 0.001f);
        screenShakeCover.transform.position = (Vector2)transform.position - shakePos - bouncePos;
    }

    private void FixedUpdate() {
        // tracking
        trackPos = VectorClamp(Vector2.Lerp(trackPos, PushBox(track.position, trackPos, trackBoxExtents), trackSpeed), lowerLeftTrackBound, upperRightTrackBound);
    }

    public void ChangeRoom(Room r) {

        lerpCurve = Settings.instance.bouncyCameraTransitions ? bouncyLerpCurve : bouncelessLerpCurve;
        lerpCurve.Start();

        roomPos = r.pos * m.rooms.roomSize;

        lowerLeftTrackBound = roomPos;
        upperRightTrackBound = (r.pos + r.size - Vector2.one) * m.rooms.roomSize;

        transitionStart = transform.position;
    }

    public void Shake(SmartCurve shake) {
        shakes.Add(shake.Copy());
    }

    public void Bounce(SmartCurve bounce, Vector2 dir) {
        bounceCurve = bounce.Copy();
        bounceDir = dir;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position, trackBoxExtents * 2);
    }

    // math functions
    private Vector2 PushBox(Vector2 c, Vector2 p, Vector2 b) => new Vector2(c.x > p.x + b.x ? c.x - b.x : c.x < p.x - b.x ? c.x + b.x : p.x,
                                                                            c.y > p.y + b.y ? c.y - b.y : c.y < p.y - b.y ? c.y + b.y : p.y);
    private Vector2 RandomVector(float r) => new Vector2(Random.Range(-r, r), Random.Range(-r, r));
    private Vector2 VectorClamp(Vector2 v, Vector2 b1, Vector2 b2) => new Vector2(Mathf.Clamp(v.x, b1.x, b2.x), Mathf.Clamp(v.y, b1.y, b2.y));
}
