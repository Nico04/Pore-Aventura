using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Accord.IO;

public static class DataBase  {
    private const char LineSeperater = '\n'; // It defines line seperate character
    private const char FieldSeperator = ','; // It defines field seperate chracter

    public static Vector3[,,] VelocityField;        //3D speedField in a normalized coordinates space. Une
    public const float SpaceFactor = 0.05044f;      //0.050438596491228f;

    //Build the speedField Array from the extracted matlab file directly ("monoVarSingle" : 1 Matlab variable de type Single[3,189,175,357] (mais attention à la transposition faite par la librairie))
    public static void BuildSpeedfield(string filePath) {
		//Open Matlab file        
		MethodExtensions.LogWithElapsedTime("Build velocity field - Open Matlab file");
		var reader = new MatReader(filePath, false, false);  //With autoTranspose=false, it's 10x faster, but it is transposed.

		//Store variables
		MethodExtensions.LogWithElapsedTime("Build velocity field - Read Matlab variable");
		float[,,,] speedFieldMatlab = reader.Read<float[,,,]>(reader.FieldNames[0]);  //System.Double[3,189,175,357] <=> [v,z,y,x]

		//Build final Vector3 array       
		MethodExtensions.LogWithElapsedTime("Build velocity field - Build internal Array");
		VelocityField = new Vector3[speedFieldMatlab.GetUpperBound(3) + 2, speedFieldMatlab.GetUpperBound(2) + 2, speedFieldMatlab.GetUpperBound(1) + 2];
        for (int x = 0; x < VelocityField.GetUpperBound(0); x++) {
            for (int y = 0; y < VelocityField.GetUpperBound(1); y++) {
                for (int z = 0; z < VelocityField.GetUpperBound(2); z++) {
                    VelocityField[x + 1, y + 1, z + 1] = new Vector3(speedFieldMatlab[0, z, y, x], speedFieldMatlab[1, z, y, x], speedFieldMatlab[2, z, y, x]);
                }
            }
        }
    }

    //Interpole la vitesse au point p de l'espace donné
    //p : point où l’interpolation est souhaitée
    //speedField : champ des vitesse. Tableau de Vector3 à 3 dimensions dont les indices sont les coordonnées spatiales (X, Y, Z) au facteur près 
    //spaceFactor : facteur d'espace (pour passage des unités de l'espace à un indice du tableau speedField)
    public static Vector3 GetSpeedAtPoint(Vector3 p) {
        if (VelocityField == null) {
            Debug.LogError($"{nameof(VelocityField)} must be initiated");
            return Vector3.zero;
        }

        //1. Trouver les points 8 voisins de p (stockés dans 2 points représentant les extrêmes)
        //Ramener le point p dans l'espace unitaire de speedField
        Vector3 pUnit = p / SpaceFactor;

        //Point précédent
        PointInt3 previousPoint = new PointInt3(Mathf.FloorToInt(pUnit.x), Mathf.FloorToInt(pUnit.y), Mathf.FloorToInt(pUnit.z));

        //Point suivant
        PointInt3 nextPoint = new PointInt3(Mathf.CeilToInt(pUnit.x), Mathf.CeilToInt(pUnit.y), Mathf.CeilToInt(pUnit.z));

        //Vérifier que les points existent
        if (previousPoint.x <= 0 ||
            previousPoint.y <= 0 ||
            previousPoint.z <= 0 ||
            nextPoint.x <= 0 ||
            nextPoint.y <= 0 ||
            nextPoint.z <= 0 ||
            previousPoint.x > VelocityField.GetUpperBound(0) ||
            previousPoint.y > VelocityField.GetUpperBound(1) ||
            previousPoint.z > VelocityField.GetUpperBound(2) ||
            nextPoint.x > VelocityField.GetUpperBound(0) ||
            nextPoint.y > VelocityField.GetUpperBound(1) ||
            nextPoint.z > VelocityField.GetUpperBound(2))
            return Vector3.zero;

        //Si les deux points sont les mêmes
        if (previousPoint == nextPoint)
            return VelocityField[previousPoint.x, previousPoint.y, previousPoint.z];

        //2. Calculer les ratio de positions
        Vector3 ratio = pUnit - previousPoint;

        //3. Calculer la vitesse au point voulu
        float[] interpolatedSpeed = new float[3];
        for (int i = 0; i < 3; i++) {
            float speedComponent =
                VelocityField[previousPoint.x, previousPoint.y, previousPoint.z].GetCoordinateByIndex(i) * (1 - ratio.x) * (1 - ratio.y) * (1 - ratio.z) +
                VelocityField[nextPoint.x, previousPoint.y, previousPoint.z].GetCoordinateByIndex(i) * ratio.x * (1 - ratio.y) * (1 - ratio.z) +
                VelocityField[previousPoint.x, nextPoint.y, previousPoint.z].GetCoordinateByIndex(i) * (1 - ratio.x) * ratio.y * (1 - ratio.z) +
                VelocityField[nextPoint.x, nextPoint.y, previousPoint.z].GetCoordinateByIndex(i) * ratio.x * ratio.y * (1 - ratio.z) +
                VelocityField[previousPoint.x, previousPoint.y, nextPoint.z].GetCoordinateByIndex(i) * (1 - ratio.x) * (1 - ratio.y) * ratio.z +
                VelocityField[nextPoint.x, previousPoint.y, nextPoint.z].GetCoordinateByIndex(i) * ratio.x * (1 - ratio.y) * ratio.z +
                VelocityField[previousPoint.x, nextPoint.y, nextPoint.z].GetCoordinateByIndex(i) * (1 - ratio.x) * ratio.y * ratio.z +
                VelocityField[nextPoint.x, nextPoint.y, nextPoint.z].GetCoordinateByIndex(i) * ratio.x * ratio.y * ratio.z;

            interpolatedSpeed[i] = speedComponent;
        }

        return new Vector3(interpolatedSpeed[0], interpolatedSpeed[1], interpolatedSpeed[2]);
    }

