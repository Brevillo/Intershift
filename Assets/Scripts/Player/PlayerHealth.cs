using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour {

    private PlayerManager m;

    [SerializeField] private float respawnInputLockDur;

    [Header("Death Time Animation")]
    [SerializeField] private string deathSound;
    [SerializeField] private float deathTimeFreezeDur;
    [SerializeField] private SmartCurve deathCamShake, slowDownCurve, rewindCurve;

    // respawning
    private bool landedInNewRoom = true;
    private Vector2 checkPointGravDir = Vector2.down;

    // death animation
    private bool doingDeathAnim;
    private float ogFixedDeltaTime;
    private List<Vector2> savePos = new List<Vector2> ();
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
        if (!landedInNewRoom && !doingDeathAnim && m.movement.onGround) {
            landedInNewRoom = true;
            checkPointGravDir = m.movement.gravDir;
            RestartSavePosAt(transform.position);
        }
    }

    private void FixedUpdate() {

        // save position
        if (rewindCurve.Done() && savePos[savePos.Count - 1] != (Vector2)transform.position)
            savePos.Add(transform.position);
        else if (!rewindCurve.Done()) {
            float rewindPercent = rewindCurve.Evaluate(false, true);
            transform.SetPositionAndRotation(savePos[(int)(rewindPercent * savePosLength)],
                Quaternion.SlerpUnclamped(deathAnimStartRotation, deathAnimEndRotation, 1 - rewindPercent));
        }
    }

    public void Death() {
        if (doingDeathAnim) return;

        Audio.Play(deathSound);
        m.cam.Shake(deathCamShake);
        StartCoroutine(DeathTimeAnimation());
    }

    private IEnumerator DeathTimeAnimation() {

        doingDeathAnim = true;
        m.input.lockInput = true;

        // time freeze
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(deathTimeFreezeDur);
        Time.timeScale = 1;

        //// time slodown
        //slowDownCurve.Start();
        //while (!slowDownCurve.Done()) {
        //    SetTimeScale(slowDownCurve.Evaluate());
        //    yield return null;
        //}

        // rewind player
        m.FreezePlayer(true);
        SetTimeScale(1);
        rewindCurve.Start();
        savePosLength = savePos.Count - 1;
        deathAnimStartRotation = transform.rotation;
        deathAnimEndRotation = Quaternion.LookRotation(Vector3.forward, -checkPointGravDir);
        while (!rewindCurve.Done())  yield return null;

        // finish rewinding
        m.movement.ResetTo(savePos[0], checkPointGravDir);
        RestartSavePosAt(savePos[0]);
        m.FreezePlayer(false);
        doingDeathAnim = false;

        // lock input after respawning
        yield return new WaitForSeconds(respawnInputLockDur);
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
