using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math.Geometry;
using UnityEngine;
using UnityEngine.UI;

public class TrajectoriesManager : MonoBehaviour {
	public static TrajectoriesManager Instance;

	public int Resolution = 15;
	public int SpawnDelay = 500;    //in ms
	public float TrajectoriesStep = 0.1f;
	public float Size = 10f;

	public Text ShowVariableText;
	public BuildMicrostructure BuildMicrostructure;
	public GridSpawner GridSpawner;
	public BuildStreamLines PopulateStreamLines;


	private Trajectory[] _trajectories;
	public Trajectory[] Trajectories => _trajectories ?? (_trajectories = BuildTrajectories());


	private float? _trajectoriesMaxDistance;
	public float TrajectoriesMaxDistance => _trajectoriesMaxDistance ?? (_trajectoriesMaxDistance = Trajectories.Max(t => t.Distances.Max())).GetValueOrDefault();


	private float? _trajectoriesAverageDistance;
	public float TrajectoriesAverageDistance => _trajectoriesAverageDistance ?? (_trajectoriesAverageDistance = Trajectories.Average(t => t.Distances.Average())).GetValueOrDefault();

	public void AskRebuildTrajectories() {
		_trajectories = null;
		_trajectoriesMaxDistance = null;
		_trajectoriesAverageDistance = null;
	} 

	public float Spacing => Size / Resolution;

	private void Start() {
		if (Instance != null) throw new InvalidOperationException();
		Instance = this;

		Size = BuildMicrostructure.Size.y;
		UpdateVariableText();
	}
	
	private Trajectory[] BuildTrajectories() {
		var trajectories = new List<Trajectory>();

		//Loop through all injection point
		for (float y = 0.1f; y <= Size; y += Spacing) {
			for (float z = 0.1f; z <= Size; z += Spacing) {
				//Build one trajectory
				var trajectory = BuildTrajectory(new Vector3(GridSpawner.gameObject.transform.position.x, y, z));

				//Add it to master array if it is not null
				if (trajectory != null)
					trajectories.Add(trajectory);
			}
		}

		//Compute stats
		//ComputeTrajectoriesStats();

		//Convert to array
		return trajectories.ToArray();
	}

	public Trajectory BuildTrajectory(Vector3 startPosition) {
		//Init list
		var trajectory = new List<Vector3> { startPosition };
		var currentPoint = trajectory[0];

		//Get speed at currentPoint
		Vector3 currentSpeed;
		while (!DataBase.IsSpeedTooLow(currentSpeed = DataBase.GetSpeedAtPoint(currentPoint))) {		//TODO use IsSpeedTooLow everywhere
			//Move to next point by going into the speed direction by a defined distance
			currentPoint += currentSpeed * TrajectoriesStep;

			//Add point to list
			trajectory.Add(currentPoint);
		}

		//Debug.Log(trajectory.Average(p => p.magnitude) + "|" + trajectory.Count);

		return trajectory.Count > 1 ? new Trajectory(trajectory.ToArray()) : null;
	}

	private void ComputeTrajectoriesStats() {
		float max = 0f;
		float sum = 0f;
		int quantity = 0;

		foreach (var trajectory in Trajectories) {
			for (int p = 1; p < trajectory.Points.Length; p++) {
				//Get distance
				var dist = Vector3.Distance(trajectory.Points[p], trajectory.Points[p - 1]);
				
				//Get max
				if (dist > max)
					max = dist;

				//Compute average
				sum += dist;
				quantity++;
			}
		}

		_trajectoriesMaxDistance = max;
		_trajectoriesAverageDistance = sum / quantity;
	}

	public Vector3 GetNextTrajectoryPosition(Vector3[] trajectory, ref int currentPositionIndex) => trajectory.ElementAtOrDefault(++currentPositionIndex);

	private StepRules _spawnDelayStepRules = new StepRules(new List<StepRange> { 
		new StepRange(0, 500, 100),
		new StepRange(500, 2000, 250),
		new StepRange(2000, int.MaxValue, 1000)
	});

	private StepRules _resolutionStepRules = new StepRules(new List<StepRange> {
		new StepRange(0, 50, 5),
		new StepRange(50, int.MaxValue, 10)
	});

