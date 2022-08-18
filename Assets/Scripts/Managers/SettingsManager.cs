using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour {

    public static Settings settings = null;

    [SerializeField] private Settings localSettings;

    private void Awake() {
        if (settings == null) settings = new Settings();

        localSettings = settings;
    }

    private void OnValidate() {
        settings = localSettings;
    }
}

[System.Serializable]
public class Settings {

    public bool cameraShake = true;
    public bool bouncyCameraTransitions = true;

    [Range(0f, 1f)]
    public float musicVolume = 0.5f, soundEffectsVolume = 0.5f;

}
