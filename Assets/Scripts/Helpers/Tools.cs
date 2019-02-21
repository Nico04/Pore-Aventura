using UnityEngine;

public static class Tools {
	/// True modulo function
	/// C# and C++'s % operator is actually NOT a modulo, it's remainder
	/// See https://stackoverflow.com/a/6400477/4037891
	public static float TrueModulo(float a, float b) {
		return a - b * Mathf.Floor(a / b);
	}
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