    public static bool IsSpeedTooLow(Vector3 speed) => speed.magnitude <= 0.0001f;

    public static List<Vector3>[] CsvToVector3List(string data) {
        //Split each lines
        string[] lines = data.Split(LineSeperater);

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
                fields[i] = lines[dataLineIndex * 3 + i].Split(FieldSeperator);

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

#region Old methods
#if false
    //Build the speedField Array from the extracted matlab file directly ("monoVarArray" : 1 Matlab variable de type Double[3,189,175,357] (mais attention à la transposition faite par la librairie))
    public static void BuildSpeedfield3(string filePath) {
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch(); stopWatch.Start();

        //Open Matlab file        
        var reader = new MatReader(filePath, false, false);  //TODO handle error     //With autoTranspose=false, it's 10x faster, but it is transposed.
        MyExtensions.LogWithElapsedTime("Open Matlab file", stopWatch);

        //Store variables        
        Double[,,,] speedField_Matlab = reader.Read<Double[,,,]>(reader.FieldNames[0]);  //System.Double[3,189,175,357] <=> [v,z,y,x]
        MyExtensions.LogWithElapsedTime("Open Matlab variable", stopWatch);

        //Build final Vector3 array             
        speedField = new Vector3[speedField_Matlab.GetUpperBound(3) + 2, speedField_Matlab.GetUpperBound(2) + 2, speedField_Matlab.GetUpperBound(1) + 2];
        for (int x = 0; x < speedField.GetUpperBound(0); x++) {
            for (int y = 0; y < speedField.GetUpperBound(1); y++) {
                for (int z = 0; z < speedField.GetUpperBound(2); z++) {
                    speedField[x+1, y+1, z+1] = new Vector3((float)speedField_Matlab[0, z, y, x], (float)speedField_Matlab[1, z, y, x], (float)speedField_Matlab[2, z, y, x]);
                }
            }
        }

        MyExtensions.LogWithElapsedTime("Build final Vector3 Array", stopWatch);
    }

    



    //Build the speedField Array from the extracted matlab file directly ("SpeedField3D" : 4 Matlab variable (VX, VY, VZ of type Float [357,175,189] and spaceFactor of type float)
    public static Vector3[,,] BuildSpeedfield2(string filePath) {
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch(); stopWatch.Start();

        //Open Matlab file
        MyExtensions.LogWithElapsedTime("Open Matlab file", stopWatch);
        var reader = new MatReader(filePath, false, false);     //With autoTranspose=true, it's 10x slower !

        //Store variables
        MyExtensions.LogWithElapsedTime("Open Matlab variables", stopWatch);
        float spaceFactor = reader.Read<float>("spaceFactor");
        float[][,,] matlab_speedField = new float[3][,,];
        for (int i = 0; i < matlab_speedField.Length; i++) {
            matlab_speedField[i] = reader.Read<float[,,]>(reader.FieldNames[i]);
        }

        //Build final Vector3 array
        MyExtensions.LogWithElapsedTime("Build final Vector3 Array", stopWatch);
        int percentage = 0;//TODO remove
        Vector3[,,] speedField = new Vector3[matlab_speedField[0].GetUpperBound(0) + 1, matlab_speedField[0].GetUpperBound(1) + 1, matlab_speedField[0].GetUpperBound(2) + 1];
        for (int x = 0; x <= speedField.GetUpperBound(0); x++) {
            for (int y = 0; y <= speedField.GetUpperBound(1); y++) {
                for (int z = 0; z <= speedField.GetUpperBound(2); z++) {
                    speedField[x, y, z] = new Vector3(matlab_speedField[0][x, y, z], matlab_speedField[1][x, y, z], matlab_speedField[2][x, y, z]);
                }
            }
        }

        MyExtensions.LogWithElapsedTime("end", stopWatch);
        return speedField;
    }





    

    //Build the speedField Array from the extracted matlab file directly ("XYZ_VXVYVZ" : 6 lines into one Matlab variable)
    public static Vector3[,,] BuildSpeedfield(string filePath) {
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch(); stopWatch.Start();
        Debug.Log("BuildSpeedfield Start");

        /** Method with Accord.NET */
        var reader = new MatReader(filePath, false, false);     //With autoTranspose=true, it's 10x slower !
        float[,] dataTransposed = reader.Read<float[,]>(reader.FieldNames[0]);        //Data is not transposed for performance, but it's stored transposed in regard of what we want
        MyExtensions.LogWithElapsedTime("Open Matlab file", stopWatch);

        //Normalize space coordinates        
        int[,] unsortedNormalizedCoordinates = new int[3, dataTransposed.GetUpperBound(0) + 1];
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j <= dataTransposed.GetUpperBound(0); j++) {
                unsortedNormalizedCoordinates[i, j] = (int)Math.Round(dataTransposed[j,i] / spaceFactor);
            }
        }
        MyExtensions.LogWithElapsedTime($"Normalize space coordinate ([{dataTransposed.GetUpperBound(0)},{dataTransposed.GetUpperBound(1)}])", stopWatch);

