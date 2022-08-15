using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour {

    private PlayerManager m;

    [SerializeField] private float respawnInputLockDur;

    [Header("Death Time Animation")]
    [SerializeField] private string deathSound;
    [SerializeField] private SmartCurve deathCamShake;
    [SerializeField] private SmartCurve slowDownCurve, rewindCurve;

    private bool doingDeathAnim, landedInNewRoom = true;
    private float ogFixedDeltaTime;
    private Vector2 checkPointGravDir = Vector2.down;
    private List<Vector2> savePos = new List<Vector2> ();

    private void Start() {
        m = GetComponent<PlayerManager>();

        ogFixedDeltaTime = Time.fixedDeltaTime;
        rewindCurve.Stop();
        savePos.Add(transform.position);

        m.rooms.RoomChange.AddListener(r => { landedInNewRoom = false; });

        m.movement.PlayerLanded.AddListener(NewSpawnpoint);
    }

    private void Update() {

        if (m.input.Debug1.down) Death();
        if (m.input.Debug2.down) Debug.Break();
    }

    private void FixedUpdate() {

        // save position
        if (rewindCurve.Done() && savePos[savePos.Count - 1] != (Vector2)transform.position)
            savePos.Add(transform.position);
    }

    private void NewSpawnpoint() {
        if (!landedInNewRoom && !doingDeathAnim) {
            landedInNewRoom = true;
            checkPointGravDir = m.movement.gravDir;
            RestartSavePosAt(transform.position);
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

        // time slodown
        m.input.lockInput = true;
        slowDownCurve.Start();
        while (!slowDownCurve.Done()) {
            SetTimeScale(slowDownCurve.Evaluate());
            yield return null;
        }

        // rewind player
        m.FreezePlayer(true);
        SetTimeScale(1);
        rewindCurve.Start();
        int length = savePos.Count - 1;
        while (!rewindCurve.Done()) {
            transform.position = savePos[(int)(rewindCurve.Evaluate() * length)];
            yield return null;
        }

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
