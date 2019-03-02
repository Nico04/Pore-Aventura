using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;


public static class MethodExtensions {
    /** Unused
    public static IEnumerator AsIEnumerator(this Task task) {
        while (!task.IsCompleted) {
            yield return null;
        }

        if (task.IsFaulted) {
            throw task.Exception;
        }
    }*/

    /** Unused
    public static float GetCoordinateByIndex(this Vector3 vec, int coordinateIndex) {
        switch (coordinateIndex) {
            case 0:
                return vec.x;
            case 1:
                return vec.y;
            case 2:
                return vec.z;
            default:
                throw new System.Exception("coordinateIndex must be between 0 and 2");
        }
    }*/

	/***As Vector3 is a struct, any modification done in this struc is not saved because it is passed by value...
    public static void setCoordinateByIndex(this Vector3 vec, int coordinateIndex, float value) {
        switch (coordinateIndex) {
            case 0:
                vec.x = value;
                return;
            case 1:
                vec.y = value;
                return;
            case 2:
                vec.z = value;
                return;
            default:
                throw new System.Exception("coordinateIndex must be between 0 and 2");
        }
    }*/

	/** Unused
    public static string ToStringLong(this Vector3 vec) {
        string[] output = new string[3];
        for (int i = 0; i < output.Length; i++) {
            output[i] = vec.GetCoordinateByIndex(i).ToString("0.###", new System.Globalization.CultureInfo("en-US"));
        }

        return $"({output[0]},{output[1]},{output[2]})";
    }*/

    public static Vector3 SetCoordinate(this Vector3 vector, float? x = null, float? y = null, float? z = null) {
        if (x != null)
            vector.x = x.GetValueOrDefault();
        if (y != null)
            vector.x = y.GetValueOrDefault();
        if (z != null)
            vector.x = z.GetValueOrDefault();
        return vector;
    }

	private static readonly System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
    public static void LogWithElapsedTime(string message) {
		string timeElapsed = Mathf.RoundToInt((float)StopWatch.Elapsed.TotalMilliseconds) + "ms";
        //Debug.Log(message + (StopWatch.IsRunning ? $" ({timeElapsed})" : ""));
        Messager.AddMessage((StopWatch.IsRunning ? $" ({timeElapsed})" + (!string.IsNullOrEmpty(message) ? Environment.NewLine : "") : "") + message);

        StopWatch.Restart();
    }

    //Remove a bit of typo when we doesn't need the context for awaitable task
    //see https://blogs.infinitesquare.com/posts/divers/astuce-asyncawait-et-configureawait
    public static ConfiguredTaskAwaitable NoSync(this Task task) => task.ConfigureAwait(false);
    public static ConfiguredTaskAwaitable<T> NoSync<T>(this Task<T> task) => task.ConfigureAwait(false);

}
