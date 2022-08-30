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

    #region private variables

    public static Vector3 respawmInfo = Vector3.forward * 270f;

    // movement
    internal bool lockMovement;
    private float
        jumpBufferTimer,     // time since jump pressed
        wallClingTimer;      // time since left wall slide, but still clinging
    private int wallJumpDir; // wallDir of the current walljump
    private Vector2 vel;     // velocity adjusted for gravity
    internal bool onGround;  // is the player on the ground?

    // gravity shifting
    internal float gravDir = 270;    // current gravity direction
    private float shiftBufferTimer,  // time since grav shift input
                  shiftRefreshTimer, // time since last grav shift
                  shiftBufferDir;    // buffered grav shift direction
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

    #endregion

    private void Start() {
        ResetBuffers();
        ResetTo(respawmInfo);

        TransitionManager.ResetLevel += () => {
            ResetTo(respawmInfo, respawmInfo.z);
            ResetBuffers();
        };
    }

    public void ResetTo(Vector3 v) => ResetTo(v, v.z);
    public void ResetTo(Vector2 pos, float newGravDir) {
        transform.SetPositionAndRotation(pos + Vector2.up * PlayerManager.col.bounds.extents.y, Quaternion.Euler(0, 0, newGravDir + 90));

        PlayerManager.rb.velocity = Vector2.zero;
        gravDir = newGravDir;
        ChangeState(State.grounded);

        shiftRotation.Stop();
        ResetBuffers();
    }

    private void Update() {

        // determine velocity
        vel = RotateVector(PlayerManager.rb.velocity, gravDir, true);

        Vector2 gravVector = gravDir.DegToVector();

        // determine if touching ground or ceiling
        bool jumping = state == State.jumping || state == State.walljumping,
             headBump = BoxCheck(-gravVector, ceilingCheckDist) == 1 && jumping;
             onGround = BoxCheck(gravVector, groundCheckDist) == 1 && !jumping;

        // determine wall proximity
        Vector2 rightVector = RotateVector(Vector2.right, gravDir);
        int wallDir = BoxCheck(rightVector, wallCheckDist) - BoxCheck(-rightVector, wallCheckDist);

        // outer input
        Vector2 rawInput       = lockMovement ? Vector2.zero : PlayerManager.input.Move;
        bool inputJumpDown     = PlayerManager.input.Jump.down && !lockMovement,
             inputJumpReleased = PlayerManager.input.Jump.released && !lockMovement,
             inputActionDown   = PlayerManager.input.Action.down && !lockMovement;

        // directional input
        Vector2 input       = ConfineDirections(RotateVector(rawInput, gravDir, true), 8),
                shiftInput  = ConfineDirections(rawInput, 4, VectFunc(Mathf.Abs, new Vector2(gravVector.y, gravVector.x)));
        // jump input
        bool jumpDown       = BufferTimer(inputJumpDown, jumpBuffer, ref jumpBufferTimer),
             jumpReleased   = inputJumpReleased,
        // shift input
             shiftDown      = inputActionDown && shiftInput != Vector2.zero && shiftInput != gravVector,
             shiftBuffered  = BufferTimer(shiftDown, shiftBuffer, ref shiftBufferTimer),
        // wall input
             xInput         = Mathf.Abs(input.x) > 0.01f,
             newWall        = !(wallJumpDir == wallDir && state == State.walljumping),
             slideInput     = input.x.Sign0() == wallDir && xInput && newWall && !onGround,
             clinging       = BufferTimer(slideInput, wallClingTime, ref wallClingTimer),
             wallJumpDown   = wallDir != 0 && jumpDown && newWall;

        // gravity shift
        if (shiftDown) shiftBufferDir = Mathf.Atan2(shiftInput.y, shiftInput.x) * Mathf.Rad2Deg;
        if (onGround) shiftsRemaining = maxShifts;
        if (shiftsRemaining > 0) shiftRefreshTimer += Time.deltaTime;
        PlayerManager.rend.color = shiftsRemaining > 0 ? Color.white : Color.red;

        if (shiftBuffered && shiftsRemaining > 0 && shiftRefreshTimer >= shiftRefreshTime) {
            shiftRefreshTimer = 0;
            shiftBufferTimer = Mathf.Infinity;
            shiftsRemaining--;

            // impacts to player
            gravDir = shiftBufferDir;
            shiftRotation.Start();
            shiftStart = transform.rotation;
            shiftEnd = Quaternion.Euler(0, 0, gravDir + 90);

            // impacts to state machine
            onGround = false;
            ChangeState(State.falling);

            // effects
            PlayerManager.instance.ScreenFreeze(shiftScreenFreezeDur);
            PlayerManager.cam.Shake(shiftShake);
            PlayerManager.cam.Bounce(shiftBounce, gravVector * -1f);

            return;
        }

        // become grounded
        if (onGround && state != State.grounded) {
            ChangeState(State.grounded);
            Audio.Play(landSound);
            stepDistRemaining = stepDist;
        }

        // during state
        stateDur += Time.deltaTime;
        switch (state) {

            case State.grounded:
                Run(input.x, xInput ? groundAccel : groundDeccel, runSpeed);

                if (!onGround) ChangeState(State.falling);
                else if (jumpDown) ChangeState(State.jumping);
                break;


            case State.jumping:
                Run(input.x, xInput ? airAccel : airDeccel, Mathf.Max(runSpeed, vel.x * input.x.Sign0()));

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

                Run(input.x, xInput ? airAccel : airDeccel, Mathf.Max(runSpeed, vel.x * input.x.Sign0()));

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

        // step sounds
        if (!xInput) stepDistRemaining = 0;
        else if (onGround && xInput) {
            float absVel = Mathf.Abs(vel.x);
            if (absVel > 1f) stepDistRemaining -= absVel * Time.deltaTime;

            if (stepDistRemaining <= 0) {
                stepDistRemaining = stepDist;
                Audio.Play(stepSound);
            }
        }

        // clamp velocitty
        vel.y = Mathf.Max(vel.y, -(input.y < 0 ? fastFallSpeed : maxFallSpeed));

        // applying gravity adjusted velocity
        PlayerManager.rb.velocity = RotateVector(vel, gravDir);
    }

    private void FixedUpdate() {
        // for first order changes to the player's position
        if (!shiftRotation.Done()) transform.rotation = Quaternion.Slerp(shiftStart, shiftEnd, shiftRotation.Evaluate(false));
    }

    private void Run(float dir, float accel, float maxSpeed) => vel.x += (dir.Sign0() * maxSpeed - vel.x) * accel * Time.deltaTime; // thank you https://pastebin.com/Dju3wz6J

    // timer functions
    private bool BufferTimer(bool reset, float time, ref float timer) {
        timer = reset ? 0 : (timer + Time.deltaTime);
        return timer <= time;
    }
    private void ResetBuffers() => jumpBufferTimer = shiftBufferTimer = Mathf.Infinity;

    // math functions
    private Vector2 RotateVector(Vector2 v, float r, bool inverse = false) {
        float a = (r + 90) * Mathf.Deg2Rad;
        if (inverse) a = Mathf.PI * 2 - a;
        float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
    private Vector2 ConfineDirections(Vector2 v, int dir, Vector2? favor = null) {

        if (v == Vector2.zero) return Vector2.zero;
        if (favor != null && Mathf.Abs(v.x) == Mathf.Abs(v.y)) return (v * (Vector2)favor).normalized;

        float m = 2f * Mathf.PI / dir,
              a = Mathf.Round(Mathf.Atan2(v.y, v.x) / m) * m;
        Vector2 v1 = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

        return new Vector2(v1.x.RoundTo(3), v1.y.RoundTo(3));
    }
    private Vector2 VectFunc(System.Func<float, int, float> f, Vector2 v, int i) => new Vector2(f(v.x, i), f(v.y, i));
    private Vector2 VectFunc(System.Func<float, float> f, Vector2 v) => new Vector2(f(v.x), f(v.y));

    // boxcast functions
    private int BoxCheck(Vector2 dir, float dist) {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, PlayerManager.col.size * transform.localScale, transform.eulerAngles.z, dir, dist, PlayerManager.groundMask);
        return hit.normal == dir * -1 ? 1 : 0;
    }
}
