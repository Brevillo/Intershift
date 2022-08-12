using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OldPlayerMovement : MonoBehaviour {

    ///* Features (roughly in order of appearance)
    //-  shift gravity and rotate to that direction
    //-  input direction is relative to current gravity
    //-  specific max movement speed
    //-  different acceleration/decceleration for ground/air
    //-  exact jump heigh specification
    //-  different gravity for jump/falling
    //-  hover at the peak of your jump
    //-  clamp fall speed (faster/slower based on input)
    //-  coyote time
    //-  jump buffer
    //-  slide down walls with a specific speed and acceleration
    //-  cling to walls for a short period before leaving them
    //-  walljumping to specific height and horizontal distance
    //-  how close you need to be to a wall to wall jump varies with how fast you're moving
    //-  lock input for a short duration after respawning
    //*/

    //private PlayerManager m;

    //#region parameters

    //[Header("Running")]
    //[SerializeField] private float maxMoveSpeed = 11;
    //[SerializeField] private float groundAccel = 16,
    //                               groundDeccel = 20,
    //                               airAccel = 12,
    //                               airDeccel = 20;

    //[Header("Running")]
    //[SerializeField] private float jumpHeight = 4;
    //[SerializeField] private float jumpGrav = 65,
    //                               fallGrav = 80,
    //                               peakTime = 0.03f,
    //                               maxFallSpeed = 14,
    //                               fastFallSpeed = 20,
    //                               slowFallSpeed,
    //                               coyoteTime = 0.1f,
    //                               jumpBuffer = 0.1f;

    //[Header("Wall Jumping")]
    //[SerializeField] private Vector2 wallJump = new Vector2(1.5f, 4);
    //[SerializeField] private float wallJumpHorizontalBoost,
    //                               wallJumpSpeedKeepTime,
    //                               wallClingTime = 0.09f,
    //                               slideGrav = 5,
    //                               slideSpeed = 6;

    //[Header("Gravity Shifting")]
    //[SerializeField] private float shiftBuffer = 0.1f;
    //[SerializeField] private SmartCurve shiftRotation, shiftShake, shiftBounce;

    //[Header("Other")]
    //[SerializeField] private float groundCheckDist = 0.05f;
    //[SerializeField] private float minWallCheckDist = 0.1f,
    //                               maxWallCheckDist = 0.3f;

    //#endregion

    //// movement
    //private float coyoteTimer,  // time since left ground
    //    jumpBufferTimer,        // time since jump pressed
    //    peakTimer,              // time since jump peak started
    //    wallClingTimer,         // time since tried to leave wall cling
    //    wallJumpKbTimer,        // time since wall jumped
    //    wallJumpSpeedKeepTimer, // time since started wall cling
    //    wallJumpSpeedKeep,      // 
    //    gravStrength,           // current strength of gravity
    //    currentMaxFallSpeed;    // current maximum falling speed
    //private bool jumping;       // moving upwards due to jumping?

    //// gravity shifting
    //private float shiftBufferTimer;             // time since grav shift input
    //private Vector2 shiftBufferDir;             // buffered grav shift direction
    //private int maxShifts = 1, shiftsRemaining; // max number of grav shifts before landing, number of grav shifts remaining since leaving the ground
    //internal Vector2 gravDir;                   // current gravity direction
    //private Quaternion shiftStartRotation,      // start of gravity shift rotation
    //                   shiftEndRotation;        // end of gravity shift rotation

    //private void Start() {
    //    m = GetComponent<PlayerManager>();
    //    gravDir = Vector2.down;
    //    ResetTimers();
    //}

    //private void Update() {

    //    // renaming all input for easier access
    //    Vector2 input = m.input.Move.normalized;
    //    bool jumpDown = m.input.Jump.down,
    //     jumpReleased = m.input.Jump.released,
    //            shift = m.input.Action.down;

    //    // get velocity relative to the current gravity
    //    Vector2 vel = RotateByVector(m.rb.velocity, gravDir, true);

    //    // ground check
    //    bool grounded = BoxCheck(gravDir, groundCheckDist) == 1 && Mathf.Round(vel.y) <= 0;

    //    #region gravity shifting
    //    // gravity shifting
    //    bool shiftInput    = shift && input != Vector2.zero,
    //         shiftBuffered = TimerCountReset(ref shiftBufferTimer, shiftBuffer, shiftInput);
    //    if (shiftInput) shiftBufferDir = input;
    //    if (grounded) shiftsRemaining = maxShifts;

    //    if (shiftBuffered && shiftsRemaining > 0) {
    //        shiftBufferTimer = Mathf.Infinity;
    //        gravDir = RemoveDiagonal(shiftBufferDir, new Vector2(Mathf.Abs(gravDir.y), Mathf.Abs(gravDir.x)));
    //        shiftsRemaining--;

    //        // effects
    //        m.cam.Shake(shiftShake);
    //        m.cam.Bounce(shiftBounce, gravDir * -1);

    //        // player rotation
    //        shiftRotation.Start();
    //        shiftStartRotation = transform.rotation;
    //        shiftEndRotation = Quaternion.LookRotation(Vector3.forward, -gravDir);

    //        return; // so you can't shift and jump on the same frame
    //    }
    //    if (!shiftRotation.Done()) transform.rotation = Quaternion.Slerp(shiftStartRotation, shiftEndRotation, shiftRotation.Evaluate());
    //    m.rend.color = shiftsRemaining > 0 ? Color.white : Color.red;
    //    #endregion

    //    // adjust input for current gravity
    //    input = RoundVector(RotateByVector(input, gravDir, true));
        
    //    // running
    //    bool wallJumpKb = TimerCountReset(ref wallJumpKbTimer, 0, Mathf.Abs(vel.x) < maxMoveSpeed);
    //    if (!wallJumpKb && wallClingTimer > 0) {
    //        float accel = input.x != 0 ? grounded ? groundAccel  : airAccel
    //                                   : grounded ? groundDeccel : airDeccel;
    //        float maxSpeed = Mathf.Max(maxMoveSpeed, vel.x * input.x); // maintain greater speed if you move in that direction
    //        vel.x += (input.x * maxSpeed - vel.x) * accel * Time.deltaTime; // thank you https://pastebin.com/Dju3wz6J
    //    }

    //    // wall check
    //    Vector2 right = RotateByVector(Vector2.right, gravDir);
    //    float wallCheckDist = minWallCheckDist + (maxWallCheckDist - minWallCheckDist) * (Mathf.Abs(vel.x) / maxMoveSpeed),
    //          wallDir = BoxCheck(right, wallCheckDist) - BoxCheck(-right, wallCheckDist);

    //    // jumping
    //    bool jumpBuffered  = TimerCountReset(ref jumpBufferTimer, jumpBuffer, jumpDown),
    //         canGroundJump = TimerCountReset(ref coyoteTimer, coyoteTime, grounded),
    //         canWallJump   = wallDir != 0 && !grounded,
    //         canKeepSpeed  = TimerCountReset(ref wallJumpSpeedKeepTimer, wallJumpSpeedKeepTime, canWallJump);

    //    wallJumpSpeedKeep = Sign0(vel.x) != Sign0(wallJumpSpeedKeep) ? maxMoveSpeed : GreaterAbs(wallJumpSpeedKeep, vel.x);
    //    m.text.text = wallJumpSpeedKeep.ToString();

    //    if (jumpBuffered && (canGroundJump || canWallJump)) {
    //        jumpBufferTimer = coyoteTimer = Mathf.Infinity;
    //        jumping = true;
    //        vel.y = Mathf.Sqrt(2 * (canGroundJump ? jumpHeight : wallJump.y) * jumpGrav);

    //        if (canWallJump) {
    //            wallJumpKbTimer = wallJump.x / maxMoveSpeed;
    //            float moveSpeed = Mathf.Max(maxMoveSpeed, canKeepSpeed ? wallJumpSpeedKeep : 0);
    //            vel.x = moveSpeed * -wallDir;
    //            wallJumpSpeedKeep = 0;
    //        }
    //    }

    //    // variable jump height
    //    if (jumpReleased && vel.y > 0) vel.y = 0;

    //    // gravity
    //    bool sliding  = wallDir == Mathf.Sign(input.x) && input.x != 0 && !grounded,
    //         peaking  = TimerCountReset(ref peakTimer, peakTime, jumping && vel.y <= 0),
    //         clinging = TimerCountReset(ref wallClingTimer, wallClingTime, sliding) && wallDir != 0 && !wallJumpKb;

    //    if (clinging) { // wall sliding
    //        gravStrength = 0;
    //        vel.y += (-slideSpeed - vel.y) * slideGrav * Time.deltaTime;
    //    } else if (peaking) { // peaked
    //        jumping = false;
    //        gravStrength = vel.y = 0;
    //    } else // regular
    //        gravStrength = jumping ? jumpGrav : fallGrav;

    //    // max fall speed
    //    currentMaxFallSpeed = input.y < 0 ? fastFallSpeed
    //                        : input.y > 0 ? slowFallSpeed
    //                        : maxFallSpeed;

    //    // applying gravity adjusted velocity
    //    m.rb.velocity = RotateByVector(vel, gravDir);
    //}

    //private void FixedUpdate() {
    //    // apply gravity
    //    m.rb.AddForce(gravDir.normalized * gravStrength, ForceMode2D.Force);

    //    // clamp fall speed
    //    Vector2 vel = RotateByVector(m.rb.velocity, gravDir, true);
    //    vel.y = Mathf.Max(vel.y, -currentMaxFallSpeed);
    //    m.rb.velocity = RotateByVector(vel, gravDir);
    //}

    //public void ResetTo(Vector2 pos) {
    //    transform.SetPositionAndRotation(pos, Quaternion.identity);
    //    m.rb.velocity = Vector2.zero;

    //    gravDir = Vector2.down;
    //    shiftRotation.Stop();
    //    ResetTimers();
    //}

    //// timer functions
    //private bool TimerCountReset(ref float timer, float time, bool reset) {
    //    // if time is 0, function assumes the timer is a countdown instead of stopwatch
    //    int dir = Sign0(time) * 2 - 1;
    //    timer = reset ? 0 : (timer + Time.deltaTime * dir);
    //    return dir < 0 ? timer > 0 : timer <= time;
    //}
    //private void ResetTimers() {
    //    coyoteTimer = jumpBufferTimer = shiftBufferTimer = peakTimer = wallClingTimer = Mathf.Infinity;
    //    wallJumpKbTimer = 0;
    //}

    //// math functions
    //private int Sign0(float i) => i > 0 ? 1 : i < 0 ? -1 : 0;
    //private float GreaterAbs(float i1, float i2) => Mathf.Abs(i1) > Mathf.Abs(i2) ? i1 : i2;
    //private Vector2 RotateByVector(Vector2 v, Vector2 r, bool inverse = false) {
    //    float a = Mathf.Atan2(r.y, r.x) + Mathf.PI / 2;
    //    if (inverse) a = Mathf.PI * 2 - a;
    //    float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
    //    return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    //}
    //private Vector2 RemoveDiagonal(Vector2 i, Vector2 favor) {
    //    float x = i.x * i.x, y = i.y * i.y;
    //    return (i * (x == y ? favor : x > y ? Vector2.right : Vector2.up)).normalized;
    //}
    //private Vector2 RoundVector(Vector2 i) => new Vector2(Mathf.Round(i.x), Mathf.Round(i.y));

    //// boxcast functions
    //private int BoxCheck(Vector2 dir, float dist) {
    //    RaycastHit2D hit = BoxCastDraw(transform.position, m.col.size * transform.localScale, transform.eulerAngles.z, dir, dist, m.groundMask);
    //    return hit.normal == dir * -1 ? 1 : 0;
    //}
    //private RaycastHit2D BoxCastDraw(Vector2 start, Vector2 size, float angle, Vector2 dir, float dist, LayerMask mask) {

    //    float w = size.x / 2, h = size.y / 2;
    //    Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
    //    Vector2 rDist = dir.normalized * dist,
    //            p1 = q * new Vector2(-w,  h) + (Vector3)start,
    //            p2 = q * new Vector2( w,  h) + (Vector3)start,
    //            p3 = q * new Vector2( w, -h) + (Vector3)start,
    //            p4 = q * new Vector2(-w, -h) + (Vector3)start,
    //            p5 = p1 + rDist, p6 = p2 + rDist,
    //            p7 = p3 + rDist, p8 = p4 + rDist;

    //    RaycastHit2D hit = Physics2D.BoxCast(start, size, angle, dir, dist, mask);
    //    Color c = hit ? Color.red : Color.green;
    //    Debug.DrawLine(p1, p2, c); Debug.DrawLine(p2, p3, c);
    //    Debug.DrawLine(p3, p4, c); Debug.DrawLine(p4, p1, c);
    //    Debug.DrawLine(p5, p6, c); Debug.DrawLine(p6, p7, c);
    //    Debug.DrawLine(p7, p8, c); Debug.DrawLine(p8, p5, c);
    //    c = Color.grey;
    //    Debug.DrawLine(p1, p5, c); Debug.DrawLine(p2, p6, c);
    //    Debug.DrawLine(p3, p7, c); Debug.DrawLine(p4, p8, c);

    //    if (hit) Debug.DrawRay(hit.point, hit.normal.normalized, Color.yellow);
    //    return hit;
    //}
}
