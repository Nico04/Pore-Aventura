using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class TracerInjectionGridGpuBuilder : Builder {
	private const int AnimationSpeed = 50;

	private VisualEffect _visualEffect;

	protected override void Start() {
		base.Start();
		_visualEffect = GetComponent<VisualEffect>();
	}

	private Texture2D _positionsTexture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	private Texture2D _colorsTexture;    //We need to keep a ref to the texture because SetTexture only make a binding.

	protected override async Task Build(CancellationToken cancellationToken) {
		var trajectories = await TrajectoriesManager.Instance.GetInjectionGridTrajectories(cancellationToken).ConfigureAwait(true);
		if (trajectories.Length == 0) {
			_visualEffect.Reinit();
			return;
		}

		int tracerSpacing = Mathf.Max((int)(TrajectoriesManager.Instance.SpawnDelay / 1000f * AnimationSpeed), 1);
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

		/** Loop on points indices then on trajectories */
		await Task.Run(() => {
			var longEnoughTraj = trajectories;
			int longEnoughTrajCount = 0;
			int maxPointsInOneTraj = trajectories.Max(tr => tr.Points.Length) / tracerSpacing * tracerSpacing;
			int tracersSum = 0;
			for (int p = 0; p < maxPointsInOneTraj; p++) {
				if (p % tracerSpacing == 0) {
					tracersSum += longEnoughTrajCount;
					longEnoughTraj = longEnoughTraj.Where(t => p < (int) (t.Points.Length / tracerSpacing) * tracerSpacing).ToArray();
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
		}, cancellationToken).ConfigureAwait(true);

		//Apply textures modif
		_positionsTexture.Apply();
		_colorsTexture.SetPixels32(colorsTextureData);
		_colorsTexture.Apply();

		//Apply value to VFX
		_visualEffect.Reinit();     //Reset vfx otherwise all particules are mixed up between trajectories (colors are mixed)
		_visualEffect.SetUInt("TracersCount", Convert.ToUInt32(tracersCount));
		_visualEffect.SetUInt("PositionsCount", Convert.ToUInt32(positionsCount));
		_visualEffect.SetUInt("AnimationSpeed", Convert.ToUInt32(AnimationSpeed));
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

	public int GetTotalParticlesCount() => _visualEffect != null ? _visualEffect.aliveParticleCount : 0;

	public void Pause(bool pause) {
		if (_visualEffect != null)
			_visualEffect.pause = pause;
	}
}
