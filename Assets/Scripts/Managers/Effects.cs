using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effects : MonoBehaviour {

    private PlayerManager m;

    [SerializeField] private ParticleSystem starSystem;
    [SerializeField] private bool wrap;

    private float prevPlayerAngle;

    private void Start() {
        m = FindObjectOfType<PlayerManager>();
        prevPlayerAngle = m.transform.eulerAngles.z * Mathf.Deg2Rad;
    }

    private void Update() {

        // rotate particle spawner
        starSystem.transform.parent.rotation = m.transform.rotation;

        // rotate particles around player and rotate their velocity
        float playerAngle = m.transform.eulerAngles.z * Mathf.Deg2Rad;
        bool rotate = playerAngle != prevPlayerAngle;
        float angleDelta = playerAngle - prevPlayerAngle,
              cos = Mathf.Cos(angleDelta), sin = Mathf.Sin(angleDelta),
              velAngle = playerAngle + Mathf.PI / 2;
        Vector2 pivot = m.transform.position,
                velDir = new Vector2(Mathf.Cos(velAngle), Mathf.Sin(velAngle));

        var particles = new ParticleSystem.Particle[starSystem.main.maxParticles];
        int num = starSystem.GetParticles(particles);

        if (rotate)
            for (int i = 0; i < num; i++) {
                var p = particles[i];
                Vector2 pos = p.position;

                // position + velocity rotation
                if (rotate) {
                    pos = RotateVectorCached(pos, pivot, cos, sin);
                    p.velocity = velDir * p.velocity.magnitude;
                }
                
                p.position = pos;
                particles[i] = p;
            }
        //else if (wrap) for (int i = 0; i < num; i++)
        //    particles[i].position = VectorMod((Vector2)particles[i].position + halfBox, box) - halfBox;


        starSystem.SetParticles(particles, num);

        prevPlayerAngle = playerAngle;
    }

    private void OnParticleTrigger() {
        
    }

    float BetterModulo(float num, float div) => num - div * Mathf.Floor(num / div);

    Vector2 VectorMod(Vector2 v1, Vector2 v2) => new Vector2(BetterModulo(v1.x, v2.x), BetterModulo(v1.y, v2.y));

    Vector2 RotateVectorCached(Vector2 vector, Vector2 pivot, float cos, float sin) {
        vector -= pivot;
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos) + pivot;
    }

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
