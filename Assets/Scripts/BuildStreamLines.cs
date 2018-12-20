using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//using DigitalRuby.FastLineRenderer;
//using Vectrosity;

public class BuildStreamLines : MonoBehaviour {
#region Unity LineRenderer v2018.2
	public GameObject Line;

	private bool _askRebuild = true;
	public void AskRebuild() {
		_askRebuild = true;
	}

	private MaterialPropertyBlock _materialPropertyBlock;
	private void Start() {
		_materialPropertyBlock = new MaterialPropertyBlock();
	}

	private void Update() {
		if (PauseManager.IsPaused)
			return;

		if (_askRebuild)
			ReBuildStreamLines();
	}
	
	private void ReBuildStreamLines() {
		_askRebuild = false;

		//Delete all existing streamlines
		for (int i = 0; i < transform.childCount; i++) {
			Destroy(transform.GetChild(i).gameObject);
		}

		/** One line object per segment method
		 * Usefull for setting colors easily, but very uneficient performance-wise
		//Loop through all injection point
		for (float y = 0.1f; y <= GridManager.Size; y += GridManager.Spacing) {
			for (float z = 0.1f; z <= GridManager.Size; z += GridManager.Spacing) {
				//Init list
				var startPosition = new Vector3(transform.position.x, y, z);

				//Get speed at currentPoint
				Vector3 currentSpeed;
				while ((currentSpeed = DataBase.GetSpeedAtPoint(startPosition)) != Vector3.zero) {
					//Move to next point by going into the speed direction by a defined distance
					var endPosition = startPosition + currentSpeed.normalized * StreamLineIncDistance;
					
					//Create a new line renderer
					var line = Instantiate(Line, Vector3.zero, Quaternion.identity, transform);
					var lineRenderer = line.GetComponent<LineRenderer>();

					//Apply points
					lineRenderer.positionCount = 2;
					lineRenderer.SetPositions(new []{startPosition, endPosition});

					//Move to next segment
					startPosition = endPosition;
				}
			}
		}

		return;
		*/
		
		/** Old code
		//Loop through all injection point
		for (float y = 0.1f; y <= TrajectoriesManager.Size; y += TrajectoriesManager.Spacing) {
			for (float z = 0.1f; z <= TrajectoriesManager.Size; z += TrajectoriesManager.Spacing) {
				//Build stream line points
				var positions = BuildStreamLine(new Vector3(transform.position.x, y, z), out var speedList);
				if (positions == null) continue;

				//Create a new line renderer
				var line = Instantiate(Line, Vector3.zero, Quaternion.identity, transform);
				var lineRenderer = line.GetComponent<LineRenderer>();

				//Apply points
				lineRenderer.positionCount = positions.Length;
				lineRenderer.SetPositions(positions);

				//
				//const int textLength = 1024;
				var texture = new Texture2D(speedList.Count, 1);
				var maxSpeed = speedList.Max();
				texture.SetPixels32(speedList.Select(s => (Color32)Color.Lerp(Color.blue, Color.red, s / maxSpeed)).ToArray());
				texture.Apply();

				lineRenderer.material.mainTexture = texture;
			}
		}*/
		
		foreach (var trajectory in TrajectoriesManager.Instance.Trajectories) {
			//Create a new line renderer
			var line = Instantiate(Line, Vector3.zero, Quaternion.identity, transform);
			var lineRenderer = line.GetComponent<LineRenderer>();

			//Apply points
			lineRenderer.positionCount = trajectory.Points.Length;
			lineRenderer.SetPositions(trajectory.Points);

			//-- Calculate colors --
			/** Obsolete
			//Build distance array
			var distances = new float[trajectory.Points.Length - 1];
			for (int i = 0; i < distances.Length; i++) 
				distances[i] = Vector3.Distance(trajectory.Points[i + 1], trajectory.Points[i]);
			*/
			
			var distances = trajectory.Distances;
			int textureSize = Mathf.CeilToInt(distances.Length / 10f);
			var distanceIncrement = distances.Sum() / (textureSize - 1);

			var colorValues = new float[textureSize];
			int currentDistanceIndex = 0;
			var currentDistance = distances[currentDistanceIndex];
			for (int i = 0; i < colorValues.Length; i++) {
				while (currentDistance < distanceIncrement * i && currentDistanceIndex + 1 < distances.Length)	//Index condition needed because of float imprecision.
					currentDistance += distances[++currentDistanceIndex];

				colorValues[i] = distances[currentDistanceIndex];
			}

			//Apply colors
			var texture = new Texture2D(textureSize, 1);
			texture.SetPixels32(colorValues.Select(s => (Color32)Color.Lerp(Color.blue, Color.red, s / (3 * TrajectoriesManager.Instance.TrajectoriesAverageDistance))).ToArray());
			texture.Apply();
			lineRenderer.material.mainTexture = texture;

			//TODO this method render black line when app is launched. Work fine after change de resolution (rebuild).
			//lineRenderer.GetPropertyBlock(_materialPropertyBlock);
			//_materialPropertyBlock.SetTexture("_MainTex", texture);
			//lineRenderer.SetPropertyBlock(_materialPropertyBlock);
		}
	}

