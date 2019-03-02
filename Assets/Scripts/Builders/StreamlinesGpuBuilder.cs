using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class StreamlinesGpuBuilder : Builder {
	private VisualEffect _visualEffect;

	protected override void Start() {
		base.Start();
		_visualEffect = GetComponent<VisualEffect>();
	}

	private const int SampleValue = 4;
	private Texture2D _positionsTexture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	private Texture2D _colorsTexture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	private Texture2D _alphasTexture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	protected override async Task Build(CancellationToken cancellationToken) {
		//Get trajectories
		var trajectories = await TrajectoriesManager.Instance.GetInjectionGridTrajectories(cancellationToken).ConfigureAwait(true);

		//Create textures
		//Unity max texture width or height is 16k : cannot use a mono-line texture.
		int size = (int)Math.Ceiling(Math.Sqrt(trajectories.Sum(t => Math.Ceiling((float)t.Points.Length / SampleValue))));
		_positionsTexture = new Texture2D(size, size, TextureFormat.RGBAFloat, false) {
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp		//Important for vfx index access
		};

		_colorsTexture = new Texture2D(size, size, TextureFormat.RGB24, false) {
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp        //Important for vfx index access
		};

		_alphasTexture = new Texture2D(size, size, TextureFormat.RFloat, false) {
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp        //Important for vfx index access
		};

		//Get textures data
		var positionsTextureData = _positionsTexture.GetRawTextureData<Vector4>();
		var colorsTextureData = _colorsTexture.GetPixels32();
		var alphasTextureData = _alphasTexture.GetRawTextureData<float>();

		//Set pixels data
		int currentPixelIndex = 0;
		await Task.Run(() => {
			foreach (var trajectory in trajectories) {
				cancellationToken.ThrowIfCancellationRequested();
				int pNext;

				for (var p = 0; p < trajectory.Points.Length; p = pNext) {
					pNext = p + SampleValue;

					//Position
					positionsTextureData[currentPixelIndex] = trajectory.Points[p];

					//Color (distance-proportional
					//If next point exist on this trajectory
					if (pNext < trajectory.Points.Length) {
						//Compute the average distance 
						int i = p;
						float sumDist = 0f;
						while (i < pNext) {
							sumDist += trajectory.Distances[i];
							i++;
						}

						float distAverage = sumDist / (pNext - p);
						colorsTextureData[currentPixelIndex] = Color.HSVToRGB(distAverage.Remap(0, 3 * TrajectoriesManager.Instance.TrajectoriesAverageDistance, 0.66f, 1f, true), 1f, 1f); //color scale commonly use (3 * average) as maximum

						//Set alpha to visible (default value is 0f = invisible)
						alphasTextureData[currentPixelIndex] = 1f;
					}

					currentPixelIndex++;
				}
			}
		}, cancellationToken).ConfigureAwait(true);

		
		//Apply texture modif
		_positionsTexture.Apply();
		_colorsTexture.SetPixels32(colorsTextureData);
		_colorsTexture.Apply();
		_alphasTexture.Apply();

		//Apply value to VFX
		_visualEffect.Reinit();     //Reset vfx otherwise all particules are mixed up between trajectories (colors are mixed)
		_visualEffect.SetUInt("SegmentCount", Convert.ToUInt32(currentPixelIndex - 1));
		_visualEffect.SetTexture("Trajectories", _positionsTexture);
		_visualEffect.SetTexture("Colors", _colorsTexture);
		_visualEffect.SetTexture("Alphas", _alphasTexture);
	}
}
