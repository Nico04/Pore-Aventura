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
	protected override async void Update () {
		base.Update();

		if (PauseManager.IsPaused)
			return;

		//Wait until spawn delay is elapsed
		if (TrajectoriesManager.Instance.SpawnDelay <= 0 || _elapsedSinceLastSpawn.ElapsedMilliseconds < TrajectoriesManager.Instance.SpawnDelay) 
			return;

		//Spawn loop
		await SpawnAll(true, CancellationToken.None).ConfigureAwait(false);

		//Reset timer
		_elapsedSinceLastSpawn.Restart();
	}

	protected override async Task Build(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		await BuildStaticGrid(cancellationToken).ConfigureAwait(false);
	}

	private readonly List<GameObject> _staticParticles = new List<GameObject>();
	private async Task BuildStaticGrid(CancellationToken cancellationToken) => await SpawnAll(false, cancellationToken).ConfigureAwait(false);

	/** Old method
	public static void SetTrajectoriesColor() {
		foreach (var trajectory in TrajectoriesManager.Instance.Trajectories) {
			// trajectory.Color = Color.HSVToRGB(
			//	trajectory.StartPoint.y / TrajectoriesManager.Instance.Size, 
			//	1f, 
			//	MinColorValue + trajectory.StartPoint.z / TrajectoriesManager.Instance.Size * (1f - MinColorValue)
			//);
			
			var Ymin = 0f;
			var Ymax = DataBase.DataSpaceSize.y;
			var N = 5;		//Color repetition cycle number

			//trajectory.Color = Color.HSVToRGB(
			//	((trajectory.StartPoint.y - Ymin) % ((Ymax - Ymin) / N)) / (Ymax - Ymin),
			//	(trajectory.StartPoint.y - Ymin) / (Ymax - Ymin),
			//	MinColorValue + trajectory.StartPoint.z / TrajectoriesManager.Instance.Size * (1f - MinColorValue)
			//);
			

			var Zmin = 0f;
			var Zmax = DataBase.DataSpaceSize.z;
			var paramS = 0.25f;
			var paramV = 1.25f;
			var Yc = (Ymax + Ymin) / 2;
			var Zc = (Zmax + Zmin) / 2;
			N = 1;
			trajectory.Color = Color.HSVToRGB(
				((trajectory.StartPoint.y - Yc) % ((Ymax - Ymin) / N)) * N / (Ymax - Ymin),
				Math.Min(1f, (1 - paramS) * (trajectory.StartPoint.z - Zmin) / (Zc - Zmin) + paramS),
				1 + Math.Min(0f, paramV * (Zc - trajectory.StartPoint.z) / (Zmax - Zmin))
			);
		}
	}*/

	private const float MinColorValue = 0.4f;
	private async Task SpawnAll(bool movingParticles, CancellationToken cancellationToken) {
		if (TrajectoriesManager.Instance.Resolution == 0)
			return;

		cancellationToken.ThrowIfCancellationRequested();

		//if it's static particles, removes previous ones
		if (!movingParticles) {
			_staticParticles.ForEach(Destroy);
			_staticParticles.Clear();
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

		var trajectories = await TrajectoriesManager.Instance.GetInjectionGridTrajectories(CancellationToken.None).ConfigureAwait(true);
		foreach (var trajectory in trajectories) {
			cancellationToken.ThrowIfCancellationRequested();
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
				_staticParticles.Add(particle);
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
