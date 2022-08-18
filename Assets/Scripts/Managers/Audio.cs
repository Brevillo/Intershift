using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

[System.Serializable]
public class SceneMusic {
    [HideInInspector] public string name;
    public Object scene;
    public AudioClip music;
    public float volume;
}

[System.Serializable]
public class SoundEffect {

    public string name;
    public AudioClip[] clip;

    [Header("Parameters")]
    public float volume;
    public float minPitch, maxPitch;
    public bool sequential = true;

    internal AudioSource source;

    private int clipIndex;
    public AudioClip GetClip() {
        AudioClip currentClip;

        if (sequential) {
            currentClip = clip[clipIndex % clip.Length];
            clipIndex++;
        } else
            currentClip = clip[Random.Range(0, clip.Length - 1)];

        return currentClip;
    }
}

public class Audio : MonoBehaviour {

    public static Audio instance;

    [SerializeField] private SceneMusic[] scenes;
    [SerializeField] private SoundEffect[] sounds;

    [SerializeField] private AudioMixerGroup musicGroup, soundGroup;

    private AudioSource musicSource;

    private void Awake() {
        if(instance == null) instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(this);
    }

    private void Start() {

        foreach (SoundEffect s in sounds) {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.outputAudioMixerGroup = soundGroup;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = true;

        UpdateMusic();
    }

    private void Update() {

        float mVol = SettingsManager.settings.musicVolume,
              sVol = SettingsManager.settings.soundEffectsVolume;
        AudioMixer mix = musicGroup.audioMixer;

        mix.SetFloat("MusicVolume", mVol == 0 ? -80f : Mathf.Log10(mVol) * 20);
        mix.SetFloat("SoundEffectsVolume", sVol == 0 ? -80f : Mathf.Log10(sVol) * 20);
    }

    public static void Play(string name) {
        foreach (SoundEffect s in instance.sounds)
            if (s.name == name) {
                s.source.pitch = Random.Range(s.minPitch, s.maxPitch);
                s.source.PlayOneShot(s.GetClip());
                s.source.volume = s.volume;
                return;
            }
        Debug.LogError("Sound effect \"" + name + "\" could not be found.");
    }

    private void UpdateMusic() {
        string currentScene = SceneManager.GetActiveScene().name;
        foreach (SceneMusic m in scenes)
            if (m.scene.name == currentScene) {

                if (m.music == musicSource.clip) return;

                musicSource.clip = m.music;
                musicSource.volume = m.volume;
                musicSource.Play();
                return;
            }
    }

    private void OnValidate() {
        foreach (SceneMusic m in scenes) m.name = m.scene.name;
    }
}
