using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour {

    public static bool showFPS = true;

    [SerializeField] private TextMeshProUGUI text;

    private List<float> deltaTimes = new List<float>(),
                        times = new List<float>();

    private void Start() {
        DontDestroyOnLoad(this);
    }

    private void Update() {

        deltaTimes.Add(Time.deltaTime);
        times.Add(Time.time);
        for (int i = 0; i < times.Count; i++) {
            if (Time.time - times[i] > 1f) {
                deltaTimes.RemoveAt(i);
                times.RemoveAt(i);
                i--;
            }
        }

        float sum = 0f;
        foreach (float t in deltaTimes) sum += t;
        float fps = Mathf.Round(1f / (sum / deltaTimes.Count));

        text.text = showFPS ? fps.ToString() : "";
    }
}
