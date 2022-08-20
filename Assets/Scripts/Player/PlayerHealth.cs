using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour {

    private PlayerManager m;

    [SerializeField] private float respawnInputLockDur;

    [Header("Death Time Animation")]
    [SerializeField] private string deathSound;
    [SerializeField] internal float
        rewindLerpSpeed;
    [SerializeField] private float
        rewindFrameFrequency,
        deathTimeFreezeDur;
    [SerializeField] private SmartCurve deathCamShake, slowDownCurve, rewindCurve;

    // respawning
    private bool landedInNewRoom = true;
    private Vector2 checkPointGravDir = Vector2.down;

    // rewinding
    internal UnityEvent
        newRewindFrame = new UnityEvent(),
        rewindStart = new UnityEvent(),
        rewindStop = new UnityEvent();
    internal int rewindPercent;
    private float rewindFrameTimer;
    private Vector2 rewindLerpVel;
    internal List<Vector2> savePos = new List<Vector2> ();

    // death animation
    private bool dying;
    private float ogFixedDeltaTime;
    private int savePosLength;
    private Quaternion deathAnimStartRotation, deathAnimEndRotation;

    private void Start() {
        m = GetComponent<PlayerManager>();

        ogFixedDeltaTime = Time.fixedDeltaTime;
        rewindCurve.Stop();
        savePos.Add(transform.position);

        m.rooms.RoomChange.AddListener(r => { landedInNewRoom = false; });
    }

    private void Update() {

        if (m.input.Debug1.down) Death();
        if (m.input.Debug2.down) SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // new spawnpoint
        if (!landedInNewRoom && !dying && m.movement.onGround) {
            landedInNewRoom = true;
            checkPointGravDir = m.movement.gravDir;
            RestartSavePosAt(transform.position);
        }
    }

    private void FixedUpdate() {

        rewindFrameTimer += Time.fixedDeltaTime;
        if (rewindFrameTimer >= rewindFrameFrequency) {
            rewindFrameTimer = 0;
            newRewindFrame.Invoke();

            // save position
            if (rewindCurve.Done()) savePos.Add(transform.position);

            // rewind
            else {
                float curve = rewindCurve.Evaluate();
                rewindPercent = (int)(curve * savePosLength);

                transform.SetPositionAndRotation(
                    Vector2.SmoothDamp(transform.position, savePos[rewindPercent], ref rewindLerpVel, rewindLerpSpeed, Mathf.Infinity, Time.fixedDeltaTime),
                    Quaternion.SlerpUnclamped(deathAnimStartRotation, deathAnimEndRotation, 1 - curve));
            }
        }
    }

    public void Death() {
        if (dying) return;

        Audio.Play(deathSound);
        m.cam.Shake(deathCamShake);
        StartCoroutine(DeathTimeAnimation());
    }

    private IEnumerator DeathTimeAnimation() {

        dying = true;
        m.input.lockInput = true;
        m.movement.enabled = false;

        // time freeze
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(deathTimeFreezeDur);
        Time.timeScale = 1;

        // time slodown
        slowDownCurve.Start();
        while (!slowDownCurve.Done()) {
            SetTimeScale(slowDownCurve.Evaluate());
            yield return null;
        }

        // rewind player
        rewindStart.Invoke();
        m.FreezePlayer(true);
        SetTimeScale(1);
        rewindCurve.Start();
        rewindFrameTimer = 0;
        savePosLength = savePos.Count - 1;
        deathAnimStartRotation = transform.rotation;
        deathAnimEndRotation = Quaternion.LookRotation(Vector3.forward, -checkPointGravDir);
        while (!rewindCurve.Done())  yield return null;

        // finish rewinding
        rewindStop.Invoke();
        m.movement.ResetTo(savePos[0], checkPointGravDir);
        RestartSavePosAt(savePos[0]);
        m.FreezePlayer(false);
        dying = false;

        // lock input after respawning
        yield return new WaitForSeconds(respawnInputLockDur);
        m.movement.enabled = true;
        m.input.lockInput = false;
    }

    private void RestartSavePosAt(Vector2 start) {
        savePos.Clear();
        savePos.Add(start);
    }

    private void SetTimeScale(float i) {
        Time.timeScale = i;
        Time.fixedDeltaTime = ogFixedDeltaTime * i;
    }
}
