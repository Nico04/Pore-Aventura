using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TracerInjectionGridBuilder : Builder {
	public GameObject SpawnObject;

	private readonly Stopwatch _elapsedSinceLastSpawn = new System.Diagnostics.Stopwatch();
	protected override void Start() {
		base.Start();
		_elapsedSinceLastSpawn.Start();
	}
	
	// Update is called once per frame
	protected override void Update () {
		base.Update();

		if (PauseManager.IsPaused)
			return;

		//Wait until spawn delay is elapsed
		if (TrajectoriesManager.Instance.SpawnDelay <= 0 || _elapsedSinceLastSpawn.ElapsedMilliseconds < TrajectoriesManager.Instance.SpawnDelay) 
			return;

		//Spawn loop
		SpawnAll(true);

		//Reset timer
		_elapsedSinceLastSpawn.Restart();
	}

	protected override async Task Build(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		await Task.Run(action: SetTrajectoriesColor, cancellationToken: cancellationToken).ConfigureAwait(true);

		cancellationToken.ThrowIfCancellationRequested();
		BuildStaticGrid();
	}

	protected override void SetVisibility(bool isVisible) {
		BasicDispatcher.RunOnMainThread(() =>
			gameObject.SetActive(!gameObject.activeInHierarchy)        //SetActive must be called in the Update() and NOT in OnGUI()
		);
	}

	private List<GameObject> staticParticles = new List<GameObject>();
	private void BuildStaticGrid() => SpawnAll(false);

	public static void SetTrajectoriesColor() {
		foreach (var trajectory in TrajectoriesManager.Instance.Trajectories) {
			/**
			 trajectory.Color = Color.HSVToRGB(
				trajectory.StartPoint.y / TrajectoriesManager.Instance.Size, 
				1f, 
				MinColorValue + trajectory.StartPoint.z / TrajectoriesManager.Instance.Size * (1f - MinColorValue)
			);*/
			
			var Ymin = 0f;
			var Ymax = TrajectoriesManager.Instance.Size.y;
			var N = 5;		//Color repetition cycle number

			/**
			trajectory.Color = Color.HSVToRGB(
				((trajectory.StartPoint.y - Ymin) % ((Ymax - Ymin) / N)) / (Ymax - Ymin),
				(trajectory.StartPoint.y - Ymin) / (Ymax - Ymin),
				MinColorValue + trajectory.StartPoint.z / TrajectoriesManager.Instance.Size * (1f - MinColorValue)
			);
			*/

			var Zmin = 0f;
			var Zmax = TrajectoriesManager.Instance.Size.y;
			var paramS = 0.25f;
			var paramV = 1.25f;
			var Yc = (Ymax - Ymin) / 2;
			var Zc = (Zmax - Zmin) / 2;
			N = 1;
			trajectory.Color = Color.HSVToRGB(
				((trajectory.StartPoint.y - Yc) % ((Ymax - Ymin) / N)) * N / (Ymax - Ymin),
				Math.Min(1, (1 - paramS) * (trajectory.StartPoint.z - Zmin) / (Zc - Zmin) + paramS),
				1 + Math.Min(0, paramV * (Zc - trajectory.StartPoint.z) / (Zmax - Zmin))
			);
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