        //Find max for each spacial dimension        
        int[] maxLengths = new int[3];
        for (int i = 0; i < maxLengths.Length; i++) {
            int max = 0;
            for (int j = 0; j <= unsortedNormalizedCoordinates.GetUpperBound(1); j++) {
                max = Math.Max(unsortedNormalizedCoordinates[i, j], max);
            }
            maxLengths[i] = max;
        }
        MyExtensions.LogWithElapsedTime("Find max for each spacial dimension", stopWatch);

        //Fill final array        
        //int percentage = 0;//TODO remove
        Vector3[,,] speeds = new Vector3[maxLengths[0] + 1, maxLengths[1] + 1, maxLengths[2] + 1];
        for (int i = 0; i <= unsortedNormalizedCoordinates.GetUpperBound(1); i++) {
            speeds[unsortedNormalizedCoordinates[0, i], unsortedNormalizedCoordinates[1, i], unsortedNormalizedCoordinates[2, i]]
                = new Vector3(dataTransposed[i,3], dataTransposed[i,4], dataTransposed[i,5]);

            //System.Threading.Thread.Sleep(500);

            //Debug
            /*if (percentage != i * 100 / unsortedNormalizedCoordinates.GetUpperBound(1))
                MyExtensions.LogWithElapsedTime((percentage = i * 100 / unsortedNormalizedCoordinates.GetUpperBound(1)).ToString(), stopWatch);*/

            //Debug.Log(Math.Round(i * 100f / unsortedNormalizedCoordinates.GetUpperBound(1), 2));

            //Debug.Log("speed[" + unsortedNormalizedCoordinates[0, i] + "," + unsortedNormalizedCoordinates[1, i] + "," + unsortedNormalizedCoordinates[2, i] + speeds[unsortedNormalizedCoordinates[0, i], unsortedNormalizedCoordinates[1, i], unsortedNormalizedCoordinates[2, i]]);
        }

