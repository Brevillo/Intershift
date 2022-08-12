using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour {

    public static Settings i;

    public bool cameraShake = true,
                cameraBounce = true;

    private void Awake() {
        if (i == null) i = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(this);
    }
}