	/** Method using constant distance step : not very physically logical.
	private const float StreamLineIncDistance = 0.01f;
	private Vector3[] BuildStreamLine(Vector3 startPoint, out List<float> speedList) {
		speedList = new List<float>();

		//Init list
		List<Vector3> points = new List<Vector3> { startPoint };
		Vector3 currentPoint = points[0];

		//Get speed at currentPoint
		Vector3 currentSpeed;
		while ((currentSpeed = DataBase.GetSpeedAtPoint(currentPoint)) != Vector3.zero) {
			//Move to next point by going into the speed direction by a defined distance
			currentPoint += currentSpeed.normalized * StreamLineIncDistance;

			//Add point to list
			points.Add(currentPoint);

			//
			speedList.Add(currentSpeed.magnitude);
		}

		return points.Count > 1 ? points.ToArray() : null;
	}*/
#endregion

#region Unity LineRenderer (vOld)
#if false
	/** Problems with Unity LineRenderer :
     * - Max color gradient key = 8
     * - Even with few keys, the color gradient positions are not accurate : a transition WILL appears smoothly between not segment no matter what you set...
     */

	public float colorGradiantTransition = 0.1f;

	// Use this for initialization
	void Start() {
		//For Testing
		Vector3[] positions_TEST = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 5, 1), new Vector3(0, 5, -3) };

		//Obect
		GameObject lineObj = new GameObject("DragLine", typeof(LineRenderer));
		LineRenderer line = lineObj.GetComponent<LineRenderer>();

		//Material        
		line.material = new Material(Shader.Find("Sprites/Default"));

		//Positions
		line.positionCount = positions_TEST.Length;
		line.SetPositions(positions_TEST);

		//Width
		line.startWidth = line.endWidth = 0.05f;

		//Color
		//line.startColor = line.endColor = Color.white;        

		//A simple 2 color gradient with a fixed alpha of 1.0f.
		/*float alpha = 1.0f;
		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.blue, 0.5f), new GradientColorKey(Color.red, 1f) },
			new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1f) }
			);
		line.colorGradient = gradient;*/
		line.colorGradient = GenerateColorGradientFromPositions(positions_TEST);
	}

	private Gradient GenerateColorGradientFromPositions(Vector3[] positions) {
		//Find distance min and max between to points to get color scale
		float[] distances = new float[positions.Length - 1];
		float distanceMin = float.MaxValue;
		float distanceMax = 0f;
		float distanceTotal = 0f;

		for (var i = 0; i < positions.Length - 1; i++) {
			float dist = Vector3.Distance(positions[i], positions[i + 1]);
			distances[i] = dist;
			distanceMin = Mathf.Min(distanceMin, dist);
			distanceMax = Mathf.Max(distanceMax, dist);
			distanceTotal += dist;
		}

		//Create the full-scale color gradient for reference
		Gradient fullScaleGradient = new Gradient();
		fullScaleGradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(Color.blue, 0f), new GradientColorKey(Color.red, 1f) },
			new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
			);

		//Create the final curve positionned color gradient
		GradientColorKey[] colorKeys = new GradientColorKey[distances.Length * 2];
		float currentTotalLength = 0f;

		for (var i = 0; i < distances.Length; i++) {    //For each segment, set a key near the start and one near the end (based on colorGradiantTransition)
			float segmentLength = distances[i];

			//Calculate segment's color
			Color segmentColor = fullScaleGradient.Evaluate((segmentLength - distanceMin) / (distanceMax - distanceMin));

			//Set start key
			colorKeys[i * 2] = new GradientColorKey(segmentColor, (currentTotalLength + colorGradiantTransition * distanceMin) / distanceTotal);

			//Set currentTotalLength
			currentTotalLength += segmentLength;

			//Set end key
			colorKeys[i * 2 + 1] = new GradientColorKey(segmentColor, (currentTotalLength - colorGradiantTransition * distanceMin) / distanceTotal);
		}

		//Build Gradient object
		Gradient gradient = new Gradient();
		gradient.SetKeys(colorKeys, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
		return gradient;
	}