        MyExtensions.LogWithElapsedTime("Fill final array", stopWatch);
        return speeds;



        /* Method with csmatio.rev14
        //Open and Extract matlab file/data 
        MyExtensions.LogWithElapsedTime("Open Matlab file", stopWatch);     
        var matLabFile = new MatFileReader(filePath);  //(100sec)  
        
        MyExtensions.LogWithElapsedTime("Get first matlab variable", stopWatch);
        MLArray mlArray = matLabFile.Content.FirstOrDefault().Value;
        if (!mlArray.IsSingle) throw new Exception("Wrong matlab type");
        
        MyExtensions.LogWithElapsedTime("Convert to an array", stopWatch);
        float[][] data = (mlArray as MLSingle).GetArray();  //(100sec)
        

        
        //Normalize space coordinates
        MyExtensions.LogWithElapsedTime("Normalize space coordinate", stopWatch);
        int[,] unsortedNormalizedCoordinates = new int[3, data[0].Length];
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < data[i].Length; j++) {                
                unsortedNormalizedCoordinates[i, j] = (int)Math.Round(data[i][j] / SpaceFactor);                
            }
        }

        //Find max for each spacial dimension
        MyExtensions.LogWithElapsedTime("Find max for each spacial dimension", stopWatch);
        int[] maxLengths = new int[3];
        for (int i = 0; i < maxLengths.Length; i++) {
            int max = 0;
            for (int j = 0; j <= unsortedNormalizedCoordinates.GetUpperBound(1); j++) {
                max = Math.Max(unsortedNormalizedCoordinates[i, j], max);
            }
            maxLengths[i] = max;
        }

        //Fill final array
        int percentage = 0;//TODO remove
        Vector3[,,] speeds = new Vector3[maxLengths[0] + 1, maxLengths[1] + 1, maxLengths[2] + 1];
        for (int i = 0; i <= unsortedNormalizedCoordinates.GetUpperBound(1); i++) {
            speeds[unsortedNormalizedCoordinates[0, i], unsortedNormalizedCoordinates[1, i], unsortedNormalizedCoordinates[2, i]]
                = new Vector3(data[3][i], data[4][i], data[5][i]);

            //System.Threading.Thread.Sleep(500);

            //Debug
            if (percentage != i * 100 / unsortedNormalizedCoordinates.GetUpperBound(1))
                MyExtensions.LogWithElapsedTime((percentage = i * 100 / unsortedNormalizedCoordinates.GetUpperBound(1)).ToString(), stopWatch);

            //Debug.Log(Math.Round(i * 100f / unsortedNormalizedCoordinates.GetUpperBound(1), 2));

            //Debug.Log("speed[" + unsortedNormalizedCoordinates[0, i] + "," + unsortedNormalizedCoordinates[1, i] + "," + unsortedNormalizedCoordinates[2, i] + speeds[unsortedNormalizedCoordinates[0, i], unsortedNormalizedCoordinates[1, i], unsortedNormalizedCoordinates[2, i]]);
        }

        return speeds;
        */
    }


    //Renvoi le champ de vitesse depuis une BDD texte. Chaque index correspond à une coordonées spaciale à multiplier par le "SpaceFactor" 
    //Function way too long with full BDD, maybe because of the try-catch bloc ?
    public static Vector3[,,] ExtractSpeedfieldDataFromCSV(string data) {
        //Split each lines (10sec)
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch(); stopWatch.Start();
        MyExtensions.LogWithElapsedTime("Split each lines", stopWatch);
        string[] lines = data.Split(new char[] { lineSeperater }, StringSplitOptions.RemoveEmptyEntries);

        //Split each fields (30sec)
        MyExtensions.LogWithElapsedTime("Split each fields", stopWatch);
        string[][] fields = new string[lines.Length][];
        for (int i = 0; i < fields.Length; i++) {
            fields[i] = lines[i].Split(fieldSeperator);
        }

        //Convert into numbers + divide by SpaceFactor (6min)        
        MyExtensions.LogWithElapsedTime("Convert into numbers 1", stopWatch);
        int[,] unsortedNormalizedCoordinates = new int[3, fields[0].Length];
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < fields[i].Length; j++) {
                try {
                    unsortedNormalizedCoordinates[i, j] = (int)Math.Round(float.Parse(fields[i][j], new CultureInfo("en-US").NumberFormat) / spaceFactor);
                }
                catch (FormatException e) {
                    Debug.Log("Error while parsing CSV data : " + e.Message + " : " + unsortedNormalizedCoordinates[i, j]);
                    return null;
                }
            }
        }

        //Convert into numbers (6min)
        MyExtensions.LogWithElapsedTime("Convert into numbers 2", stopWatch);
        float[,] unsortedSpeeds = new float[3, fields[3].Length];
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < fields[i].Length; j++) {
                try {
                    unsortedSpeeds[i, j] = float.Parse(fields[i + 3][j], new CultureInfo("en-US").NumberFormat);
                }
                catch (FormatException e) {
                    Debug.Log("Error while parsing CSV data : " + e.Message + " : " + unsortedSpeeds[i, j]);
                    return null;
                }
            }
        }

        //Find max for each spacial dimension
        MyExtensions.LogWithElapsedTime("Find max for each spacial dimension", stopWatch);
        int[] maxLengths = new int[3];
        for (int i = 0; i < maxLengths.Length; i++) {
            int max = 0;
            for (int j = 0; j <= unsortedNormalizedCoordinates.GetUpperBound(1); j++) {
                max = Math.Max(unsortedNormalizedCoordinates[i, j], max);
            }
            maxLengths[i] = max;
        }

        //Fill final array (3sec)
        int percentage = 0;//TODO remove
        Vector3[,,] speeds = new Vector3[maxLengths[0] + 1, maxLengths[1] + 1, maxLengths[2] + 1];
        for (int i = 0; i <= unsortedNormalizedCoordinates.GetUpperBound(1); i++) {
            speeds[unsortedNormalizedCoordinates[0, i], unsortedNormalizedCoordinates[1, i], unsortedNormalizedCoordinates[2, i]]
                = new Vector3(unsortedSpeeds[0, i], unsortedSpeeds[1, i], unsortedSpeeds[2, i]);

            //System.Threading.Thread.Sleep(500);

            //Debug
            if (percentage != i * 100 / unsortedNormalizedCoordinates.GetUpperBound(1))
                MyExtensions.LogWithElapsedTime((percentage = i * 100 / unsortedNormalizedCoordinates.GetUpperBound(1)).ToString(), stopWatch);

            //Debug.Log(Math.Round(i * 100f / unsortedNormalizedCoordinates.GetUpperBound(1), 2));

            //Debug.Log("speed[" + unsortedNormalizedCoordinates[0, i] + "," + unsortedNormalizedCoordinates[1, i] + "," + unsortedNormalizedCoordinates[2, i] + speeds[unsortedNormalizedCoordinates[0, i], unsortedNormalizedCoordinates[1, i], unsortedNormalizedCoordinates[2, i]]);
        }

        return speeds;
    }

    
#endif
#endregion
}
