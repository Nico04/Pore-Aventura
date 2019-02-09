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

	private static readonly System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
    public static void LogWithElapsedTime(string message) {
		string timeElapsed = Mathf.RoundToInt((float)StopWatch.Elapsed.TotalMilliseconds) + "ms";
        Debug.Log(message + (StopWatch.IsRunning ? $" ({timeElapsed})" : ""));
        Messager.AddMessage((StopWatch.IsRunning ? $" ({timeElapsed})" + (!string.IsNullOrEmpty(message) ? Environment.NewLine : "") : "") + message);

        StopWatch.Restart();
    }

    //Remove a bit of typo when we doesn't need the context for awaitable task
    //see https://blogs.infinitesquare.com/posts/divers/astuce-asyncawait-et-configureawait
    public static ConfiguredTaskAwaitable NoSync(this Task task) => task.ConfigureAwait(false);
    public static ConfiguredTaskAwaitable<T> NoSync<T>(this Task<T> task) => task.ConfigureAwait(false);

}

public struct PointInt3 {
    public int x;
    public int y;
    public int z;

    public PointInt3(int X, int y, int z) {
        x = X;
        this.y = y;
        this.z = z;
    }

    public PointInt3(float X, float y, float z) {
        x = (int)Mathf.Round(X);
        this.y = (int)Mathf.Round(y);
        this.z = (int)Mathf.Round(z);
    }

    public PointInt3(Vector3 vec) : this(vec.x, vec.y, vec.z) {
    }

    public static bool operator ==(PointInt3 p1, PointInt3 p2) {
        return p1.Equals(p2);
    }

    public static bool operator !=(PointInt3 p1, PointInt3 p2) {
        return !p1.Equals(p2);
    }

    public override bool Equals(object obj) {
        if (!(obj is PointInt3)) {
            return false;
        }

        var point = (PointInt3)obj;
        return x == point.x &&
                y == point.y &&
                z == point.z;
    }

    public override int GetHashCode() {
        var hashCode = 373119288;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        hashCode = hashCode * -1521134295 + z.GetHashCode();
        return hashCode;
    }

    public static implicit operator Vector3(PointInt3 p) {
        return new Vector3(p.x, p.y, p.z);
    }
}

public static class Messager {
    private static string _message = "";
    public static void AddMessage(string message) {
        _message += message;
    }

    public static bool HasNewMessages => _message.Length != 0;

    public static string GetMessages() {
        string messages = _message;
        _message = "";
        return messages;
    }
}
