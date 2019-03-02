using System.IO;
using UnityEngine;

public static class DataBase {
    private static float[,,,] _velocityField; // Velocity Field  Array [x, y, z, velocity(x,y,z)]. Example : [5, 5, 5, 2] will return the z coordinate of the velocity at the point (5, 5, 5)
	private const float VelocityFieldSpaceFactor = 0.050438596491228f; //facteur d'espace (pour passage des unités de l'espace à un indice du tableau speedField)
    public static Vector3 DataSpaceSize;

    public static Vector3[] SolidBoundaries;
	public static float SolidBoundaryRadius = 1f;

    public static float VelocityAverage { get; private set; }

    //Load data from the HDF5 file. This operation is very quick (<250ms).
    public static void InitData() {
        var path = Path.Combine(Application.streamingAssetsPath, "Data.h5");

        //Check if file exists
        if (!File.Exists(path))
            throw new IOException("Couldn't find " + path);
        
        //Open file
        var fileId = HdfReader.OpenFile(path, true);

        //Get datasets
        var solidBoundaries = (float[,])HdfReader.ReadDataSetToArray<float>(fileId, "/SolidBoundaries");   // Solid Boundaries Array [i, position(x,y,z)]. Example : [0, 1] will return the y coordinate of the first solid boundary.
        _velocityField = (float[,,,])HdfReader.ReadDataSetToArray<float>(fileId, "/VelocityField");

        //Set space size
        DataSpaceSize = new Vector3(_velocityField.GetUpperBound(0), _velocityField.GetUpperBound(1), _velocityField.GetUpperBound(2)) * VelocityFieldSpaceFactor;      //new Vector3(18f, 10f, 10f);

		//Compute Velocity Average
        VelocityAverage = GetVelocityAverage();     //Environ 2sec en mode debug

        //Build Vector3 Array for SolidBoundaries
        SolidBoundaries = GetSolidBoundariesVector3(solidBoundaries);
    }

    //Return a Vector3 array of the positions of the solid boundaries
	public static Vector3[] GetSolidBoundariesVector3(float[,] solidBoundaries) {
        if (solidBoundaries == null) {
            Debug.LogError($"{nameof(solidBoundaries)} must be initiated");
            return new Vector3[0];
        }

        //Convert to a Vector3 array to ease reading
        var solidBoundariesVector3 = new Vector3[solidBoundaries.GetUpperBound(0) + 1];
        for (int i = 0; i < solidBoundariesVector3.Length; i++) {
            solidBoundariesVector3[i] = new Vector3(solidBoundaries[i, 0], solidBoundaries[i, 1], solidBoundaries[i, 2]);
        }

        //Get max (x, y, z)
        //var max = new  Vector3(solidBoundaries.Max(sb => sb.x), solidBoundaries.Max(sb => sb.y), solidBoundaries.Max(sb => sb.z));

        return solidBoundariesVector3;
    }

