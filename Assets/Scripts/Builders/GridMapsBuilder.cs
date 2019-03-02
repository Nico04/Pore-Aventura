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

	private const int TextureResolution = 256;
	private Vector2 _worldToPixelRatio;

	protected override void Start() {
		base.Start();

		_solidBoundariesBuilder = SolidBoundariesHolder.GetComponent<SolidBoundariesBuilder>();

		//Set sizes and positions
		gameObject.transform.localScale = new Vector3(1f, DataBase.DataSpaceSize.y, DataBase.DataSpaceSize.z);
		Exit.transform.position = Exit.transform.position.SetCoordinate(x: 0.99f * DataBase.DataSpaceSize.x);

		//Set world to texture coordinates converter ratio
		_worldToPixelRatio = new Vector2(DataBase.DataSpaceSize.z, DataBase.DataSpaceSize.y) / TextureResolution;
	}

	protected override async Task Build(CancellationToken cancellationToken) {
		//BuildGridMaps();
		await BuildGridMapsSoapBubble(cancellationToken).ConfigureAwait(false);
	}

	//Build deux maps with the Soap Bubble / Vonoroï like algo 
	private async Task BuildGridMapsSoapBubble(CancellationToken cancellationToken) {
		//Get input data
		var trajectories = await TrajectoriesManager.Instance.GetInjectionGridTrajectories(cancellationToken).ConfigureAwait(true);

		//--- Entry Map ---
		//Set map position
		Entry.transform.position = Entry.transform.position.SetCoordinate(x: trajectories[0].StartPoint.x);

		//Build a list of PointColor of the trajectories that intersect the plan, in the texture coordinates
		List<PointColor2> entryPoints = null;
		await Task.Run(() => {
			entryPoints = trajectories.Select(t => new PointColor2 {
				Position = WorldToPixelCoordinates(t.StartPoint),
				Color = t.Color
			}).ToList();
		}, cancellationToken).ConfigureAwait(true);
		

		//Build texture
		await BuildGridMapSoapBubble(entryPoints, Entry, cancellationToken).ConfigureAwait(true);

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
					Position = WorldToPixelCoordinates(point),
					Color = trajectory.Color
				});
			}
		}, cancellationToken).ConfigureAwait(true);

		//Build texture
		await BuildGridMapSoapBubble(exitPoints, Exit, cancellationToken).ConfigureAwait(false);
	}

	//Build a Map with the Soap Bubble / Vonoroï like algo 
	private async Task BuildGridMapSoapBubble(List<PointColor2> points, GameObject textureHolder, CancellationToken cancellationToken) {
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
				var currentPositionInSpace = PixelToWorldCoordinates(textureHolderPositionX, currentPoint);
				if (SolidBoundariesBuilder.Contains(currentPositionInSpace)) {
					textureArray[p] = Color.white;
					continue;
				}

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

	private Vector2 WorldToPixelCoordinates(Vector3 worldPosition) => 
		new Vector2(worldPosition.z, worldPosition.y) / _worldToPixelRatio;

	private Vector3 PixelToWorldCoordinates(float worldX, Vector2 pixelPosition) => 
		new Vector3(worldX, pixelPosition.y * _worldToPixelRatio.y, pixelPosition.x * _worldToPixelRatio.x);
}

public struct PointColor3 {
	public Vector3 Position;
	public Color Color;
}

public class PointColor2 {
	public Vector2 Position;
	public Color Color;
}
