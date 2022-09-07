using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour {

    [SerializeField] private RectTransform playerDeathMask;
    [SerializeField] private float playerDeathInBetweenWaitTime;
    [SerializeField] private SmartCurveComposite playerDeathTransition;

    public static event System.Action ResetLevel;

    private static TransitionManager instance;

    private void Awake() {
        instance = this;
    }

    private void OnDestroy() {
        ResetLevel = null;
    }

    public static void LoadLevel(string scene) => SceneManager.LoadScene(scene);
    public static void LoadLevel(int scene) => SceneManager.LoadScene(scene);

    public static void PlayerDeath() {
        instance.StartCoroutine(instance.PlayerDeathRoutine());
    }

    private IEnumerator PlayerDeathRoutine() {

        playerDeathMask.gameObject.SetActive(true);

        playerDeathMask.position = PlayerManager.cameraObject.WorldToScreenPoint(PlayerManager.transform.position);
        playerDeathTransition.Start();

        while (!playerDeathTransition.Done()) {
            playerDeathMask.sizeDelta = Vector2.one * 6000f * playerDeathTransition.Evaluate();
            yield return null;
        }

        ResetLevel.Invoke();

        Helper.SetTimeScale(1f);
        playerDeathTransition.Continue();

        yield return new WaitForSecondsRealtime(playerDeathInBetweenWaitTime);

        while (!playerDeathTransition.Done()) {
            playerDeathMask.position = PlayerManager.cameraObject.WorldToScreenPoint(PlayerManager.transform.position);
            playerDeathMask.sizeDelta = Vector2.one * 6000f * playerDeathTransition.Evaluate();
            yield return null;
        }

        playerDeathMask.gameObject.SetActive(false);
    }
}
