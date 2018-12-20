using UnityEngine;

//[ExecuteInEditMode]
[RequireComponent(typeof(ParticleSystem))]
public class ParticleMvt : MonoBehaviour {
#if false
    public float speedFactor = 0.5f;

    private ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;

    // Use this for initialization
    void Start () {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
    }

    //-- Moves particles based on the speedField
    void LateUpdate() {
        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        int numParticlesAlive = particleSystem.GetParticles(particles);     // returns The number of particles written to the input particle array (the number of particles currently alive). 

        //Loop through alive particle
        for (int i = 0; i < numParticlesAlive; i++) {
            //Get current position
            Vector3 currentPosition = particles[i].position;

            //Get interpolated speed vector
            Vector3 newSpeed = DataBase.getSpeedAtPoint(currentPosition);
            if (newSpeed == Vector3.zero) continue;     //TODO handle

            //Set new speed vector            
            particles[i].velocity = newSpeed;
        }

        //Apply the particle changes to the particle system
        particleSystem.SetParticles(particles, numParticlesAlive);
    }

    //-- Moves particles one target after another --
    void LateUpdate_NotUSED() {
        if (PopulateStreamLines.distanceMax_CACA != 0) {
            var colorBySpeed = particleSystem.colorBySpeed;
            colorBySpeed.range = new Vector2(speedFactor * PopulateStreamLines.distanceMin_CACA, speedFactor * PopulateStreamLines.distanceMax_CACA);    //TODO CACA ordre important + pas MAJ si speedFactor MAJ
        }


        //Get custom per-particle data
        //ParticleSystem.CustomDataModule customData = particleSystem.customData;
        List<Vector4> customData = new List<Vector4>();
        particleSystem.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);        

        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        int numParticlesAlive = particleSystem.GetParticles(particles);     // returns The number of particles written to the input particle array (the number of particles currently alive). 

        //BUG ?
        if (numParticlesAlive != customData.Count) Debug.Log(numParticlesAlive);

        //Loop through alive particle
        for (int i = 0; i < numParticlesAlive; i++) {
            Vector3 targetPoint = new Vector3(customData[i].x, customData[i].y, customData[i].z);
            Vector3 currentPosition = particles[i].position;

            //If it's close to the target point, change target to next point and set new speed vector
            float distanceLeft = Vector3.Distance(currentPosition, targetPoint);
            if (distanceLeft >= PopulateStreamLines.distanceMax_CACA) { //Lost particle
                particles[i].remainingLifetime = 0;                
            }

            else if (distanceLeft < 0.1f) {
                //Set new target
                int newTargetPointIndex = (int)(customData[i].w + 1);

                //Last target reached = kill particle
                if (newTargetPointIndex >= PopulateStreamLines.positions_TEST.Count) {
                    particles[i].remainingLifetime = 0;
                    continue;
                }

                Vector3 newTargetPoint = new Vector3(
                    PopulateStreamLines.positions_TEST[newTargetPointIndex].x,
                    PopulateStreamLines.positions_TEST[newTargetPointIndex].y,
                    PopulateStreamLines.positions_TEST[newTargetPointIndex].z
                    );
                
                customData[i] = new Vector4(
                    newTargetPoint.x,
                    newTargetPoint.y,
                    newTargetPoint.z,                    
                    newTargetPointIndex
                );

                //Apply custom data
                particleSystem.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);

                //Set new speed vector
                float speed = Vector3.Distance(targetPoint, newTargetPoint) * speedFactor;
                particles[i].velocity = speed * (newTargetPoint - currentPosition).normalized; //Vector3.RotateTowards(currentPosition, newTargetPoint, 2*Mathf.PI, float.MaxValue);                
            }
        }

        // Apply the particle changes to the particle system
        particleSystem.SetParticles(particles, numParticlesAlive);
    }
#endif
}
