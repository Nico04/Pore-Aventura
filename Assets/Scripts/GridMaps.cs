using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridMaps : MonoBehaviour {
	public GameObject Entry;
	public GameObject Exit;

	private bool _askRebuild = true;
	public void AskRebuild() => _askRebuild = true;

	// Update is called once per frame
	void Update() {
		if (PauseManager.IsPaused)
			return;

		if (_askRebuild) {
			GridSpawner.SetTrajectoriesColor();			//TODO move elsewhere
			BuildGridMaps();
			_askRebuild = false;
		}
	}

	private void BuildGridMaps() {
		var trajectories = TrajectoriesManager.Instance.Trajectories;
		var entryResolution = TrajectoriesManager.Instance.Resolution;

		//--- Entry Map ---
		var entryTexture = GetNewBlackTexture(entryResolution);

		//Fill with default color
		var entryTextureArray = FillPixels(entryTexture, Color.black);

		//Fill texture
		foreach (var trajectory in trajectories) {
			//Get pixel position (x & y) of the trajectory start point world position (y and z)
			int x = CoordinateToPixelIndex(trajectory.StartPoint.z, TrajectoriesManager.Instance.Spacing);
			int y = CoordinateToPixelIndex(trajectory.StartPoint.y, TrajectoriesManager.Instance.Spacing);
			entryTextureArray[y * entryTexture.width + x] = trajectory.Color;
		}

		//Apply
		entryTexture.SetPixels32(entryTextureArray);
		entryTexture.Apply();
		Entry.GetComponent<Renderer>().material.SetTexture("_UnlitColorMap", entryTexture);

		//--- Exit Map ---
		//Get positions that intersect the exit plan
		List<PointColor> intersectingPoints = new List<PointColor>();

		foreach (var trajectory in trajectories) {
			//Find the first point which has a x < to the exit.x, starting from the end
			Vector3 point = Vector3.zero;
			int currentPointIndex = trajectory.Points.Length - 1;
			while (currentPointIndex >= 0 && trajectory.Points[currentPointIndex].x >= Exit.transform.position.x) {
				point = trajectory.Points[currentPointIndex];
				currentPointIndex--;
			}

			//Skip this trajectory if it's too short
			if (point == Vector3.zero)
				continue;

			intersectingPoints.Add(new PointColor {
				Position = point,
				Color = trajectory.Color
			});
		}

		//Find the smallest distance 
		float distanceMin = TrajectoriesManager.Instance.Spacing;

		for (int i = 0; i < intersectingPoints.Count; i++) {
			for (int j = 0; j < intersectingPoints.Count; j++) {
				if (i == j) continue;

				var dist = Vector3.Distance(intersectingPoints[i].Position, intersectingPoints[j].Position);

				if (dist < distanceMin && dist > 0.03f)
					distanceMin = dist;
			}
		}

		//Create texture
		int exitResolution = (int)(TrajectoriesManager.Instance.Size / distanceMin);
		var exitTexture = GetNewBlackTexture(exitResolution);

		//Fill with default color
		var exitTextureArray = FillPixels(exitTexture, Color.black);

		//Fill texture
		foreach (var point in intersectingPoints) {
			//Get pixel position (x & y) of the trajectory start point world position (y and z)
			int x = CoordinateToPixelIndex(point.Position.z, distanceMin);
			int y = CoordinateToPixelIndex(point.Position.y, distanceMin);
			exitTextureArray[y * exitTexture.width + x] = point.Color;
		}

		//Apply
		exitTexture.SetPixels32(exitTextureArray);
		exitTexture.Apply();
		Exit.GetComponent<Renderer>().material.SetTexture("_UnlitColorMap", exitTexture);
	}

	private Texture2D GetNewBlackTexture(int size) {
		return new Texture2D(size, size, TextureFormat.RGB24, false) {
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};
	}

	private Color32[] FillPixels(Texture2D texture, Color color) {
		var textureArray = texture.GetPixels32();
		
		for (var i = 0; i < textureArray.Length; i++)
			textureArray[i] = color;

		return textureArray;
	}

	private int CoordinateToPixelIndex(float coordinate, float spacing) {
		return Mathf.FloorToInt(coordinate / spacing);
	}
}

public struct PointColor {
	public Vector3 Position;
	public Color Color;
}
