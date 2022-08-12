using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour {

    private PlayerManager m;

    [SerializeField] private float respawnInputLockDur;
    [SerializeField] internal Transform spawnPoint;

    [Header("Death Time Animation")]
    [SerializeField] private SmartCurve deathCamShake;
    [SerializeField] private SmartCurve slowDownCurve, rewindCurve;

    private bool deathAnim;
    private float ogFixedDeltaTime;
    private List<Vector2> savePos = new List<Vector2> ();

    private void Start() {
        m = GetComponent<PlayerManager>();
        ogFixedDeltaTime = Time.fixedDeltaTime;
        rewindCurve.Stop();
        savePos.Add(transform.position);
    }

    private void Update() {
        
        if (m.input.Debug1.down) Death();
        if (m.input.Debug2.down) {
            Debug.Break();
            //TrailRenderer trail = GetComponent<TrailRenderer>();
            //trail.enabled =!trail.enabled;
            //trail.Clear();
        }
    }

    private void FixedUpdate() {

        // save position
        if (rewindCurve.Done() && savePos[savePos.Count - 1] != (Vector2)transform.position)
            savePos.Add(transform.position);
    }

    public void Death() {
        if (deathAnim) return;

        m.cam.Shake(deathCamShake);
        StartCoroutine(DeathTimeAnimation());
    }

    IEnumerator DeathTimeAnimation() {

        deathAnim = true;

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
        m.movement.ResetTo(savePos[0]);
        savePos.Clear();
        savePos.Add(transform.position);
        m.FreezePlayer(false);
        deathAnim = false;

        // lock input after respawning
        yield return new WaitForSeconds(respawnInputLockDur);
        m.input.lockInput = false;
    }

    void SetTimeScale(float i) {
        Time.timeScale = i;
        Time.fixedDeltaTime = ogFixedDeltaTime * i;
    }
}