	private bool _spawnDelayKeyIsDown = false;
	private bool _spawnResolutionKeyIsDown = false;
	private void OnGUI() {  //Called several times per frame
		bool hasChanged = false;
		if (!_spawnDelayKeyIsDown && Input.GetButtonDown("SpawnDelay")) {
			_spawnDelayKeyIsDown = true;
			SpawnDelay = _spawnDelayStepRules.StepValue(SpawnDelay, Input.GetAxis("SpawnDelay") > 0); //Math.Max(SpawnDelay + Mathf.RoundToInt(Input.GetAxis("SpawnDelay")) * 250, 0);
			hasChanged = true;
		} else if (_spawnDelayKeyIsDown && Input.GetButtonUp("SpawnDelay")) {
			_spawnDelayKeyIsDown = false;
		}

		if (!_spawnResolutionKeyIsDown && Input.GetButtonDown("SpawnResolution")) {
			_spawnResolutionKeyIsDown = true;
			Resolution = _resolutionStepRules.StepValue(Resolution, Input.GetAxis("SpawnResolution") > 0); //Math.Max(Resolution + Mathf.RoundToInt(Input.GetAxis("SpawnResolution")) * 5, 0);
			
			//Rebuild Trajectories 
			AskRebuildTrajectories();

			//re-Build visual grid
			GridSpawner.AskRebuild();

			//Re-build streamlines
			PopulateStreamLines.AskRebuild();

			hasChanged = true;
		} else if (_spawnResolutionKeyIsDown && Input.GetButtonUp("SpawnResolution")) {
			_spawnResolutionKeyIsDown = false;
		}

		if (hasChanged) {
			UpdateVariableText();
		}
	}

	private void UpdateVariableText() {
		ShowVariableText.text = $"Spawn Delay = {SpawnDelay}ms \t Spawn Resolution = {Resolution}x{Resolution}";
	}
}

public class Trajectory {
	public Vector3[] Points;
	public Vector3 StartPoint => Points[0];

	private float[] _distances;
	public float[] Distances => _distances ?? (_distances = BuildDistances());

	private Color? _color;
	public Color Color {
		get => _color.GetValueOrDefault();
		set {
			if (_color == value)
				return;

			_color = value;
			_particleMaterial = null;
		}
	}

	private Material _particleMaterial;
	public Material GetParticleMaterial(Material baseMaterial) {
		if (_color == null)
			return baseMaterial;

		if (_particleMaterial != null)
			return _particleMaterial;

		baseMaterial.color = Color;
		return _particleMaterial = baseMaterial;
	} 

	public Trajectory(Vector3[] points) {
		Points = points;
	}

	private float[] BuildDistances() {
		var distances = new float[Points.Length - 1];
		for (int i = 0; i < distances.Length; i++)
			distances[i] = Vector3.Distance(Points[i + 1], Points[i]);

		return distances;
	}
}

public class StepRules {
	public List<StepRange> Rules;

	private int _rangeMin;
	private int _rangeMax;
	public StepRules(List<StepRange> rules) {
		Rules = rules;
		_rangeMin = Rules.Min(r => r.Range.Start);
		_rangeMax = Rules.Max(r => r.Range.End);
	}

	public int StepValue(int currentValue, bool stepUp) {
		var selectFunc = new Func<StepRange, bool>(s => s.Range.Start <= currentValue && currentValue <= s.Range.End);
		int step = (stepUp ? Rules.Last(selectFunc) : Rules.First(selectFunc)).Step;
		return Clamp(currentValue + step * (stepUp ? 1 : -1));
	}

	private int Clamp(int value) {
		if (value < _rangeMin) return _rangeMin;
		if (value > _rangeMax) return _rangeMax;
		return value;
	}
}

public class StepRange {
	public RangeInt Range;
	public int Step;

	public StepRange(RangeInt range, int step) {
		Range = range;
		Step = step;
	}

	public StepRange(int rangeStart, int rangeEnd, int step) {
		Range = new RangeInt() {
			Start = rangeStart,
			End = rangeEnd
		};
		Step = step;
	}
}

public struct RangeInt {
	public int Start;
	public int End;
}
