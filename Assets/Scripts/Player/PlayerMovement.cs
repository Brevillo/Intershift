using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    #region parameters

    // the first variable has to be separate because otherwise the header is applied to every variable
    [Header("Running")]
    [SerializeField] private float
        runSpeed = 18f;                 // max run speed
    [SerializeField] private float
        groundAccel = 15f,              // acceleration while grounded
        groundDeccel = 30f,             // decceleration while grounded
        airAccel = 15f,                 // acceleration while in air
        airDeccel = 10f;                // decceleration while in air

    [Header("Jumping")]
    [SerializeField] private SmartCurve
        jumpCurve;                      // animation curve for the velocity of the jump
    [SerializeField] private float
        jumpHeight = 4f,                // max jump height
        fallGrav = 100f,                // gravity strength of the player while moving down
        maxFallSpeed = 30f,             // max downward speed of the player
        fastFallSpeed = 40f,            // max downward speed of the player inputting the down direction
        coyoteTime = 0.08f,             // time after leaving the ground when the player can still jump
        jumpBuffer = 0.1f;              // time before touching the ground when the player can buffer a jump input 

    [Header("Wall Jumping")]
    [SerializeField] private SmartCurve
        wallJumpCurve;                  // animation curve for the velocity of the walljump
    [SerializeField] private Vector2
        walljumpVector = new Vector2(0.5f, 4f);  // max horizontal and vertical distances of the walljump
    [SerializeField] private float
        walljumpSpeed = 25f,            // horizontal speed of the walljump
        walljumpNoTurnTime = 0f,        // time after the player has boosted from the wall before they can turn back towards the wall
        wallClingTime = 0.05f,          // time after trying to stop wall sliding before the player stops sliding
        slideGrav = 10f,                // gravity strength of the player while wall sliding
        slideSpeed = 8f;                // max downward speed of the player while wall sliding

    [Header("Gravity Shifting")]
    [SerializeField] internal int
        maxShifts = 1;                  // max number of gravity shifts after leaving the ground
    [SerializeField] private float
        shiftBuffer = 0.1f,             // time before regaining the ability to gravity shift when the player can buffer a shift input
        shiftRefreshTime = 0.1f,        // time after landing when the player's shifts are refilled
        shiftScreenFreezeDur = 0.05f;   // time to freeze the screen after gravity shifting
    [SerializeField] private SmartCurve
        shiftRotation,                  // animation curve of the players rotation after gravity shifting
        shiftShake,                     // animation curve of the screen shake after gravity shifting
        shiftBounce;                    // animation curve of the screen bounce after gravity shifting

    [Header("Other")]
    [SerializeField] private float
        groundCheckDist = 0.05f;        // distance from the bottom of the player's collider to check for the ground
    [SerializeField] private float
        ceilingCheckDist = 0.05f,       // distance from the top of the player's collider to check for the ceiling
        wallCheckDist = 0.1f;           // distance from the sides of the player's collider to check for walls

    [Header("Sound")]
    [SerializeField] private float
        stepDist = 3f;                  // the horizontal distance for the player to cover before playing the step sound
    [SerializeField] private string
        stepSound,                      // sound for the player walking on ground
        landSound,                      // sound for the player becoming grounded
        walljumpSound;                  // sound for the player doing a walljump

    #endregion

    public static Vector3 respawnInfo = Vector3.forward * -90f; // x/y are position, z is gravDir
    internal bool lockMovement = false;                         // locks the input
    internal float gravDir { get; private set; } = -90f;        // player gravity direction in degrees

    #region private variables

    // movement
    private float
        jumpBufferTimer,     // time since jump pressed
        wallClingTimer,      // time since left wall slide, but still clinging
        walljumpNoTurnTimer; // time since horizontal wall boost ended
    private int wallJumpDir; // wallDir of the current walljump
    private bool onGround;   // is the player on the ground?

    // gravity shifting
    private float shiftBufferTimer,  // time since grav shift input
                  shiftRefreshTimer, // time since last grav shift
                  shiftBufferDir,    // buffered grav shift direction
                  shiftStart,   // start of gravity shift rotation
                  shiftEnd;     // end of gravity shift rotation
    private int shiftsRemaining;     // number of grav shifts remaining since leaving the ground
    
    // state managment
    private enum State {
        grounded,    // player is grounded
        jumping,     // player is moving upwards after being grounded
        falling,     // player is in the air moving downwards
        sliding,     // player is touching a wall and holding that direction
        walljumping, // player is moving away and up from a wall after having touched the wall (not necessarily sliding)
    };
    private State state, prevState;
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
        TransitionManager.ResetLevel += ResetLevel;
        ResetLevel();
    }

    private void ResetLevel() {

        Vector2 pos = respawnInfo;
        float newGravDir = respawnInfo.z;

        transform.SetPositionAndRotation(pos + -newGravDir.DegToVector() * PlayerManager.col.bounds.extents.y, Quaternion.Euler(0, 0, newGravDir + 90));

        PlayerManager.rb.velocity = Vector2.zero;
        gravDir = newGravDir;
        ChangeState(State.grounded);

        shiftRotation.Stop();
        jumpCurve.Stop();
        wallJumpCurve.Stop();
        shiftStart = shiftEnd = gravDir + 90f;
        jumpBufferTimer = shiftBufferTimer = Mathf.Infinity;
    }

    private void Update() {

        // determine velocity
        Vector2 vel = RotateVector(PlayerManager.rb.velocity, gravDir, true);

        Vector2 gravVector = gravDir.DegToVector();

        // determine if touching ground or ceiling
        bool jumping  = state == State.jumping || state == State.walljumping,
             headBump = BoxCheck(-gravVector, ceilingCheckDist) == 1 && jumping;
             onGround = BoxCheck(gravVector, groundCheckDist) == 1 && !jumping;

        // determine wall proximity
        Vector2 rightVector = RotateVector(Vector2.right, gravDir);
        int wallDir = BoxCheck(rightVector, wallCheckDist) - BoxCheck(-rightVector, wallCheckDist);

        // raw input
        Vector2 inputRaw     = PlayerManager.input.Move;
        bool jumpDownRaw     = PlayerManager.input.Jump.down,
             jumpReleasedRaw = PlayerManager.input.Jump.released,
             shiftDownRaw    = PlayerManager.input.Action.down;

        // lock movement
        if (lockMovement) {
            inputRaw = Vector2.zero;
            jumpDownRaw = jumpReleasedRaw = shiftDownRaw = false;
        }

        Vector2 // directional input
            input      = ConfineDirections(RotateVector(inputRaw, gravDir, true), 8),
            shiftInput = ConfineDirections(inputRaw, 4, new Vector2(gravVector.y, gravVector.x).Abs());

        bool
        jumpBuff     = BufferTimer(jumpDownRaw, jumpBuffer, ref jumpBufferTimer),                  // jump buffered?
        jumpReleased = jumpReleasedRaw,                                                            // jump button up this frame?
        shiftDown    = shiftDownRaw && shiftInput != Vector2.zero,                                 // shift button down this frame?
        shiftBuff    = BufferTimer(shiftDown, shiftBuffer, ref shiftBufferTimer),                  // shift buffered?
        xInput       = Mathf.Abs(input.x) > 0.01f,                                                 // horizontal input is happening?
        newWall      = !(wallJumpDir == wallDir && state == State.walljumping),                    // touching a wall that wasn't just jumped off of?
        sliding      = input.x.Sign0() == wallDir && xInput && newWall && !onGround,               // able to slide?
        clinging     = BufferTimer(sliding, wallClingTime, ref wallClingTimer),                    // still clinging?
        walljumpBuff = wallDir != 0 && jumpBuff && newWall,                                        // walljump buffered?
        boosting     = stateDur <= walljumpVector.x / walljumpSpeed && state == State.walljumping, // moving away from the wall after walljumping?
        canTurn      = !BufferTimer(boosting, walljumpNoTurnTime, ref walljumpNoTurnTimer);        // can turn back to the wall after walljumping?

        // gravity shift
        if (shiftDown) shiftBufferDir = Mathf.Atan2(shiftInput.y, shiftInput.x) * Mathf.Rad2Deg;
        if (onGround) shiftsRemaining = maxShifts;
        if (shiftsRemaining > 0) shiftRefreshTimer += Time.deltaTime;
        PlayerManager.rend.color = shiftsRemaining > 0 ? Color.white : Color.red;

        if (shiftBuff && shiftsRemaining > 0 && shiftRefreshTimer >= shiftRefreshTime) {
            shiftRefreshTimer = 0;
            shiftBufferTimer = Mathf.Infinity;
            shiftsRemaining--;

            // impacts to player
            gravDir = shiftBufferDir;
            shiftRotation.Start();
            shiftStart = transform.eulerAngles.z;
            shiftEnd = gravDir + 90f;

            // impacts to state machine
            onGround = false;
            ChangeState(State.falling);

            // effects
            PlayerManager.cam.ScreenFreeze(shiftScreenFreezeDur);
            PlayerManager.cam.Shake(shiftShake);
            PlayerManager.cam.Bounce(shiftBounce, gravDir.DegToVector() * -1f);

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
                Run(input.x, xInput ? groundAccel : groundDeccel, runSpeed, ref vel);

                if (!onGround) ChangeState(State.falling);
                else if (jumpBuff) ChangeState(State.jumping);
                break;

            case State.jumping:
                Run(input.x, xInput ? airAccel : airDeccel, Mathf.Max(runSpeed, vel.x * input.x.Sign0()), ref vel);

                if (jumpReleased || headBump) {
                    jumpCurve.Stop();
                    vel.y = 0;
                } else
                    vel.y = jumpCurve.Evaluate();

                if (walljumpBuff) ChangeState(State.walljumping);
                else if (jumpCurve.Done()) ChangeState(State.falling);
                break;

            case State.falling:
                
                if (canTurn) Run(input.x, xInput ? airAccel : airDeccel, Mathf.Max(runSpeed, vel.x * input.x.Sign0()), ref vel);

                if (!wallJumpCurve.Done()) vel.y = wallJumpCurve.Evaluate();
                else vel.y -= fallGrav * Time.deltaTime;

                if (walljumpBuff) ChangeState(State.walljumping);
                else if (sliding) ChangeState(State.sliding);
                else if (stateDur <= coyoteTime && jumpBuff && prevState == State.grounded) ChangeState(State.jumping);
                break;

            case State.sliding:
                vel.y -= (slideSpeed + vel.y) * slideGrav * Time.deltaTime;

                if (walljumpBuff) ChangeState(State.walljumping);
                else if (!clinging || wallDir == 0) ChangeState(State.falling);
                break;

            case State.walljumping:

                if (canTurn) Run(input.x, xInput ? airAccel : airDeccel, Mathf.Max(runSpeed, vel.x * input.x.Sign0()), ref vel);

                if (jumpReleased || headBump) {
                    wallJumpCurve.Stop();
                    vel.y = 0;
                } else
                    vel.y = wallJumpCurve.Evaluate();

                if (walljumpBuff) ChangeState(State.walljumping);
                else if (!boosting && wallJumpCurve.Done()) ChangeState(State.falling);
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
                jumpBufferTimer = walljumpNoTurnTimer = Mathf.Infinity;
                wallJumpDir = wallDir;;
                wallJumpCurve.Start();
                wallJumpCurve.valueScale = walljumpVector.y;
                vel.x = walljumpSpeed * -wallDir;
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

        if (shiftRotation.Done()) {
            PlayerManager.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            transform.rotation = Quaternion.Euler(0f, 0f, shiftEnd);
        } else {
            PlayerManager.rb.MoveRotation(Mathf.LerpAngle(shiftStart, shiftEnd, Mathf.Min(1f, shiftRotation.Evaluate())));
            PlayerManager.rb.constraints = 0;
        }
    }

    private void Run(float dir, float accel, float maxSpeed, ref Vector2 vel) => vel.x += (dir.Sign0() * maxSpeed - vel.x) * accel * Time.deltaTime; // thank you https://pastebin.com/Dju3wz6J

    // timer functions
    private bool BufferTimer(bool reset, float time, ref float timer) {
        timer = reset ? 0 : (timer + Time.deltaTime);
        return timer <= time;
    }

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

    // boxcast functions
    private int BoxCheck(Vector2 dir, float dist) {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, PlayerManager.col.size * transform.localScale, transform.eulerAngles.z, dir, dist, PlayerManager.groundMask);
        return hit.normal == dir * -1 ? 1 : 0;
    }
}
