using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class GridSpawnerVfx : MonoBehaviour {
	public GameObject ParticlesSpawner;

	private bool _askRebuild = true;
	public void AskRebuild() => _askRebuild = true;
	
	private bool _askUpdateSpawnDelay = true;
	public void AskUpdateSpawnDelay() => _askUpdateSpawnDelay = true;

	// Update is called once per frame
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


	private List<GameObject> _spawners = new List<GameObject>();
	private List<Texture2D> _textureHolder;
	private void BuildSpawners() {
		//Remove all previous spawners
		_spawners.ForEach(Destroy);
		_spawners.Clear();


		_textureHolder = new List<Texture2D>();
		foreach (var trajectory in TrajectoriesManager.Instance.Trajectories) {
			var particlesSpawner = Instantiate(ParticlesSpawner, trajectory.StartPoint, Quaternion.identity, transform);
			var visualEffect = particlesSpawner.GetComponent<VisualEffect>();


			/** RGBA32 method
			 * Uniquement 256 valeurs par axe => Pas assez précis
			var colors = trajectory.Points.Select(p => new Color32(PositionToColor(p.x), PositionToColor(p.y), PositionToColor(p.z), byte.MaxValue)).ToArray();
			
			var texture = new Texture2D(colors.Length, 1, TextureFormat.RGBA32, false);
			texture.SetPixels32(colors);			
			*/

			/** RGBAHalf Method - GetRawTextureData
			 * Ne fonctionne pas bien (position incohérentes)
			var texture = new Texture2D(trajectory.Points.Length, 1, TextureFormat.RGBAHalf, false);
			var textureData = texture.GetRawTextureData<UInt16>();

			for (int i = 0; i < trajectory.Points.Length; i++) {
				var p = trajectory.Points[i];
				textureData[i * 4] = PositionToUInt16(p.x);
				textureData[i * 4 + 1] = PositionToUInt16(p.y);
				textureData[i * 4 + 2] = PositionToUInt16(p.z);
				textureData[i * 4 + 3] = UInt16.MaxValue;
			}
			*/

			/** RGBAHalf Method - GetRawTextureData
			 * Ne fonctionne pas bien du tout
			 * 15360 correspond au blanc (donc max) quand on regarde en reverse engineering
			 *
			var texture = new Texture2D(trajectory.Points.Length, 1, TextureFormat.RGBAHalf, false);
			var textureData = texture.GetRawTextureData<UInt16>();

			for (int i = 0; i < trajectory.Points.Length; i++) {
				var p = trajectory.Points[i];
				textureData[i * 4] = PositionToColor16(p.x);
				textureData[i * 4 + 1] = PositionToColor16(p.y);
				textureData[i * 4 + 2] = PositionToColor16(p.z);
				textureData[i * 4 + 3] = Convert.ToUInt16(15360);
			}*/

			/** RGBAHalf Method - SetPixels
			 * SetPixels : This function works only on RGBA32, ARGB32, RGB24 and Alpha8 texture formats
			 * It's not meant to be used with RGBAHalf but it actually works
			 *
			var texture = new Texture2D(trajectory.Points.Length, 1, TextureFormat.RGBAHalf, false);
			texture.SetPixels(trajectory.Points.Select(p => new Color(PositionToColor(p.x), PositionToColor(p.y), PositionToColor(p.z), 1f)).ToArray());
			*/

			/** reverse engineering
			texture.SetPixel(0, 0, Color.black);
			texture.SetPixel(1, 0, Color.white);

			texture.Apply();

			var textureData = texture.GetRawTextureData();
			var textureData2 = texture.GetRawTextureData<UInt16>();
			var textureData3 = texture.GetRawTextureData<Int16>();
			*/

			/** RGBAFloat Method
			 * More precise and way more easier to handle (because float exist in C# whereas float16 doesn't)
			 */
			var texture = new Texture2D(trajectory.Points.Length, 1, TextureFormat.RGBAFloat, false);
			var textureData = texture.GetRawTextureData<Vector4>();     //We can directly use Vector4 because is 4 float, that matches RGBA channels
			for (var i = 0; i < textureData.Length; i++) 
				textureData[i] = trajectory.Points[i];		//Implicite Vector3 to Vector4 conversion, as we don't use alpha chanel 
			
			texture.Apply();

			//Apply value to VFX
			visualEffect.SetUInt("TrajectoryLength", Convert.ToUInt32(trajectory.Points.Length));
			visualEffect.SetTexture("Trajectory", texture);
			
			//Hold value to lists
			_spawners.Add(particlesSpawner);
			_textureHolder.Add(texture);     //We need to keep a ref to the texture because SetTexture only make a binding.
		}

		Debug.Log($"{TextureFormat.RGBAHalf} is supported = {SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf)}");
	}

	public int GetTotalParticlesCount() => _spawners.Sum(s => s.GetComponent<VisualEffect>().aliveParticleCount);

	private static byte PositionToColor32(float position) => Convert.ToByte(position * byte.MaxValue / 20);
	private static UInt16 PositionToColor16(float position) => Convert.ToUInt16(position * 15360 / 20);
	private static float PositionToColor(float position) => position * 1f / 20;

	
	private void UpdateSpawnDelay() {
		foreach (var spawner in _spawners) {
			spawner.GetComponent<VisualEffect>().SetFloat("SpawnRate", TrajectoriesManager.Instance.SpawnRate);
		}
	}
	
	public void Pause(bool pause) {
		foreach (var spawner in _spawners) {
			spawner.GetComponent<VisualEffect>().pause = pause;
		}
	}
}