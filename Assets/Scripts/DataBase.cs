using System.IO;
using UnityEngine;
using Hdf5DotNetTools;

public static class DataBase {
	private static double[,] _solidBoundaries; // Solid Boundaries Array [i, position(x,y,z)]. Example : [0, 1] will return the y coordinate of the first solid boundary.
    private static double[,,,] _velocityField; // Speed Field  Array [velocity(x,y,z), x, y, z]. Example : [2, 5, 5, 5] will return the z coordinate of the velocity at the point (5, 5, 5)

    private const float VelocityFieldSpaceFactor = 0.050438596491228f; //facteur d'espace (pour passage des unités de l'espace à un indice du tableau speedField)

    public static Vector3 DataSpaceSize;

    //Load data from the HDF5 file. This operation is very quick (<250ms).
    public static void LoadData() {
        var path = Path.Combine(Application.streamingAssetsPath, "Data.h5");

        //Open file
        var fileId = Hdf5.OpenFile(path, true);

        //Get datasets
        _solidBoundaries = (double[,]) Hdf5.ReadDatasetToArray<double>(fileId, "/SolidBoundaries");
        _velocityField = (double[,,,]) Hdf5.ReadDatasetToArray<double>(fileId, "/SpeedField");

        //Set space size
        DataSpaceSize = new Vector3(_velocityField.GetUpperBound(1), _velocityField.GetUpperBound(2), _velocityField.GetUpperBound(3)) * VelocityFieldSpaceFactor;

        //TODO DEBUG ONLY
        DataSpaceSize = new Vector3(18f, 10f, 10f);
}

    //Return a Vector3 array of the positions of the solid boundaries
	public static Vector3[] GetSolidBoundaries() {
        if (_solidBoundaries == null) {
            Debug.LogError($"{nameof(_solidBoundaries)} must be initiated");
            return new Vector3[0];
        }

        //Convert to a Vector3 array to ease reading
        var solidBoundaries = new Vector3[_solidBoundaries.GetUpperBound(0) + 1];
        for (int i = 0; i < solidBoundaries.Length; i++) {
            solidBoundaries[i] = new Vector3((float) _solidBoundaries[i, 0], (float) _solidBoundaries[i, 1], (float) _solidBoundaries[i, 2]);
        }

        //Get max (x, y, z)
        //var max = new  Vector3(solidBoundaries.Max(sb => sb.x), solidBoundaries.Max(sb => sb.y), solidBoundaries.Max(sb => sb.z));

        return solidBoundaries;
    }

