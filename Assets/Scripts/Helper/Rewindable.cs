using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rewindable : MonoBehaviour {

    private static PlayerHealth rewind;

    [SerializeField] private Behaviour[] disableDuringRewind;

    private bool rewinding;

    private List<Vector3> positions = new List<Vector3>();
    private Vector2 positionVel;

    private List<float> rotations = new List<float>();
    private float rotationVel;

    private void Start() {
        if (rewind == null) rewind = FindObjectOfType<PlayerManager>().health;

        rewind.newRewindFrame.AddListener(NewRewindFrame);
        rewind.rewindStart.AddListener(StartRewind);
        rewind.rewindStop.AddListener(StopRewind);

        positions.Add(transform.position);
        rotations.Add(transform.eulerAngles.z);
    }

    private void NewRewindFrame() {
        if (rewinding) {
            int percent = rewind.rewindPercent;
            transform.SetPositionAndRotation(
                Vector2.SmoothDamp(
                    transform.position,
                    positions[percent],
                    ref positionVel,
                    rewind.rewindLerpSpeed, Mathf.Infinity,
                    Time.fixedDeltaTime),
                Quaternion.Euler(0, 0, Mathf.SmoothDampAngle(
                    transform.eulerAngles.z,
                    rotations[percent],
                    ref rotationVel,
                    rewind.rewindLerpSpeed, Mathf.Infinity,
                    Time.fixedDeltaTime)));
        } else {
            positions.Add(transform.position);
            rotations.Add(transform.eulerAngles.z);
        }

        print(positions.Count + " ?= " + rewind.savePos.Count + " ::: " + (positions.Count == rewind.savePos.Count));
    }

    private void StartRewind() {
        foreach (Behaviour b in disableDuringRewind) b.enabled = false;
        rewinding = true;
        print("started");
    }

    private void StopRewind() {
        foreach (Behaviour b in disableDuringRewind) b.enabled = true;
        rewinding = false;
        print("stopped");

        positions.Clear();
        rotations.Clear();

        NewRewindFrame();
    }
}
