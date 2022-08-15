using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovement : MonoBehaviour {

    /* Features (roughly in order of appearance)
    -  shift gravity and rotate to that direction
    -  input direction is relative to current gravity
    -  specific max movement speed
    -  different acceleration/decceleration for ground/air
    -  exact jump heigh specification
    -  different gravity for jump/falling
    -  hover at the peak of your jump
    -  clamp fall speed (faster/slower based on input)
    -  coyote time
    -  jump buffer
    -  slide down walls with a specific speed and acceleration
    -  cling to walls for a short period before leaving them
    -  walljumping to specific height and horizontal distance
    -  lock input for a short duration after respawning
    */

    #region parameters

    [Header("Running")]
    [SerializeField] private float runSpeed = 11f;
    [SerializeField] private float
        groundAccel = 16f,
        groundDeccel = 20f,
        airAccel = 12f,
        airDeccel = 20f;

    [Header("Jumping")]
    [SerializeField] private SmartCurve jumpCurve;
    [SerializeField] private float
        jumpHeight = 4f,
        fallGrav = 100f,
        maxFallSpeed = 14f,
        fastFallSpeed = 20f,
        coyoteTime = 0.1f,
        jumpBuffer = 0.1f;

    [Header("Wall Jumping")]
    [SerializeField] private SmartCurve wallJumpCurve;
    [SerializeField] private Vector2 walljumpVector = new Vector2(1.5f, 4f);
    [SerializeField] private float
        walljumpSpeed,
        walljumpNoTurnTime,
        wallClingTime = 0.09f,
        slideGrav = 5f,
        slideSpeed = 6f;

    [Header("Gravity Shifting")]
    [SerializeField] internal int maxShifts = 1;
    [SerializeField] private float
        shiftBuffer = 0.1f,
        shiftRefreshTime,
        shiftScreenFreezeDur;
    [SerializeField] private SmartCurve
        shiftRotation,
        shiftShake,
        shiftBounce;

    [Header("Other")]
    [SerializeField] private float groundCheckDist = 0.05f;
    [SerializeField] private float
        ceilingCheckDist = 0.05f,
        wallCheckDist = 0.1f;

    [Header("Sound")]
    [SerializeField] private float stepDist;
    [SerializeField] private string
        stepSound,
        landSound,
        walljumpSound;

    #endregion

    private PlayerManager m;

    // movement
    private float
        jumpBufferTimer,     // time since jump pressed
        wallClingTimer;      // time since left wall slide, but still clinging
    private int wallJumpDir; // wallDir of the current walljump
    private Vector2 vel;     // velocity adjusted for gravity
    internal UnityEvent PlayerLanded = new UnityEvent();

    // gravity shifting
    internal Vector2 gravDir = Vector2.down; // current gravity direction
    private float shiftBufferTimer,  // time since grav shift input
                  shiftRefreshTimer; // time since last grav shift
    private Vector2 shiftBufferDir;  // buffered grav shift direction
    private int shiftsRemaining;     // number of grav shifts remaining since leaving the ground
    private Quaternion shiftStart,   // start of gravity shift rotation
                       shiftEnd;     // end of gravity shift rotation
    
    // state managment
    private enum State { grounded, jumping, falling, sliding, walljumping };
    private State state = State.grounded, prevState;
    private float stateDur;
    private void ChangeState(State newState) {
        prevState = state;
        state = newState;
        stateDur = 0;
    }

    // sound
    private float stepDistRemaining;

    private void Start() {
        m = GetComponent<PlayerManager>();
        ResetBuffers();
    }
    public void ResetTo(Vector2 pos, Vector2 gravDir) {
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Vector3.forward, -gravDir));
        m.rb.velocity = Vector2.zero;
        this.gravDir = gravDir;
        shiftRotation.Stop();
        ChangeState(State.grounded);
        ResetBuffers();
    }

    private void Update() {

        // determine velocity
        vel = RotateByVector(m.rb.velocity, gravDir, true);

        // determine if touching ground or ceiling
        bool jumping = state == State.jumping || state == State.walljumping,
             onGround = BoxCheck(gravDir, groundCheckDist) == 1 && !jumping,
             headBump = BoxCheck(-gravDir, ceilingCheckDist) == 1 && jumping;

        // determine wall proximity
        Vector2 rightVector = RotateByVector(Vector2.right, gravDir);
        int wallDir = BoxCheck(rightVector, wallCheckDist) - BoxCheck(-rightVector, wallCheckDist);

        // directional input
        Vector2 rawInput    = m.input.Move,
                input       = ConfineDirections(RotateByVector(rawInput, gravDir, true));
        // jump input
        bool jumpDown       = BufferTimer(m.input.Jump.down, jumpBuffer, ref jumpBufferTimer),
             jumpReleased   = m.input.Jump.released,
        // shift input
             shiftDown          = m.input.Action.down && rawInput != Vector2.zero,
             shiftBuffered  = BufferTimer(shiftDown, shiftBuffer, ref shiftBufferTimer),
        // wall input
             xInput         = Mathf.Abs(input.x) > 0.01f,
             newWall        = !(wallJumpDir == wallDir && state == State.walljumping),
             slideInput     = Sign0(input.x) == wallDir && xInput && newWall && !onGround,
             clinging       = BufferTimer(slideInput, wallClingTime, ref wallClingTimer),
             wallJumpDown   = wallDir != 0 && jumpDown && newWall;

        // gravity shift
        if (!shiftRotation.Done()) transform.rotation = Quaternion.Slerp(shiftStart, shiftEnd, shiftRotation.Evaluate());
        m.rend.color = shiftsRemaining > 0 ? Color.white : Color.red;

        if (shiftDown) shiftBufferDir = rawInput;
        if (onGround) shiftsRemaining = maxShifts;
        if (shiftsRemaining > 0) shiftRefreshTimer += Time.deltaTime;

        if (shiftBuffered && shiftsRemaining > 0 && shiftRefreshTimer >= shiftRefreshTime) {
            shiftRefreshTimer = 0;
            shiftBufferTimer = Mathf.Infinity;
            shiftsRemaining--;

            // impacts to player
            gravDir = RemoveDiagonal(shiftBufferDir, new Vector2(Mathf.Abs(gravDir.y), Mathf.Abs(gravDir.x)));
            shiftRotation.Start();
            shiftStart = transform.rotation;
            shiftEnd = Quaternion.LookRotation(Vector3.forward, -gravDir);

            // impacts to state machine
            onGround = false;
            ChangeState(State.falling);

            // effects
            m.ScreenFreeze(shiftScreenFreezeDur);
            m.cam.Shake(shiftShake);
            m.cam.Bounce(shiftBounce, gravDir * -1);

            return;
        }

        // become grounded
        if (onGround && state != State.grounded) {
            ChangeState(State.grounded);
            PlayerLanded.Invoke();
            Audio.Play(landSound);
            stepDistRemaining = stepDist;
        }

        // state managment
        stateDur += Time.deltaTime;

        // during state
        switch (state) {

            case State.grounded:
                Run(input.x, xInput ? groundAccel : groundDeccel, runSpeed);

                if (!onGround) ChangeState(State.falling);
                else if (jumpDown) ChangeState(State.jumping);
                break;


            case State.jumping:
                Run(input.x, xInput ? airAccel : airDeccel, Mathf.Max(runSpeed, vel.x * Sign0(input.x)));

                if (jumpReleased || headBump) {
                    jumpCurve.Stop();
                    vel.y = 0;
                } else
                    vel.y = jumpCurve.Evaluate(true);

                if (wallJumpDown) ChangeState(State.walljumping);
                else if (jumpCurve.Done()) ChangeState(State.falling);

                break;


            case State.falling:
                if (prevState == State.walljumping && stateDur <= walljumpNoTurnTime && input.x == wallJumpDir)
                    input.x = 0;

                Run(input.x, xInput ? airAccel : airDeccel, Mathf.Max(runSpeed, vel.x * Sign0(input.x)));

                vel.y -= fallGrav * Time.deltaTime;

                if (wallJumpDown) ChangeState(State.walljumping);
                else if (slideInput) ChangeState(State.sliding);
                else if (stateDur <= coyoteTime && jumpDown && prevState == State.grounded) ChangeState(State.jumping);
                break;


            case State.sliding:
                vel.y -= (slideSpeed + vel.y) * slideGrav * Time.deltaTime;

                if (wallJumpDown) ChangeState(State.walljumping);
                else if (!clinging || wallDir == 0) ChangeState(State.falling);
                break;


            case State.walljumping:

                if (jumpReleased || headBump) {
                    wallJumpCurve.Stop();
                    vel.y = 0;
                } else
                    vel.y = wallJumpCurve.Evaluate(true);

                if (wallJumpDown) ChangeState(State.walljumping);
                else if (stateDur > walljumpVector.x / walljumpSpeed || wallJumpCurve.Done()) ChangeState(State.falling);
                break;
        }

        // enter state
        if (stateDur == 0) switch (state) {

            case State.jumping:
                jumpBufferTimer = Mathf.Infinity;
                jumpCurve.Start();
                jumpCurve.valueScale = jumpHeight;
                break;

            case State.sliding:
                vel.x = 0;
                break;

            case State.walljumping:
                Audio.Play(walljumpSound);
                jumpBufferTimer = Mathf.Infinity;
                wallJumpDir = wallDir;;
                wallJumpCurve.Start();
                wallJumpCurve.valueScale = walljumpVector.y;
                vel.x =walljumpSpeed * -wallDir;
                break;
        }

        // debug
        m.DebugText("State: " + state);
        m.DebugText("Velocity: " + vel);

        // step sounds
        if (!xInput) stepDistRemaining = 0;
        if (onGround && xInput) {
            stepDistRemaining -= Mathf.Abs(vel.x) * Time.deltaTime;

            if (stepDistRemaining <= 0) {
                stepDistRemaining = stepDist;
                Audio.Play(stepSound);
            }
        }

        // clamp velocitty
        vel.y = Mathf.Max(vel.y, -(input.y < 0 ? fastFallSpeed : maxFallSpeed));

        // applying gravity adjusted velocity
        m.rb.velocity = RotateByVector(vel, gravDir);
    }

    private void Run(float dir, float accel, float maxSpeed) => vel.x += (Sign0(dir) * maxSpeed - vel.x) * accel * Time.deltaTime; // thank you https://pastebin.com/Dju3wz6J

    // timer functions
    private bool BufferTimer(bool reset, float time, ref float timer) {
        timer = reset ? 0 : (timer + Time.deltaTime);
        return timer <= time;
    }
    private void ResetBuffers() => jumpBufferTimer = shiftBufferTimer = Mathf.Infinity;

    // math functions
    /// <summary>
    /// Returns the sign of i, and 0 if i == 0.
    /// </summary>
    private int Sign0(float i) => i > 0 ? 1 : i < 0 ? -1 : 0;
    /// <summary>
    /// Rotates vector v by vector r. If inverse, rotates the opposite direction.
    /// </summary>
    private Vector2 RotateByVector(Vector2 v, Vector2 r, bool inverse = false) {
        float a = Mathf.Atan2(r.y, r.x) + Mathf.PI / 2;
        if (inverse) a = Mathf.PI * 2 - a;
        float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
    /// <summary>
    /// Returns vector v but bound to the closest up/down/left/right direction.
    /// </summary>
    /// <param name="favor">
    /// Axis to favor if perfectly between two direction
    /// </param>
    private Vector2 RemoveDiagonal(Vector2 v, Vector2 favor) {
        float x = v.x * v.x, y = v.y * v.y;
        return (v * (x == y ? favor : x > y ? Vector2.right : Vector2.up)).normalized;
    }
    /// <summary>
    /// Confines vector v to 8 direcitons.
    /// </summary>
    private Vector2 ConfineDirections(Vector2 v) => new Vector2(Sign0(Mathf.Round(v.x)), Sign0(Mathf.Round(v.y)));

    // boxcast functions
    private int BoxCheck(Vector2 dir, float dist) {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, m.col.size * transform.localScale, transform.eulerAngles.z, dir, dist, m.groundMask);
        return hit.normal == dir * -1 ? 1 : 0;
    }
}
