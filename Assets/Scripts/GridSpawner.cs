using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class GridSpawner : MonoBehaviour {
	public GameObject SpawnObject;

	private bool _askRebuild = true;
	public void AskRebuild() {
		_askRebuild = true;
	}

	private readonly System.Diagnostics.Stopwatch _elapsedSinceLastSpawn = new System.Diagnostics.Stopwatch();


	private void Start () {
		_elapsedSinceLastSpawn.Start();
	}
	
	// Update is called once per frame
	private void Update () {
#if DEBUG
		//Apply limit to particles count
		//if (transform.childCount > 5000)
		//	return;
#endif

		if (PauseManager.IsPaused)
			return;

		//Build visual grid
		if (_askRebuild) {
			SetTrajectoriesColor();
			BuildStaticGrid();
			_askRebuild = false;
		}

		//Wait until spawn delay is elapsed
		if (TrajectoriesManager.Instance.SpawnDelay <= 0 || _elapsedSinceLastSpawn.ElapsedMilliseconds < TrajectoriesManager.Instance.SpawnDelay) 
			return;

		//Spawn loop
		SpawnAll(true);

		//Reset timer
		_elapsedSinceLastSpawn.Restart();
	}

	private List<GameObject> staticParticles = new List<GameObject>();
	private void BuildStaticGrid() => SpawnAll(false);

	private void SetTrajectoriesColor() {
		foreach (var trajectory in TrajectoriesManager.Instance.Trajectories) {
			trajectory.Color = Color.HSVToRGB(trajectory.StartPoint.y / TrajectoriesManager.Instance.Size, 1f, MinColorValue + trajectory.StartPoint.z / TrajectoriesManager.Instance.Size * (1f - MinColorValue));
		}
	}

	private const float MinColorValue = 0.4f;
	private void SpawnAll(bool movingParticles) {
		if (TrajectoriesManager.Instance.Resolution == 0)
			return;

		//if it's static particles, removes previous ones
		if (!movingParticles) {
			staticParticles.ForEach(Destroy);
			staticParticles.Clear();
		}

		//Spawn loop
		/** Old 
		for (float y = 0.1f; y <= TrajectoriesManager.Size; y += TrajectoriesManager.Spacing) {
			for (float z = 0.1f; z <= TrajectoriesManager.Size; z += TrajectoriesManager.Spacing) {
				GameObject particle = Instantiate(SpawnObject, new Vector3(transform.position.x, y, z), Quaternion.identity, transform);
				particle.GetComponent<Renderer>().material.color = Color.HSVToRGB(y / TrajectoriesManager.Size, 1f, MinColorValue + z / TrajectoriesManager.Size * (1f - MinColorValue) );
				//particle.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(y / _gridSize, 1f, MinColorValue + z / _gridSize * (1f - MinColorValue) );

				if (!movingParticles) {
					particle.GetComponent<FollowStream>().enabled = false;
					staticParticles.Add(particle);
				}
			}
		}*/

		foreach (var trajectory in TrajectoriesManager.Instance.Trajectories) {
			/**
			//Create new instance a particle
			var particle = Instantiate(SpawnObject, trajectory.StartPoint, Quaternion.identity, transform);

			//Store his trajectory
			particle.GetComponent<FollowStream>().Trajectory = trajectory;

			//Change color
			particle.GetComponent<Renderer>().material = trajectory.GetParticleMaterial(particle.GetComponent<Renderer>().material);
			*/

			//Create new particle
			var particle = CreateNewParticle(SpawnObject, trajectory, transform);

			//static particle case
			if (!movingParticles) {
				particle.GetComponent<FollowStream>().enabled = false;
				staticParticles.Add(particle);
			}
		}
	}

	private static MaterialPropertyBlock _materialPropertyBlock;
	private static MaterialPropertyBlock MaterialPropertyBlock => _materialPropertyBlock ?? (_materialPropertyBlock = new MaterialPropertyBlock());

	public static GameObject CreateNewParticle(GameObject spawnObject, Trajectory trajectory, Transform parent) {
		//Create new instance a particle
		var particle = Instantiate(spawnObject, trajectory.StartPoint, Quaternion.identity, parent);
		
		//Store his trajectory
		particle.GetComponent<FollowStream>().Trajectory = trajectory;

		//Change color
		//particle.GetComponent<Renderer>().material = trajectory.GetParticleMaterial(particle.GetComponent<Renderer>().material);

		var renderer = particle.GetComponent<Renderer>();
		renderer.GetPropertyBlock(MaterialPropertyBlock);
		MaterialPropertyBlock.SetColor("_Color", trajectory.Color);
		renderer.SetPropertyBlock(MaterialPropertyBlock);

		return particle;
	}
}
