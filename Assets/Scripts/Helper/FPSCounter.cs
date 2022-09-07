using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour {

    public static bool showFPS = true;
    [SerializeField] private bool serializedShowFPS;
    [SerializeField] private TextMeshProUGUI text;

    private const float fpsAveragingDur = 1f;
    private List<float> deltaTimes = new List<float>(),
                        times = new List<float>();

    private static FPSCounter instance;
    private void Start() {
        if (instance == null) instance = this;
        else Destroy(this);
        DontDestroyOnLoad(this);
    }

    private void OnValidate() {
        showFPS = serializedShowFPS;
    }

    private void Update() {

        // fps is calculated by the average deltaTime over the past fpsAveragingDur seconds

        deltaTimes.Add(Time.deltaTime);
        times.Add(Time.time);
        for (int i = 0; i < times.Count; i++) {
            if (Time.time - times[i] > fpsAveragingDur) {
                deltaTimes.RemoveAt(i);
                times.RemoveAt(i);
                i--;
            }
        }

        float sum = 0f;
        foreach (float t in deltaTimes) sum += t;
        float fps = Mathf.Round(deltaTimes.Count / sum);

        text.text = showFPS ? fps.ToString() : "";
    }
}
