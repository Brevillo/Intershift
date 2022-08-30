using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour {

    [Header("Death Time Animation")]
    [SerializeField] private string deathSound;
    [SerializeField] private float deathTimeFreezeDur;
    [SerializeField] private SmartCurve deathCamShake, slowDownCurve;

    // death animation
    private bool dying;

    private void Start() {
        TransitionManager.TransitionComplete += TransitionComplete;
    }

    private void TransitionComplete() {
        PlayerManager.movement.lockMovement = false;
        dying = false;
    }

    private void Update() {
        if (PlayerManager.input.Debug1.down) Death();
        if (PlayerManager.input.Debug2.down) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Death() {
        if (dying) return;

        Audio.Play(deathSound);
        PlayerManager.cam.Shake(deathCamShake);
        StartCoroutine(DeathTimeAnimation());
    }

    private IEnumerator DeathTimeAnimation() {

        dying = true;
        PlayerManager.movement.lockMovement = true;

        // time freeze
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(deathTimeFreezeDur);
        Time.timeScale = 1;

        // time slodown
        slowDownCurve.Start();
        while (!slowDownCurve.Done()) {
            Helper.SetTimeScale(slowDownCurve.Evaluate());
            yield return null;
        }

        // respawn
        TransitionManager.PlayerDeath();
    }
}
