using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class TracerInjectionGridGpuBuilder : Builder {

	private bool _askUpdateSpawnDelay = true;
	public void AskUpdateSpawnDelay() => _askUpdateSpawnDelay = true;
	private int _tracerSpacing;
	private int _animationSpeed = 50;
	//public int VFXRefreshFrequency = 10;

	private VisualEffect _visualEffect;

	protected override void Start() {
		base.Start();
		_visualEffect = GetComponent<VisualEffect>();
	}

	protected override void Update() {
		base.Update();

		if (PauseManager.IsPaused)
			return;

		if (_askUpdateSpawnDelay) {
			UpdateSpawnDelay();
			_askUpdateSpawnDelay = false;
		}
	}

	private Texture2D _positionsTexture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	private Texture2D _colorsTexture;    //We need to keep a ref to the texture because SetTexture only make a binding.

	protected override async Task Build(CancellationToken cancellationToken) {
		var trajectories = await TrajectoriesManager.Instance.GetInjectionGridTrajectories(cancellationToken).ConfigureAwait(true);
		if (trajectories.Length == 0) {
			_visualEffect.Reinit();
			return;
		}

		int tracerSpacing = _tracerSpacing;
		int tracersCount = trajectories.Sum(t => (int)(t.Points.Length / tracerSpacing));
		if (tracersCount <= 0) {
			_visualEffect.Reinit();
			return;
		}

		int positionsCount = tracersCount * tracerSpacing;
		int textureWidth = Mathf.CeilToInt(Mathf.Sqrt(positionsCount));

		_positionsTexture = new Texture2D(textureWidth, textureWidth, TextureFormat.RGBAFloat, false) {
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};

		_colorsTexture = new Texture2D(textureWidth, textureWidth, TextureFormat.RGB24, false) {
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};

		var positionsTextureData = _positionsTexture.GetRawTextureData<Vector4>();
		var colorsTextureData = _colorsTexture.GetPixels32();

		//Apply value to VFX
		_visualEffect.Reinit();     //Reset vfx otherwise all particules are mixed up between trajectories (colors are mixed)
		_visualEffect.SetUInt("TracersCount", Convert.ToUInt32(tracersCount));
		_visualEffect.SetUInt("PositionsCount", Convert.ToUInt32(positionsCount));
		_visualEffect.SetUInt("AnimationSpeed", Convert.ToUInt32(_animationSpeed));

		int tracersSum = 0;

		/** Loop on points indices then on trajectories */
		var longEnoughTraj = trajectories;
		int longEnoughTrajCount = 0;
		int maxPointsInOneTraj = trajectories.Max(tr => tr.Points.Length) / tracerSpacing * tracerSpacing;

		for (int p = 0; p < maxPointsInOneTraj; p++) {
			if (p % tracerSpacing == 0) {
				tracersSum += longEnoughTrajCount;
				longEnoughTraj = longEnoughTraj.Where(t => p < (int)(t.Points.Length / tracerSpacing) * tracerSpacing).ToArray();
				longEnoughTrajCount = longEnoughTraj.Length;
			}

			// Loop on trajectories containing at least p indices
			for (int t = 0; t < longEnoughTrajCount; t++) {
				var traj = longEnoughTraj[t];
				var point = traj.Points[p];

				int pixelIndex = t + tracersSum + (p % tracerSpacing) * tracersCount;
				positionsTextureData[pixelIndex] = point;
				colorsTextureData[pixelIndex] = traj.Color;
			}
		}

		_positionsTexture.Apply();
		_colorsTexture.SetPixels32(colorsTextureData);
		_colorsTexture.Apply();

		//Apply value to VFX
		//_visualEffect.Reinit();
		_visualEffect.SetTexture("Positions", _positionsTexture);
		_visualEffect.SetTexture("Colors", _colorsTexture);

		/** Loop on trajectories then on points (minimum iterations number)
        for (int m = 0; m < trajectories.Length; m += trajectories.Length / VFXRefreshPeriod)
        {
            await Task.Run(() =>
            {
                for (int t = m; t < m + trajectories.Length / VFXRefreshPeriod; t++)
                {
                    var traj = trajectories[t];
                    int tracersInTraj = (int)(traj.Points.Length / tracerSpacing);
                    int pointsInTraj = tracersInTraj * tracerSpacing;

                    for (int p = 0; p < pointsInTraj; p++)
                    {
                        var point = traj.Points[p];

                        int pixelIndex = (int)(p / tracerSpacing) + tracersSum + (p % tracerSpacing) * tracersCount;
                        positionsTextureData[pixelIndex] = point;
                        colorsTextureData[pixelIndex] = Color.HSVToRGB(traj.Color.r, traj.Color.g, traj.Color.b);
                    }

                    tracersSum += tracersInTraj;
                }
            }, cancellationToken).ConfigureAwait(true);

            _positionsTexture.Apply();
            _colorsTexture.SetPixels32(colorsTextureData);
            _colorsTexture.Apply();

            //Apply value to VFX
            //_visualEffect.Reinit();
            _visualEffect.SetTexture("Positions", _positionsTexture);
            _visualEffect.SetTexture("Colors", _colorsTexture);
        }*/
	}

	/** Old Method
	private Texture2D _texture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	protected override async Task Build(CancellationToken cancellationToken) {
		var trajectories = TrajectoriesManager.Instance.Trajectories;
		_texture = new Texture2D(trajectories.Max(t => t.Points.Length), trajectories.Length, TextureFormat.RGBAFloat, false);

		var textureData = _texture.GetRawTextureData<Vector4>();

		for (int y = 0; y < _texture.height; y++) {
			var trajectory = trajectories[y];
			for (int x = 0; x < trajectory.Points.Length; x++) {
				Vector4 pixel = trajectory.Points[x];
				pixel.w = trajectory.Points.Length;     //Store length of the trajectory in the alpha channel to be used in the vfx

				textureData[y * _texture.width + x] = pixel;
			}
		}

		_texture.Apply();

		//Apply value to VFX
		_visualEffect.Reinit();		//Reset vfx otherwise all particules are mixed up between trajectories (colors are mixed)
		_visualEffect.SetUInt("TextureWidth", Convert.ToUInt32(_texture.width));
		_visualEffect.SetUInt("TrajectoriesCount", Convert.ToUInt32(trajectories.Length));
		_visualEffect.SetTexture("Trajectories", _texture);
	}*/

	//Scale an input value that goes between 0 and max, to be in range 0 to 1.
	private float ScaleToRange01(float value, float max) => value / max;
	private static float PositionToColor(float position) => position * 1f / 20;

	// TODO verify the rule between spawn delay, spacing and animation speed
	private void UpdateSpawnDelay() => _tracerSpacing = Mathf.Max((int)(TrajectoriesManager.Instance.SpawnDelay / 1000f * _animationSpeed), 1); // _visualEffect.SetFloat("SpawnDelay", TrajectoriesManager.Instance.SpawnDelay / 1000f);

	public int GetTotalParticlesCount() => _visualEffect != null ? _visualEffect.aliveParticleCount : 0;

	public void Pause(bool pause) {
		if (_visualEffect != null)
			_visualEffect.pause = pause;
	}
}