    //Interpole la vitesse au point p de l'espace donné
    //p : point où l’interpolation est souhaitée
    //speedField : champ des vitesse. Tableau de Vector3 à 3 dimensions dont les indices sont les coordonnées spatiales (X, Y, Z) au facteur près 
    //spaceFactor : facteur d'espace (pour passage des unités de l'espace à un indice du tableau speedField)
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
            upperPoint.x > _velocityField.GetUpperBound(1) ||
            upperPoint.y > _velocityField.GetUpperBound(2) ||
            upperPoint.z > _velocityField.GetUpperBound(3) ||
            lowerPoint.x > _velocityField.GetUpperBound(1) ||
            lowerPoint.y > _velocityField.GetUpperBound(2) ||
            lowerPoint.z > _velocityField.GetUpperBound(3))
            return Vector3.zero;

        //Si les deux points sont les mêmes
        if (upperPoint == lowerPoint)
            return GetVelocityAtPoint(upperPoint.x, upperPoint.y, upperPoint.z);

        //2. Calculer les ratio de positions
        var ratio = pUnit - upperPoint;

        //3. Calculer la vitesse au point voulu
        var interpolatedSpeed = new double[3];
        for (int i = 0; i < 3; i++) {
            var speedComponent =
                _velocityField[i, upperPoint.x, upperPoint.y, upperPoint.z] * (1 - ratio.x) * (1 - ratio.y) * (1 - ratio.z) +
                _velocityField[i, lowerPoint.x, upperPoint.y, upperPoint.z] * ratio.x * (1 - ratio.y) * (1 - ratio.z) +
                _velocityField[i, upperPoint.x, lowerPoint.y, upperPoint.z] * (1 - ratio.x) * ratio.y * (1 - ratio.z) +
                _velocityField[i, lowerPoint.x, lowerPoint.y, upperPoint.z] * ratio.x * ratio.y * (1 - ratio.z) +
                _velocityField[i, upperPoint.x, upperPoint.y, lowerPoint.z] * (1 - ratio.x) * (1 - ratio.y) * ratio.z +
                _velocityField[i, lowerPoint.x, upperPoint.y, lowerPoint.z] * ratio.x * (1 - ratio.y) * ratio.z +
                _velocityField[i, upperPoint.x, lowerPoint.y, lowerPoint.z] * (1 - ratio.x) * ratio.y * ratio.z +
                _velocityField[i, lowerPoint.x, lowerPoint.y, lowerPoint.z] * ratio.x * ratio.y * ratio.z;

            interpolatedSpeed[i] = speedComponent;
        }

        return new Vector3((float)interpolatedSpeed[0], (float)interpolatedSpeed[1], (float)interpolatedSpeed[2]);
    }
    
    private static Vector3 GetVelocityAtPoint(int x, int y, int z) {
        return new Vector3((float) _velocityField[0, x, y, z], (float) _velocityField[0, x, y, z], (float) _velocityField[0, x, y, z]);
    }
    
	public static bool IsSpeedTooLow(Vector3 speed) => speed.magnitude <= 0.0001f;

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

	/** Old Csv Methods    
    private const char LineSeparator = '\n'; // It defines line separate character
    private const char FieldSeparator = ','; // It defines field separate character

    public static List<Vector3>[] CsvToVector3List(string data) {
        //Split each lines
        string[] lines = data.Split(LineSeparator);

        //Check number of lines
        if ((lines.Length / 3f) % 1 != 0) {
            Debug.Log("Error while parsing CSV data : " + "Incorrect number of lines : " + lines.Length);
            return null;
        }

        //Final array of list of Vector3 to be filled
        List<Vector3>[] positions = new List<Vector3>[lines.Length / 3];

        //Go through data lines by 3
        for (int dataLineIndex = 0; dataLineIndex < lines.Length / 3; dataLineIndex++) {
            //Split each lines
            string[][] fields = new string[3][];
            int? numberOfItems = null;
            for (int i = 0; i < fields.Length; i++) {
                fields[i] = lines[dataLineIndex * 3 + i].Split(FieldSeparator);

                //Removes all zero at the end (empty values)
                RemoveTrailingZeros(ref fields[i]);

                //TODO DEBUG ONLY SET MAX LENGTH
                if (fields[i].Length > 10000) Array.Resize(ref fields[i], 10000);

                // Check all 3 lines have same length
                if (numberOfItems != null && fields[i].Length != numberOfItems) {
                    Debug.Log("Error while parsing CSV data : " + "3 associated lines have different lentgh : " + dataLineIndex);
                    return null;
                }
                numberOfItems = fields[i].Length;
            }

            //Create Vector3
            positions[dataLineIndex] = new List<Vector3>();
            for (int fieldIndex = 0; fieldIndex < fields[0].Length; fieldIndex++) {
                //Convert each field into a float
                float[] coordinates = new float[3];
                for (int i = 0; i < coordinates.Length; i++) {
                    try {
                        coordinates[i] = float.Parse(fields[i][fieldIndex], new CultureInfo("en-US").NumberFormat);
                    }
                    catch (Exception e) {
                        Debug.Log("Error while parsing CSV data : " + e.Message + " : " + fields[i][fieldIndex]);
                        return null;
                    }
                }

                positions[dataLineIndex].Add(new Vector3(coordinates[0], coordinates[1], coordinates[2]));
            }
        }

        return positions;
    }

	private static void RemoveTrailingZeros(ref string[] data) {
        int lastNonNullIndex = data.Length;
        for (int i = data.Length - 1; i >= 0; i--) {
            if (data[i] != "0") {
                lastNonNullIndex = i;
                break;
            }
        }

        Array.Resize(ref data, lastNonNullIndex + 1);
    }
    */
}