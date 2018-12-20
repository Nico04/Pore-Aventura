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

	private Texture2D _texture;
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
		_visualEffect.SetUInt("TextureWidth", Convert.ToUInt32(_texture.width));
		_visualEffect.SetUInt("TrajectoriesCount", Convert.ToUInt32(trajectories.Length));
		_visualEffect.SetTexture("Trajectories", _texture);


		/**
		for (int y = 0; y < _texture.height; y++) {
			var trajectory = trajectories[y];
			for (int x = 0; x < trajectory.Points.Length; x++) {
				var p = trajectory.Points[x];
				textureData[y * _texture.width + x * 4] = ScaleToRange01(p.x, 20f);
				textureData[y * _texture.width + x * 4 + 1] = ScaleToRange01(p.y, 20f);
				textureData[y * _texture.width + x * 4 + 2] = ScaleToRange01(p.z, 20f);
				textureData[y * _texture.width + x * 4 + 3] = trajectory.Points.Length;		//Store length of the trajectory in the alpha channel to be used in the vfx
			}
		}*/



		//DEBUG
		//Texture2D texCopy = new Texture2D(_texture.width, _texture.height, _texture.format, false);
		//texCopy.LoadRawTextureData(_texture.GetRawTextureData());
		//texCopy.Apply();
		//Puis comparer texture et texCopy

		/**
		var debugTexture = new Texture2D(2, 3, TextureFormat.RGBAFloat, false);
		var debugData = debugTexture.GetRawTextureData<float>();
		debugData[0 * 4] = 0f;
		debugData[1 * 4] = 0f;
		debugData[2 * 4] = -1f;
		debugData[3 * 4] = 1f;
		debugData[4 * 4] = float.MinValue;
		debugData[5 * 4] = float.MaxValue;
		debugTexture.Apply();
		*/


		/*
		var debugTexture = new Texture2D(DebugTexture.width, DebugTexture.height, TextureFormat.RGBAHalf, false);
		Graphics.ConvertTexture(DebugTexture, debugTexture);
		debugTexture.Apply();

		DebugRenderer.material.SetTexture("_UnlitColorMap", debugTexture);

		Debug.Log($"{debugTexture.format} is supported = {SystemInfo.SupportsTextureFormat(debugTexture.format)}");
		*/
	}

	//Scale an input value that goes between 0 and max, to be in range 0 to 1.
	private float ScaleToRange01(float value, float max) => value / max;
	private static float PositionToColor(float position) => position * 1f / 20;

	private void UpdateSpawnDelay() => _visualEffect.SetFloat("SpawnDelay", TrajectoriesManager.Instance.SpawnDelay / 1000f);

	public int GetTotalParticlesCount() => _visualEffect != null ? _visualEffect.aliveParticleCount : 0;

	public void Pause(bool pause) => _visualEffect.pause = pause;
}
