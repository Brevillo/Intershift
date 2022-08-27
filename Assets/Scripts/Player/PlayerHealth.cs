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
    private Vector2 respawnGravDir = Vector2.down, respawnPos;

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
    private Quaternion deathAnimStartRotation, deathAnimEndRotation;

    private void Start() {
        m = GetComponent<PlayerManager>();

        ogFixedDeltaTime = Time.fixedDeltaTime;
        rewindCurve.Stop();
        savePos.Add(transform.position);

        m.rooms.ExitRespawnZone.AddListener(ExitRespawnZone);
    }

    private void Update() {
        if (m.input.Debug1.down) Death();
        if (m.input.Debug2.down) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void FixedUpdate() {

        // save position
        if (rewindCurve.Done()) {
            rewindFrameTimer += Time.fixedDeltaTime;

            if (rewindFrameTimer >= rewindFrameFrequency && savePos[savePos.Count - 1] != (Vector2)transform.position) {
                rewindFrameTimer = 0;
                savePos.Add(transform.position);
                newRewindFrame.Invoke();
            }

        // rewind
        } else {
            float curve = rewindCurve.Evaluate();
            rewindPercent = (int)(curve * (savePos.Count - 1f));

            transform.SetPositionAndRotation(
                Vector2.SmoothDamp(transform.position, savePos[rewindPercent], ref rewindLerpVel, rewindLerpSpeed, Mathf.Infinity, Time.fixedDeltaTime),
                Quaternion.SlerpUnclamped(deathAnimStartRotation, deathAnimEndRotation, 1 - curve));
        }
    }

    public void Death() {
        if (dying) return;

        Audio.Play(deathSound);
        m.cam.Shake(deathCamShake);
        StartCoroutine(DeathTimeAnimation());
    }

    private void ExitRespawnZone(Vector2 gravDir) {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, gravDir, Mathf.Infinity, m.groundMask);
        if (hit) {
            respawnGravDir = gravDir;
            respawnPos = hit.point;

            rewindStop.Invoke();
            RestartSavePosAt(respawnPos);

        } else
            Debug.LogError("Couldn't find respawn position.");
    }

    private IEnumerator DeathTimeAnimation() {

        dying = true;
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

        // rewind start
        rewindStart.Invoke();
        m.FreezePlayer(true);
        SetTimeScale(1);
        rewindCurve.Start();
        deathAnimStartRotation = transform.rotation;
        deathAnimEndRotation = Quaternion.LookRotation(Vector3.forward, -respawnGravDir);
        while (!rewindCurve.Done())  yield return null;

        // finish stop
        rewindStop.Invoke();
        m.movement.ResetTo(savePos[0], respawnGravDir);
        RestartSavePosAt(savePos[0]);
        m.FreezePlayer(false);
        rewindFrameTimer = 0;
        dying = false;

        // lock input after respawning
        yield return new WaitForSeconds(respawnInputLockDur);
        m.movement.enabled = true;
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