#endif
	#endregion

#region Vectrosity
#if false
    /* Very fast and convenient
     * BUT not very accurate at screen, shown by creation order (z-order can be wrong)
     */
    //async void Start() {
    void Start() {
        /*System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch(); stopWatch.Start();
        int i;
        for (i = 0; i < 1000; i++) {
            DataBase.getSpeedAtPoint(new Vector3(292.5f, 84.3f, 95.1f) * DataBase.spaceFactor);
        }
        MyExtensions.LogWithElapsedTime("getSpeedAtPoint * " + i, stopWatch);
        return;
        */

        /*
        //Matlab test  
        var reader = new MatReader(@"Assets\Data\monoVarSingle.mat", false, false);        
        return;
        */

        //speedField = DataBaseExtractionTools.BuildSpeedfield3(@"Assets\Data\monoVarArray.mat");
        //speedField = await Task.Run(() => DataBaseExtractionTools.BuildSpeedfield3());
        //Debug.Log(speedField[200, 100, 30].toStringLong() + " == [0.1453, 0.0708, 0.2071]");     //V(200,100,30)]=[0.1453, 0.0708, 0.2071]
        //Debug.Log(speedField[200, 169, 90].toStringLong() + " == [0.2699, -0.1494, 0.0801]");     ////V(200, 169, 90)]=[0.2699, -0.1494, 0.0801]
        //Debug.Log(getSpeedAtPoint(new Vector3(292.5f, 84.3f, 95.1f) * DataBaseExtractionTools.SpaceFactor, speedField, DataBaseExtractionTools.SpaceFactor).GetValueOrDefault().toStringLong());        //V(292.5;84.3;95.1)=[0.453321539749967;0.0773046135082961;-0.0217250031829686]
        //return;

        /*
        speedField = DataBaseExtractionTools.BuildSpeedfield2(@"C:\Users\Nicolas\Documents\Unity3D\Pore Aventura\Assets\Data\SpeedField3D.mat");
        Debug.Log($"({speedField[200, 100, 30].x}, {speedField[200, 100, 30].y}, {speedField[200, 100, 30].z})");
        Debug.Log($"({speedField[200, 169, 90].x}, {speedField[200, 169, 90].y}, {speedField[200, 169, 90].z})");
        return;
        */

        /*
        //speedField = await Task.Run(() => DataBaseExtractionTools.BuildSpeedfield(@"C:\Users\Nicolas\Documents\Unity3D\Pore Aventura\Assets\Data\XYZ_VXVYVZ.mat"));
        speedField = DataBaseExtractionTools.BuildSpeedfield(@"C:\Users\Nicolas\Documents\Unity3D\Pore Aventura\Assets\Data\XYZ_VXVYVZ.mat");

        //Debug.Log(speedField[200, 100, 30].toStringLong() + " == [0.1453, 0.0708, 0.2071]");     //V(200,100,30)]=[0.1453, 0.0708, 0.2071]
        //Debug.Log(speedField[200, 169, 90].toStringLong() + " == [0.2699, -0.1494, 0.0801]");     ////V(200, 169, 90)]=[0.2699, -0.1494, 0.0801]


        //Interpolation test
        //Debug.Log(getSpeedAtPoint(new Vector3(200, 100, 30)             * DataBaseExtractionTools.SpaceFactor, speedField, DataBaseExtractionTools.SpaceFactor).GetValueOrDefault().toStringLong());
        //Debug.Log(getSpeedAtPoint(new Vector3(200.2f, 100.2f, 30.2f)    * DataBaseExtractionTools.SpaceFactor, speedField, DataBaseExtractionTools.SpaceFactor).GetValueOrDefault().toStringLong());
        //Debug.Log(getSpeedAtPoint(new Vector3(200, 169, 90)             * DataBaseExtractionTools.SpaceFactor, speedField, DataBaseExtractionTools.SpaceFactor).GetValueOrDefault().toStringLong());
        //Debug.Log(getSpeedAtPoint(new Vector3(200.3f, 169.1f, 90.2f)    * DataBaseExtractionTools.SpaceFactor, speedField, DataBaseExtractionTools.SpaceFactor).GetValueOrDefault().toStringLong());
        //Debug.Log(getSpeedAtPoint(new Vector3(292.5f, 84.3f, 95.1f) * DataBaseExtractionTools.SpaceFactor, speedField, DataBaseExtractionTools.SpaceFactor).GetValueOrDefault().toStringLong());        //V(292.5;84.3;95.1)=[0.453321539749967;0.0773046135082961;-0.0217250031829686]
        */

        //Streamline draw
        //drawStreamLines();
        





        //read speedField CSV
        /*string data = csvFile.text;
        speedField = await Task.Run(() => DataBaseExtractionTools.ExtractSpeedfieldDataFromCSV(data));
        return;
        */




        /*positions_TEST = new List<Vector3>() { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 5, 1), new Vector3(0, 5, -1) };
        for (int i = 0; i < 5; i++) {
            positions_TEST.Add(new Vector3(Random.Range(0f, 20f), Random.Range(0f, 20f), Random.Range(0f, 20f)));
        }*/
        /*positions_TEST = DataBaseExtractionTools.ExtractStreamlinesDataFromCSV(csvFile.text)[1];

        VectorLine line = new VectorLine("Grid", positions_TEST, 3f, LineType.Continuous);
        line.SetColors(GenerateColorsFromPositions(positions_TEST));
        line.Draw3DAuto();*/
    }

    private void drawStreamLines() {
        List<Vector3> points = generateOneStreamLine(new Vector3(200 * DataBase.SpaceFactor, 100 * DataBase.SpaceFactor, 30 * DataBase.SpaceFactor));

        /*
        VectorLine line = new VectorLine("Grid", points, 3f, LineType.Continuous);
        //line.SetColors(GenerateColorsFromPositions(positions_TEST));
        line.Draw3DAuto();
        */

        FastLineRenderer LineRenderer = FastLineRenderer.CreateWithParent(gameObject, FastLineRendererTemplate);
        FastLineRendererProperties props = new FastLineRendererProperties();
        props.Radius = 0.01f;
        props.LineJoin = FastLineRendererLineJoin.AdjustPosition;
        LineRenderer.AddLine(props, points, null, false, false);
        LineRenderer.Apply();
    }

    private const float streamLineIncDistance = 0.1f;
    private List<Vector3> generateOneStreamLine(Vector3 startPoint) {
        //Init list
        List<Vector3> points = new List<Vector3>() { startPoint };
        Vector3 currentPoint = points[0];

        //Get speed at currentPoint
        Vector3 currentSpeed;
        while ((currentSpeed = DataBase.GetSpeedAtPoint(currentPoint)) != Vector3.zero) {
            //Move to next point by going into the speed direction by a defined distance
            currentPoint += currentSpeed.normalized * streamLineIncDistance;

            //Add point to list
            points.Add(currentPoint);
        }

        return points;
    }

    private List<Color32> GenerateColorsFromPositions(List<Vector3> positions) {
        //Find distance min and max between to points to get color scale
        float[] distances = new float[positions.Count - 1];
        float distanceMin = float.MaxValue;
        float distanceMax = 0f;
        float distanceTotal = 0f;

        for (var i = 0; i < positions.Count - 1; i++) {
            float dist = Vector3.Distance(positions[i], positions[i + 1]);
            distances[i] = dist;
            distanceMin = Mathf.Min(distanceMin, dist);
            distanceMax = Mathf.Max(distanceMax, dist);
            distanceTotal += dist;
        }

        //Create the full-scale color gradient for reference
        Gradient fullScaleGradient = new Gradient();
        fullScaleGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.blue, 0f), new GradientColorKey(Color.red, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );

        //Create the color's array for each segment
        List<Color32> colors = new List<Color32>();
        for (var i = 0; i < distances.Length; i++) {
            colors.Add(fullScaleGradient.Evaluate((distances[i] - distanceMin) / (distanceMax - distanceMin)));
        }

        distanceMax_CACA = distanceMax;
        distanceMin_CACA = distanceMin;

        return colors;
    }
#endif
#endregion
}