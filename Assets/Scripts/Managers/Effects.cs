using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effects : MonoBehaviour {

    private PlayerManager m;

    [SerializeField] private ParticleSystem starSystem;
    [SerializeField] private float starRotationLerpTime;
    [SerializeField] private ParticleSystem.MinMaxCurve parallax; // useful little thingy 

    private float prevLerpAngle, lerpAngle, lerpVel;
    private Vector2 prevCamPos;

    private void Start() {
        m = FindObjectOfType<PlayerManager>();
        prevLerpAngle = m.transform.eulerAngles.z * Mathf.Deg2Rad;
    }

    private void FixedUpdate() {

        // rotate particle spawner
        starSystem.transform.parent.rotation = m.transform.rotation;

        // rotate particles around player and rotate their velocity
        lerpAngle = Mathf.SmoothDampAngle(lerpAngle, m.transform.eulerAngles.z, ref lerpVel, starRotationLerpTime, Mathf.Infinity, Time.fixedDeltaTime);

        var particles = new ParticleSystem.Particle[starSystem.main.maxParticles];
        int num = starSystem.GetParticles(particles);

        // rotation
        float angleDelta = (lerpAngle - prevLerpAngle) * Mathf.Deg2Rad,
                cos = Mathf.Cos(angleDelta), sin = Mathf.Sin(angleDelta),
                velAngle = (lerpAngle + 90f) * Mathf.Deg2Rad;
        Vector2 pivot = m.transform.position,
                velDir = new Vector2(Mathf.Cos(velAngle), Mathf.Sin(velAngle));
        prevLerpAngle = lerpAngle;

        // parallax

        Vector2 camPos = m.cam.transform.position,
                camDelta = camPos - prevCamPos;
        prevCamPos = camPos;
        float minSpeed = starSystem.main.startSpeed.constantMin,
              speedDif = starSystem.main.startSpeed.constantMax - minSpeed;

        for (int i = 0; i < num; i++) {
            var p = particles[i];
            float speed = p.velocity.magnitude;

            p.velocity = velDir * speed;
            p.position += (Vector3)camDelta * parallax.Evaluate(1f - (speed - minSpeed) / speedDif);
            p.position = RotateVectorCached(p.position, pivot, cos, sin);

            particles[i] = p;
        }

        starSystem.SetParticles(particles, num);
    }

    private float BetterModulo(float num, float div) => num - div * Mathf.Floor(num / div);
    private Vector2 VectorMod(Vector2 v1, Vector2 v2) => new Vector2(BetterModulo(v1.x, v2.x), BetterModulo(v1.y, v2.y));

    private Vector2 RotateVectorCached(Vector2 vector, Vector2 pivot, float cos, float sin) {
        vector -= pivot;
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos) + pivot;
    }

    // sad this doesn't work
    private void SetSimSpace(ParticleSystem system, ParticleSystemSimulationSpace space) {
        // save system state
        var particles = new ParticleSystem.Particle[system.main.maxParticles];
        int num = system.GetParticles(particles);

        // change simulation space
        var main = system.main;
        main.simulationSpace = space;

        // reapply system state
        system.SetParticles(particles, num);
    }
}
