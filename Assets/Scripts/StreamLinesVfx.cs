using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class StreamLinesVfx : MonoBehaviour {
	private bool _askRebuild = true;
	public void AskRebuild() => _askRebuild = true;

	private VisualEffect _visualEffect;
	private void Start() {
		_visualEffect = GetComponent<VisualEffect>();
	}
	
	void Update() {
		if (PauseManager.IsPaused)
			return;

		if (_askRebuild) {
			BuildStreamLines();
			_askRebuild = false;
		}
	}

	private const float SampleValue = 1;
	private Texture2D _texture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	private void BuildStreamLines() {
		var trajectories = TrajectoriesManager.Instance.Trajectories;
		int size = (int)Math.Ceiling(Math.Sqrt(trajectories.Sum(t => Math.Ceiling(t.Points.Length / SampleValue))));
		_texture = new Texture2D(size, size, TextureFormat.RGBAFloat, false);       //Unity max texture width or height is 16k : cannot use a mono-line texture.
		_texture.wrapMode = TextureWrapMode.Clamp;		//Important for vfx index access

		var textureData = _texture.GetRawTextureData<Vector4>();
		
		int currentPixelIndex = 0;
		foreach (var trajectory in trajectories) {
			for (var p = 0; p < trajectory.Points.Length; p += (int)SampleValue) {
				textureData[currentPixelIndex++] = trajectory.Points[p];
			}
		}

		_texture.Apply();

		//Apply value to VFX
		_visualEffect.Reinit();     //Reset vfx otherwise all particules are mixed up between trajectories (colors are mixed)
		_visualEffect.SetUInt("TextureWidth", Convert.ToUInt32(_texture.width));
		_visualEffect.SetFloat("DistanceMax", TrajectoriesManager.Instance.TrajectoriesDistanceMax);
		_visualEffect.SetFloat("DistanceColorMax", 3 * TrajectoriesManager.Instance.TrajectoriesAverageDistance);		//color scale commonly use (3 * average) as maximum
		_visualEffect.SetTexture("Trajectories", _texture);

		Debug.Log($"BuildStreamLines|size={size}|");
	}
}
