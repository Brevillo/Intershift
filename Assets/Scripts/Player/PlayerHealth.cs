using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour {

    [Header("Death Time Animation")]
    [SerializeField] private string deathSound;
    [SerializeField] private float deathTimeFreezeDur;
    [SerializeField] private SmartCurve deathCamShake, slowDownCurve;

    private bool dying;
    private bool godmode;

    private void Start() {
        TransitionManager.ResetLevel += ResetLevel;
    }

    private void ResetLevel() {
        PlayerManager.movement.lockMovement = false;
        dying = false;
    }

    private void Update() {
        if (PlayerManager.input.Debug1.down) Death();
        if (PlayerManager.input.Debug2.down) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (PlayerManager.input.FPS.down) FPSCounter.showFPS = !FPSCounter.showFPS;
        if (PlayerManager.input.Godmode.down) godmode = !godmode;
    }

    public void Death() {
        if (dying || godmode) return;

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
