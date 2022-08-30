using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour {

    [SerializeField] private SmartCurveComposite playerDeathTransition;
    [SerializeField] private RectTransform playerDeathMask;

    public static event System.Action ResetLevel, TransitionComplete;

    private static TransitionManager instance;

    private void Awake() {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public static void PlayerDeath() {
        instance.StartCoroutine(instance.PlayerDeathRoutine());
    }

    private IEnumerator PlayerDeathRoutine() {

        playerDeathMask.gameObject.SetActive(true);

        playerDeathTransition.Start();
        while (!playerDeathTransition.Done()) {
            playerDeathMask.sizeDelta = Vector2.one * 2600 * playerDeathTransition.Evaluate();
            yield return null;
        }

        ResetLevel.Invoke();

        Helper.SetTimeScale(1f);

        playerDeathTransition.Continue();
        while (!playerDeathTransition.Done()) {
            playerDeathMask.sizeDelta = Vector2.one * 2600 * playerDeathTransition.Evaluate();
            yield return null;
        }

        TransitionComplete.Invoke();

        playerDeathMask.gameObject.SetActive(false);
    }
}
