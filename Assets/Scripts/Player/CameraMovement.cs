using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

    /* Camera Features
    TRANSITION  : transitions between rooms with an animation curve
    TRACK       : tracks the player in larger rooms, and stops at room walls
    TRACK BOX   : an invisble box that the player must push for the camera tracking to move
    SHAKE       : camera shakes randomly for a duration and amplitude controlled by animation curve
    BOUNCE      : camera bounces back and forth on a specific axis for a duration and amplitude controlled by animation curve
    SHAKE COVER : a black cover will appear when the camera is shaking to ensure other rooms remain unseen
    */

    [SerializeField] private float trackSpeed;
    [SerializeField] internal Vector2 trackBoxExtents;
    [SerializeField] private GameObject screenShakeCover;
    [SerializeField] private SmartCurve transitionCurve;

    // transition
    private Vector2 transitionStart, transitionPos;

    // tracking
    private bool centering;
    private Vector2 trackVel, trackPos, lowerTrackBound, upperTrackBound;

    // shake
    private List<SmartCurve> shakes = new List<SmartCurve>();
    private Vector2 shakePos;

    // bounce
    private SmartCurve bounceCurve = new SmartCurve();
    private Vector2 bouncePos, bounceDir;

    private void Start() {
        PlayerManager.rooms.RoomChange.AddListener(ChangeRoom);

        RoomManager.instance.CheckRooms(PlayerMovement.respawmInfo);
        transitionCurve.Stop();

        TransitionManager.ResetLevel += () => {
            RoomManager.instance.CheckRooms(PlayerMovement.respawmInfo);
            transitionCurve.Stop();
        };

        //if (startRoom != null) {
        //    transitionPos = trackPos = lowerTrackBound = startRoom.pos * m.rooms.roomSize;
        //    upperTrackBound = startRoom.UpperBound() * m.rooms.roomSize;
        //}
    }

    private void Update() {

        // transition
        float transitionPercent = transitionCurve.Done() ? 1f : transitionCurve.Evaluate();
        transitionPos = Vector2.LerpUnclamped(transitionStart, trackPos, transitionPercent);

        // camera effects
        if (SettingsManager.settings.cameraShake) {

            // shaking
            shakePos = Vector2.zero;
            if (shakes.Count != 0) {
                shakes.RemoveAll(s => s.Done());
                float amp = 0;
                foreach (SmartCurve s in shakes) amp = Mathf.Max(s.Evaluate(), amp);
                shakePos = RandomVector(amp);
            }

            // bouncing
            bouncePos = bounceCurve.Done() ? Vector2.zero : bounceDir * bounceCurve.Evaluate();
        }
        else shakePos = bouncePos = Vector2.zero;

        // shake cover
        screenShakeCover.SetActive(shakePos + bouncePos != Vector2.zero || transitionPercent > 1f);
        screenShakeCover.transform.position = transitionPercent > 1f ? trackPos : transitionPos;

        UpdatePosition();
    }

    private void FixedUpdate() {
        // tracking
        Vector2 pushbox = PushBox(PlayerManager.instance.transform.position, trackPos, trackBoxExtents);
        Vector2 smooth = Vector2.SmoothDamp(trackPos, pushbox, ref trackVel, trackSpeed, Mathf.Infinity, Time.fixedDeltaTime);
        Vector2 clamp = VectorClamp(smooth, lowerTrackBound, upperTrackBound);
        trackPos = clamp;

        UpdatePosition();
    }

    private void UpdatePosition() {
        transform.position = (Vector3)(transitionPos + shakePos + bouncePos) + Vector3.back * 10;
    }

    public void ChangeRoom(Room r) {
        transitionStart = transitionPos;
        transitionCurve.Start();

        lowerTrackBound = r.pos * PlayerManager.rooms.roomSize;
        upperTrackBound = r.UpperBound() * PlayerManager.rooms.roomSize;
    }

    public void Shake(SmartCurve shake) => shakes.Add(shake.Copy());
    public void Bounce(SmartCurve bounce, Vector2 dir) {
        bounceCurve = bounce.Copy();
        bounceDir = dir;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position, trackBoxExtents * 2);
    }

    // math functions
    private Vector2 TriggerPushBox(Vector2 c, Vector2 p, Vector2 b) => new Vector2(Mathf.Abs(c.x - p.x) > b.x ? 1 : 0,
                                                                                   Mathf.Abs(c.y - p.y) > b.y ? 1 : 0);
    private Vector2 PushBox(Vector2 c, Vector2 p, Vector2 b) => new Vector2(c.x > p.x + b.x ? c.x - b.x : c.x < p.x - b.x ? c.x + b.x : p.x,
                                                                            c.y > p.y + b.y ? c.y - b.y : c.y < p.y - b.y ? c.y + b.y : p.y);
    private Vector2 RandomVector(float r) => new Vector2(Random.Range(-r, r), Random.Range(-r, r));
    private Vector2 VectorClamp(Vector2 v, Vector2 b1, Vector2 b2) => new Vector2(Mathf.Clamp(v.x, b1.x, b2.x), Mathf.Clamp(v.y, b1.y, b2.y));
}
