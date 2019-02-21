using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GridMapsBuilder : Builder {
	public GameObject Entry;
	public GameObject Exit;
	public GameObject SolidBoundariesHolder;

	private SolidBoundariesBuilder _solidBoundariesBuilder;

	protected override void Start() {
		base.Start();

		_solidBoundariesBuilder = SolidBoundariesHolder.GetComponent<SolidBoundariesBuilder>();
	}

	protected override async Task Build(CancellationToken cancellationToken) {
		//BuildGridMaps();
		await BuildGridMapsSoapBubble(cancellationToken).ConfigureAwait(false);
	}

	/** Old pixelated method
	private void BuildGridMaps() {
		var trajectories = TrajectoriesManager.Instance.Trajectories;
		var entryResolution = TrajectoriesManager.Instance.Resolution;

		//--- Entry Map ---
		var entryTexture = GetNewTexture(entryResolution);

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
		List<PointColor3> intersectingPoints = new List<PointColor3>();

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

			intersectingPoints.Add(new PointColor3 {
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
		int exitResolution = (int)(DataBase.DataSpaceSize.y / distanceMin);
		var exitTexture = GetNewTexture(exitResolution);

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
	}*/

	private const int TextureResolution = 256;

	//Build deux maps with the Soap Bubble / Vonoroï like algo 
	private async Task BuildGridMapsSoapBubble(CancellationToken cancellationToken) {
		var trajectories = await TrajectoriesManager.Instance.GetInjectionGridTrajectories(cancellationToken).ConfigureAwait(true);
		float textureSpacing = DataBase.DataSpaceSize.y / TextureResolution;

		//--- Entry Map ---
		//Build a list of PointColor of the trajectories that intersect the plan, in the texture coordinates
		List<PointColor2> entryPoints = null;
		await Task.Run(() => {
			entryPoints = trajectories.Select(t => new PointColor2 {
				Position = new Vector2(
					CoordinateToPixelIndex(t.StartPoint.z, textureSpacing),
					CoordinateToPixelIndex(t.StartPoint.y, textureSpacing)),
				Color = t.Color
			}).ToList();
		}, cancellationToken).ConfigureAwait(true);
		

		//Build texture
		await BuildGridMapSoapBubble(entryPoints, textureSpacing, Entry, cancellationToken).ConfigureAwait(true);

		//--- Exit Map ---
		//Build a list of PointColor of the trajectories that intersect the plan, in the texture coordinates
		List<PointColor2> exitPoints = new List<PointColor2>();
		var exitPositionX = Exit.transform.position.x;

		await Task.Run(() => {
			foreach (var trajectory in trajectories) {
				//Find the first point which has a x < to the exit.x, starting from the end
				Vector3 point = Vector3.zero;
				int currentPointIndex = trajectory.Points.Length - 1;
				while (currentPointIndex >= 0 && trajectory.Points[currentPointIndex].x >= exitPositionX) {
					point = trajectory.Points[currentPointIndex];
					currentPointIndex--;
				}

				//Skip this trajectory if it's too short
				if (point == Vector3.zero)
					continue;

				exitPoints.Add(new PointColor2 {
					Position = new Vector2(
						CoordinateToPixelIndex(point.z, textureSpacing),
						CoordinateToPixelIndex(point.y, textureSpacing)),
					Color = trajectory.Color
				});
			}
		}, cancellationToken).ConfigureAwait(true);

		//Build texture
		await BuildGridMapSoapBubble(exitPoints, textureSpacing, Exit, cancellationToken).ConfigureAwait(false);
	}

	//Build a Map with the Soap Bubble / Vonoroï like algo 
	private async Task BuildGridMapSoapBubble(List<PointColor2> points, float textureSpacing, GameObject textureHolder, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();

		var textureHolderPositionX = textureHolder.transform.position.x;
		const int pointRadius = 5;

		//New texture
		var texture = GetNewTexture(TextureResolution);
		var textureArray = texture.GetPixels32();
		var textureWidth = texture.width;

		//Set pixels
		await Task.Run(() => {
			for (int p = 0; p < textureArray.Length; p++) {
				//Convert into coordinates
				var currentPoint = new Vector2(p % textureWidth, (int)(p / textureWidth));

				//if this pixel is INTO a microstructure sphere, set it to white and continue;
				bool isInsideSolidBounds = false;
				var currentPositionInSpace = new Vector3(textureHolderPositionX, currentPoint.y * textureSpacing, currentPoint.x * textureSpacing);
				foreach (var position in _solidBoundariesBuilder.Positions) {
					if (Vector3.Distance(currentPositionInSpace, position) < 1f) {
						textureArray[p] = Color.white;
						isInsideSolidBounds = true;
						break;
					}
				}
				if (isInsideSolidBounds)
					continue;
				

				//Find the closest point
				PointColor2 closestPoint = null;
				float distanceMin = float.MaxValue;
				foreach (var point in points) {
					float distance;
					if ((distance = Vector2.Distance(currentPoint, point.Position)) < distanceMin) {
						distanceMin = distance;
						closestPoint = point;
					}
				}

				//Apply color to pixel
				textureArray[p] = distanceMin <= pointRadius ? closestPoint.Color : Color.black;
			}
		}, cancellationToken).ConfigureAwait(true);

		//Apply
		texture.SetPixels32(textureArray);
		texture.Apply();
		textureHolder.GetComponent<Renderer>().material.SetTexture("_UnlitColorMap", texture);
	}

	private Texture2D GetNewTexture(int size) {
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

public struct PointColor3 {
	public Vector3 Position;
	public Color Color;
}

public class PointColor2 {
	public Vector2 Position;
	public Color Color;
}
