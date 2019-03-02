using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TrajectoriesManager : MonoBehaviour {
	public static TrajectoriesManager Instance;

	public int Resolution = 15;
	public int SpawnDelay = 500;    //in ms
	public float TrajectoriesStep = 0.1f;

	public Text ShowVariableText;
	public List<Builder> BuildersToUpdateOnResolutionChange = new List<Builder>();
	public TracerInjectionGridGpuBuilder TracerInjectionGridGpuBuilder;

	private float? _trajectoriesMaxDistance;
	public float TrajectoriesDistanceMax => _trajectoriesMaxDistance ?? (_trajectoriesMaxDistance = _trajectories.Max(t => t.Distances.Max())).GetValueOrDefault();


	private float? _trajectoriesAverageDistance;
	public float TrajectoriesAverageDistance => _trajectoriesAverageDistance ?? (_trajectoriesAverageDistance = _trajectories.Average(t => t.Distances.Average())).GetValueOrDefault();

	public float SpawnRate => 1f / (SpawnDelay / 1000f);        //In particle per second

	public void AskRebuildTrajectories() {
		_trajectories = null;
		_trajectoriesMaxDistance = null;
		_trajectoriesAverageDistance = null;
	}

	public float Spacing => DataBase.DataSpaceSize.y / Resolution;

	private void Start() {
		if (Instance != null) throw new InvalidOperationException();
		Instance = this;

		UpdateVariableText();
	}

	private Trajectory[] _trajectories;
	private Task<Trajectory[]> _currentBuildTask;
	public async Task<Trajectory[]> GetInjectionGridTrajectories(CancellationToken cancellationToken) {
		//Just return the array if it is already built
		if (_trajectories != null)
			return _trajectories;

		try {
			//If no build is currently running, start the building process
			if (_currentBuildTask == null) 
				_currentBuildTask = Task.Run(() => BuildInjectionGridTrajectories(), cancellationToken);

			//Wait the process to finish
			return (_trajectories = await _currentBuildTask.ConfigureAwait(false));
		} finally {
			_currentBuildTask = null;
		}
	}

	private Trajectory[] BuildInjectionGridTrajectories() {
		var trajectories = new List<Trajectory>();

		//Loop through all injection point
		for (float y = Spacing / 2; y <= DataBase.DataSpaceSize.y; y += Spacing) {
			for (float z = Spacing / 2; z <= DataBase.DataSpaceSize.z; z += Spacing) {
				//Build one trajectory
				var trajectory = BuildTrajectory(new Vector3(0.1f, y, z), Trajectory.Types.InjectionGrid);

				//Add it to master array if it is not null
				if (trajectory != null)
					trajectories.Add(trajectory);
			}
		}

		var trajectoriesArray = trajectories.ToArray();

		//Compute stats
		//ComputeTrajectoriesStats(trajectoriesArray);

		//Convert to array
		return trajectoriesArray;
	}

	//private System.Diagnostics.Stopwatch _debugStopwatch;

	public Trajectory BuildTrajectory(Vector3 startPosition, Trajectory.Types type, Color? color = null) {
		//Init list
		var trajectory = new List<Vector3> { startPosition };
		var currentPoint = trajectory[0];

		//Get speed at currentPoint
		Vector3 currentSpeed;
		while (!DataBase.IsVelocityTooLow(currentSpeed = DataBase.GetInterpolatedVelocityAtPosition(currentPoint))) {
			//TODO use IsSpeedTooLow everywhere
			//Move to next point by going into the speed direction by a defined distance
			currentPoint += currentSpeed * TrajectoriesStep;

			//Add point to list
			trajectory.Add(currentPoint);
		}

		return trajectory.Count > 1 ? new Trajectory(trajectory.ToArray(), type, color) : null;
	}

	private void ComputeTrajectoriesStats(Trajectory[] trajectories) {
		float max = 0f;
		float sum = 0f;
		int quantity = 0;

		foreach (var trajectory in trajectories) {
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

	private readonly StepRules _spawnDelayStepRules = new StepRules(new List<StepRange> {
		new StepRange(0, 500, 100),
		new StepRange(500, 2000, 250),
		new StepRange(2000, int.MaxValue, 1000)
	});

	private readonly StepRules _resolutionStepRules = new StepRules(new List<StepRange> {
		new StepRange(5, 50, 5),
		new StepRange(50, int.MaxValue, 10)
	});

	private bool _spawnDelayKeyIsDown = false;
	private bool _spawnResolutionKeyIsDown = false;
	private void OnGUI() {  //Called several times per frame
		//Exit if it is not the right event type
		if (Event.current.type != EventType.KeyDown && Event.current.type != EventType.KeyUp)
			return;

		bool hasChanged = false;
		if (!_spawnDelayKeyIsDown && Input.GetButtonDown("SpawnDelay")) {
			_spawnDelayKeyIsDown = true;
			var oldDelay = SpawnDelay;
			SpawnDelay = _spawnDelayStepRules.StepValue(SpawnDelay, Input.GetAxis("SpawnDelay") > 0);

			if (oldDelay != SpawnDelay) {
				//Update SpawnDelay for vfx
				//GridSpawnerVfx.AskUpdateSpawnDelay();

				//TracerInjectionGridGpuBuilder need to be rebuilt when SpawnDelay is changed
				TracerInjectionGridGpuBuilder.AskRebuild();

				hasChanged = true;
			}
		} else if (_spawnDelayKeyIsDown && Input.GetButtonUp("SpawnDelay")) {
			_spawnDelayKeyIsDown = false;
		}

		if (!_spawnResolutionKeyIsDown && Input.GetButtonDown("SpawnResolution")) {
			_spawnResolutionKeyIsDown = true;
			var oldResolution = Resolution;
			Resolution = _resolutionStepRules.StepValue(Resolution, Input.GetAxis("SpawnResolution") > 0);

			if (Resolution != oldResolution) {
				//Rebuild Trajectories 
				AskRebuildTrajectories();

				//Rebuild builders
				BuildersToUpdateOnResolutionChange.ForEach(b => b.AskRebuild());

				hasChanged = true;
			}
		} else if (_spawnResolutionKeyIsDown && Input.GetButtonUp("SpawnResolution")) {
			_spawnResolutionKeyIsDown = false;
		}

		if (hasChanged)
			UpdateVariableText();
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

	public enum Types { InjectionGrid, Manual }
	public Types Type; 

	private Color? _color;
	public Color Color {
		get {
			if (_color == null) {
				if (Type == Types.Manual) {
					_color = Color.HSVToRGB(0.6f, 1f, 1f);
				} else if (Type == Types.InjectionGrid) {
					/** Basic color algo
					 trajectory.Color = Color.HSVToRGB(
						trajectory.StartPoint.y / TrajectoriesManager.Instance.Size, 
						1f, 
						MinColorValue + trajectory.StartPoint.z / TrajectoriesManager.Instance.Size * (1f - MinColorValue)
					);*/

					/** Advanced color algo by Mathieu Souzy */
					const float yMin = 0f;
					var yMax = DataBase.DataSpaceSize.y;
					const int N = 1;            //Color repetition cycle number. C'est le paramètre qui permet de choisir si on veut explorer N fois la gamme des H, pour éviter de faire des sauts de couleurs bizarre.
					const float zMin = 0f;
					var zMax = DataBase.DataSpaceSize.z;
					const float paramS = 0.25f;     //S permet d'explorer l'échelle de couleur de blanc à la couleur correspondant au H que t'as choisi
					const float paramV = 1.25f;     //V permet d'explorer l'échelle de couleur de noir à la couleur correspondant au H que t'as choisi
					var yCenter = (yMax + yMin) / 2;
					var zCenter = (zMax + zMin) / 2;

					_color = Color.HSVToRGB(
						Tools.TrueModulo(StartPoint.y - yCenter, (yMax - yMin) / N) * N / (yMax - yMin),
						Math.Min(1f, (1 - paramS) * (StartPoint.z - zMin) / (zCenter - zMin) + paramS),     //la formule pour S consiste a varier S linéairement sur la gamme [minS;1] sur [0;zMax/2], puis pour z>zMax/2, S=1
						1 + Math.Min(0f, paramV * (zCenter - StartPoint.z) / (zMax - zMin))                 //la formule pour V consiste à avoir V = 1 sur[0; zMax / 2], puis varier V linéairement sur la gamme[1; minV]  pour[zMax / 2; zMax].
					);
				}
			}

			return _color.GetValueOrDefault();
		}
		set {
			if (_color == value)
				return;

			_color = value;
			//_particleMaterial = null;
		}
	}

	/** usefull ?
	private Material _particleMaterial;
	public Material GetParticleMaterial(Material baseMaterial) {
		if (_color == null)
			return baseMaterial;

		if (_particleMaterial != null)
			return _particleMaterial;

		baseMaterial.color = Color;
		return _particleMaterial = baseMaterial;
	}*/

	public Trajectory(Vector3[] points, Types type, Color? color = null) {
		Points = points;
		Type = type;
		_color = color;
	}

	private float[] BuildDistances() {
		var distances = new float[Points.Length - 1];
		for (int i = 0; i < distances.Length; i++)
			distances[i] = Vector3.Distance(Points[i], Points[i + 1]);

		return distances;
	}
}

public class StepRules {
	private readonly List<StepRange> _rules;
	private readonly int _rangeMin;
	private readonly int _rangeMax;

	public StepRules(List<StepRange> rules) {
		_rules = rules;
		_rangeMin = _rules.Min(r => r.Range.Start);
		_rangeMax = _rules.Max(r => r.Range.End);
	}

	public int StepValue(int currentValue, bool stepUp) {
		var selectFunc = new Func<StepRange, bool>(s => s.Range.Start <= currentValue && currentValue <= s.Range.End);
		int step = (stepUp ? _rules.Last(selectFunc) : _rules.First(selectFunc)).Step;
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
