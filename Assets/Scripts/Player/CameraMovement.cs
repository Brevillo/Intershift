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
    FREEZE      : screen will freeze for an amount of time
    */
    [SerializeField] private float trackSpeed;
    [SerializeField] internal Vector2 trackBoxExtents;
    [SerializeField] private RectTransform screenShakeCover;
    [SerializeField] private SmartCurve transitionCurve, sizeCurve;
    [SerializeField] private Camera cam;

    // transition
    private Vector2 transitionStart, transitionPos;
    private float sizeStart = 1f, sizeEnd = 1f;

    // tracking
    private Vector2 trackVel, trackPos, trackCenter, roomSize;

    // shake
    private List<SmartCurve> shakes = new List<SmartCurve>();
    private Vector2 shakePos;

    // bounce
    private SmartCurve bounceCurve = new SmartCurve();
    private Vector2 bouncePos, bounceDir;

    private void Start() {

        PlayerManager.rooms.RoomChange += ChangeRoom;
        TransitionManager.ResetLevel += ResetLevel;

        RoomManager.instance.CheckRooms(PlayerManager.transform.position);

        ResetLevel();
    }

    private void ResetLevel() {
        transitionCurve.Stop();
        sizeCurve.Stop();
        bounceCurve.Stop();

        RoomManager.instance.CheckRooms(PlayerMovement.respawnInfo);
        shakePos = bouncePos = trackVel = Vector2.zero;
        transitionPos = trackPos = VectorClamp(PlayerManager.transform.position, trackCenter, roomSize);
    }

    private void Update() {

        // camera effects
        if (SettingsManager.settings.cameraShake) {

            // shaking
            shakePos = Vector2.zero;
            if (shakes.Count != 0) {

                // remove all finished shake curves
                shakes.RemoveAll(s => s.Done());

                // find largest amplitude curve
                float amp = 0;
                foreach (SmartCurve s in shakes) amp = Mathf.Max(s.Evaluate(), amp);
                shakePos = Random.insideUnitCircle * amp;
            }

            // bouncing
            bouncePos = bounceCurve.Done() ? Vector2.zero : bounceDir * bounceCurve.Evaluate();
        }
        else shakePos = bouncePos = Vector2.zero;
    }

    private void FixedUpdate() {

        // size transition
        float currentSize = Mathf.LerpUnclamped(sizeStart, sizeEnd, sizeCurve.Done() ? 1f : sizeCurve.Evaluate());
        cam.orthographicSize = RoomManager.roomSize.y / 2 * currentSize;

        // tracking
        Vector2 pushbox = PushBox(PlayerManager.transform.position, trackPos, trackBoxExtents),
                smooth = Vector2.SmoothDamp(trackPos, pushbox, ref trackVel, trackSpeed, Mathf.Infinity, Time.fixedDeltaTime),
                trackBounds = (roomSize - Vector2.one * sizeEnd) / 2f * RoomManager.roomSize,
                clamp = VectorClamp(smooth, trackCenter - trackBounds, trackCenter + trackBounds);
        trackPos = clamp;

        // transition
        float transitionPercent = transitionCurve.Done() ? 1f : transitionCurve.Evaluate();
        transitionPos = Vector2.LerpUnclamped(transitionStart, trackPos, transitionPercent);

        // set camera position
        transform.position = (Vector3)(transitionPos + shakePos + bouncePos) + Vector3.back * 10;

        // shake cover
        screenShakeCover.gameObject.SetActive(shakePos + bouncePos != Vector2.zero || transitionPercent > 1f);
        screenShakeCover.position = transitionPercent > 1f ? trackPos : transitionPos;
        screenShakeCover.sizeDelta = RoomManager.roomSize * currentSize;
    }

    public void ChangeRoom(Room r) {
        transitionStart = transitionPos;
        transitionCurve.Start();

        sizeStart = sizeEnd;
        sizeEnd = r.cameraSize;
        sizeCurve.Start();

        trackCenter = r.center * RoomManager.roomSize;
        roomSize = r.size;
    }

    public void Snap(Room r, int respawn) {
        Vector2 trackCenter = r.center * RoomManager.roomSize,
                trackBounds = (r.size - Vector2.one * r.cameraSize) / 2f * RoomManager.roomSize;

        transform.position = (Vector3)VectorClamp(r.respawnPoints[respawn].worldPos, trackCenter - trackBounds, trackCenter + trackBounds) + Vector3.back * 10f;
        cam.orthographicSize = RoomManager.roomSize.y / 2f * r.cameraSize;
    }

    public void Shake(SmartCurve shake) => shakes.Add(shake.Copy());
    public void Bounce(SmartCurve bounce, Vector2 dir) {
        bounceCurve = bounce.Copy();
        bounceDir = dir;
    }
    public void ScreenFreeze(float dur) => StartCoroutine(ScreenFreezeCoroutine(dur));
    private IEnumerator ScreenFreezeCoroutine(float dur) {
        float ogTimeScale = Time.timeScale;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(dur);
        Time.timeScale = ogTimeScale;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position, trackBoxExtents * 2);
    }

    // math functions
    private bool TriggerPushBox(Vector2 c, Vector2 p, Vector2 b) => Mathf.Abs(c.x - p.x) > b.x && Mathf.Abs(c.y - p.y) > b.y;
    private Vector2 PushBox(Vector2 c, Vector2 p, Vector2 b) => new Vector2(c.x > p.x + b.x ? c.x - b.x : c.x < p.x - b.x ? c.x + b.x : p.x,
                                                                            c.y > p.y + b.y ? c.y - b.y : c.y < p.y - b.y ? c.y + b.y : p.y);
    private Vector2 VectorClamp(Vector2 v, Vector2 b1, Vector2 b2) => new Vector2(Mathf.Clamp(v.x, b1.x, b2.x), Mathf.Clamp(v.y, b1.y, b2.y));
}
