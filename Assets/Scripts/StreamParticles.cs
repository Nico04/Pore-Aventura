using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class StreamParticles : MonoBehaviour {
    public GameObject SpawnObject;
    public GameObject Spawner;

	private void Start() {
        _elapsedSinceLastSpawn.Start();
	}

    private bool _keyIsDown = false;
    private void OnGUI() {
        if (!_keyIsDown && Input.GetButtonDown("Shoot") && !PauseManager.IsPaused) {        
            _keyIsDown = true;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                CreateNewSpawner(Spawner.transform.position);
            else
                CreateNewParticle(Spawner.transform.position);
        }
        else if (_keyIsDown && Input.GetButtonUp("Shoot")) {
            _keyIsDown = false;
        }
    }

    private readonly List<Trajectory> _spawners = new List<Trajectory>();
    private readonly Stopwatch _elapsedSinceLastSpawn = new Stopwatch();
	private void Update() {
        if (PauseManager.IsPaused)
            return;

	    //Wait until spawn delay is elapsed
	    if (TrajectoriesManager.Instance.SpawnDelay <= 0 || _elapsedSinceLastSpawn.ElapsedMilliseconds < TrajectoriesManager.Instance.SpawnDelay)
	        return;

        //Spawn all
		foreach (var trajectory in _spawners) {
		    //var particle = CreateNewParticle(trajectory);
		    //particle.GetComponent<Renderer>().material.color = spawner.color;

			CreateNewParticle(trajectory);
		}

	    //Reset timer
	    _elapsedSinceLastSpawn.Restart();
	}

    private void CreateNewSpawner(Vector3 position) {
		/**
        _spawners.Add(new Spawner {
            position = position,
            color = Color.HSVToRGB(Random.value, 1f, 1f)
		});*/

		//Create new trajectory
	    var trajectory = TrajectoriesManager.Instance.BuildTrajectory(position);
	    if (trajectory == null) return;

		//Set color
	    trajectory.Color = Color.HSVToRGB(Random.value, 1f, 1f);

		//Add to spawner list
		_spawners.Add(trajectory);
    }

	private void CreateNewParticle(Vector3 position) {
		//Create new trajectory
		var trajectory = TrajectoriesManager.Instance.BuildTrajectory(position);
		if (trajectory == null) return;

		CreateNewParticle(trajectory);
    }

    private void CreateNewParticle(Trajectory trajectory) {
	    //return Instantiate(SpawnObject, position, Quaternion.identity, transform);

	    GridSpawner.CreateNewParticle(SpawnObject, trajectory, transform);
    }

    public void DeleteSpawners() {
        _spawners.Clear();
    }
}

//public struct Spawner {
//    public Vector3 position;
//    public Color color;
//}