	//Interpole la vitesse au point p de l'espace donné
	//point : point où l’interpolation est souhaitée
	public static Vector3 GetInterpolatedVelocityAtPosition(Vector3 position) {
        if (_velocityField == null) {
            Debug.LogError($"{nameof(_velocityField)} must be initiated");
            return Vector3.zero;
        }

        //1. Trouver les points 8 voisins de p (stockés dans 2 points représentant les extrêmes)
        //Ramener le point p dans l'espace unitaire de speedField
        Vector3 pUnit = position / VelocityFieldSpaceFactor;

        //Point le plus haut
        PointInt3 upperPoint = new PointInt3(Mathf.FloorToInt(pUnit.x), Mathf.FloorToInt(pUnit.y), Mathf.FloorToInt(pUnit.z));

        //Point le plus bas
        PointInt3 lowerPoint = new PointInt3(Mathf.CeilToInt(pUnit.x), Mathf.CeilToInt(pUnit.y), Mathf.CeilToInt(pUnit.z));

        //Vérifier que les points existent
        if (upperPoint.x <= 0 ||
            upperPoint.y <= 0 ||
            upperPoint.z <= 0 ||
            lowerPoint.x <= 0 ||
            lowerPoint.y <= 0 ||
            lowerPoint.z <= 0 ||
            upperPoint.x > _velocityField.GetUpperBound(0) ||
            upperPoint.y > _velocityField.GetUpperBound(1) ||
            upperPoint.z > _velocityField.GetUpperBound(2) ||
            lowerPoint.x > _velocityField.GetUpperBound(0) ||
            lowerPoint.y > _velocityField.GetUpperBound(1) ||
            lowerPoint.z > _velocityField.GetUpperBound(2))
            return Vector3.zero;

		//Si un des 8 points voisins a une vitesse trop faible ET s'il est contenu dans un solid boundary.
		//Ce skip empêche de potentiels calculs de trajectoire infini (sur certaines résolution, la trajectoire fait des aller-retour entre 2 points et se retrouve coincée).
		//SolidBoundariesBuilder.Contains() étant un peu lourd, la 1ere partie du if permet de ne pas le lancer tout le temps
		if ((IsVelocityTooLow(GetVelocityAtIndex(upperPoint.x, upperPoint.y, upperPoint.z)) ||
            IsVelocityTooLow(GetVelocityAtIndex(lowerPoint.x, upperPoint.y, upperPoint.z)) ||
            IsVelocityTooLow(GetVelocityAtIndex(upperPoint.x, lowerPoint.y, upperPoint.z)) ||
            IsVelocityTooLow(GetVelocityAtIndex(lowerPoint.x, lowerPoint.y, upperPoint.z)) ||
            IsVelocityTooLow(GetVelocityAtIndex(upperPoint.x, upperPoint.y, lowerPoint.z)) ||
            IsVelocityTooLow(GetVelocityAtIndex(lowerPoint.x, upperPoint.y, lowerPoint.z)) ||
            IsVelocityTooLow(GetVelocityAtIndex(upperPoint.x, lowerPoint.y, lowerPoint.z)) ||
            IsVelocityTooLow(GetVelocityAtIndex(lowerPoint.x, lowerPoint.y, lowerPoint.z))) 
            && SolidBoundariesBuilder.Contains(position)) {
			return Vector3.zero;
        }

        //Si les deux points sont les mêmes
        if (upperPoint == lowerPoint)
            return GetVelocityAtIndex(upperPoint.x, upperPoint.y, upperPoint.z);

        //2. Calculer les ratio de positions
        var ratio = pUnit - upperPoint;

        //3. Calculer la vitesse au point voulu
        var interpolatedVelocity = new float[3];
        for (int i = 0; i < 3; i++) {
            var speedComponent =
                _velocityField[upperPoint.x, upperPoint.y, upperPoint.z, i] * (1 - ratio.x) * (1 - ratio.y) * (1 - ratio.z) +
                _velocityField[lowerPoint.x, upperPoint.y, upperPoint.z, i] * ratio.x * (1 - ratio.y) * (1 - ratio.z) +
                _velocityField[upperPoint.x, lowerPoint.y, upperPoint.z, i] * (1 - ratio.x) * ratio.y * (1 - ratio.z) +
                _velocityField[lowerPoint.x, lowerPoint.y, upperPoint.z, i] * ratio.x * ratio.y * (1 - ratio.z) +
                _velocityField[upperPoint.x, upperPoint.y, lowerPoint.z, i] * (1 - ratio.x) * (1 - ratio.y) * ratio.z +
                _velocityField[lowerPoint.x, upperPoint.y, lowerPoint.z, i] * ratio.x * (1 - ratio.y) * ratio.z +
                _velocityField[upperPoint.x, lowerPoint.y, lowerPoint.z, i] * (1 - ratio.x) * ratio.y * ratio.z +
                _velocityField[lowerPoint.x, lowerPoint.y, lowerPoint.z, i] * ratio.x * ratio.y * ratio.z;

            interpolatedVelocity[i] = speedComponent;
        }

        return new Vector3(interpolatedVelocity[0], interpolatedVelocity[1], interpolatedVelocity[2]);
    }
    
    private static Vector3 GetVelocityAtIndex(int x, int y, int z) {
        return new Vector3(_velocityField[x, y, z, 0], _velocityField[x, y, z, 1], _velocityField[x, y, z, 2]);
    }

    private static float GetVelocityAverage() {
        float speedSum = 0f;
        int summedCount = 0;

        for (int x = 0; x <= _velocityField.GetUpperBound(0); x++) {
            for (int y = 0; y <= _velocityField.GetUpperBound(1); y++) {
                for (int z = 0; z <= _velocityField.GetUpperBound(2); z++) {
                    var speed = GetVelocityAtIndex(x, y, z).magnitude;
                    if (speed == 0f)
                        continue;

                    speedSum += speed;
                    summedCount++;
                }
            }
        }

        return speedSum / summedCount;
    }
    
	public static bool IsVelocityTooLow(Vector3 speed) => speed.magnitude <= 0.0001f * VelocityAverage;

	/** Old 
    //Build the speedField Array from the extracted matlab file directly ("monoVarSingle" : 1 Matlab variable de type Single[3,189,175,357] (mais attention à la transposition faite par la librairie))
    public static void BuildSpeedField(string filePath) {
        //Open Matlab file        
        MethodExtensions.LogWithElapsedTime("Build velocity field - Open Matlab file");
        var reader = new MatReader(filePath, false, false); //With autoTranspose=false, it's 10x faster, but it is transposed.

        //Store variables
        MethodExtensions.LogWithElapsedTime("Build velocity field - Read Matlab variable");
        float[,,,] speedFieldMatlab = reader.Read<float[,,,]>(reader.FieldNames[0]); //System.Double[3,189,175,357] <=> [v,z,y,x]

        //Build final Vector3 array       
        MethodExtensions.LogWithElapsedTime("Build velocity field - Build internal Array");
        _velocityField = new Vector3[speedFieldMatlab.GetUpperBound(3) + 2, speedFieldMatlab.GetUpperBound(2) + 2, speedFieldMatlab.GetUpperBound(1) + 2];
        for (int x = 0; x < _velocityField.GetUpperBound(0); x++) {
            for (int y = 0; y < _velocityField.GetUpperBound(1); y++) {
                for (int z = 0; z < _velocityField.GetUpperBound(2); z++) {
                    _velocityField[x + 1, y + 1, z + 1] = new Vector3(speedFieldMatlab[0, z, y, x], speedFieldMatlab[1, z, y, x], speedFieldMatlab[2, z, y, x]);
                }
            }
        }
    }
    */
}