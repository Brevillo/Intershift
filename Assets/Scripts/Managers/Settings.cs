using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour {

    public static Settings instance;

    public bool cameraShake = true,
                bouncyCameraTransitions = true;

    [Range(0f, 1f)]
    public float musicVolume = 0.5f, soundEffectsVolume = 0.5f;

    private void Awake() {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(this);
    }
}
