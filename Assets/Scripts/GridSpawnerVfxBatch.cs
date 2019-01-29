using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class GridSpawnerVfxBatch : MonoBehaviour {
	private bool _askRebuild = true;
	public void AskRebuild() => _askRebuild = true;

	private bool _askUpdateSpawnDelay = true;
	public void AskUpdateSpawnDelay() => _askUpdateSpawnDelay = true;

	private VisualEffect _visualEffect;
	private void Start() {
		_visualEffect = GetComponent<VisualEffect>();
	}

	private void Update() {
		if (PauseManager.IsPaused)
			return;

		if (_askRebuild) {
			BuildSpawners();
			_askRebuild = false;
		}

		if (_askUpdateSpawnDelay) {
			UpdateSpawnDelay();
			_askUpdateSpawnDelay = false;
		}
	}

	private Texture2D _texture;    //We need to keep a ref to the texture because SetTexture only make a binding.
	private void BuildSpawners() {
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
	}

	//Scale an input value that goes between 0 and max, to be in range 0 to 1.
	private float ScaleToRange01(float value, float max) => value / max;
	private static float PositionToColor(float position) => position * 1f / 20;

	private void UpdateSpawnDelay() => _visualEffect.SetFloat("SpawnDelay", TrajectoriesManager.Instance.SpawnDelay / 1000f);

	public int GetTotalParticlesCount() => _visualEffect != null ? _visualEffect.aliveParticleCount : 0;

	public void Pause(bool pause) {
		if (_visualEffect != null)
			_visualEffect.pause = pause;
	}
}
