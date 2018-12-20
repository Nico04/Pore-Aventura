using System;
using UnityEngine;

public class FollowStream : MonoBehaviour {
	[HideInInspector]
	public Trajectory Trajectory;
	private int _currentTrajectoryIndex = 0;

	// Update is called once per frame
	private void Update () {
		if (PauseManager.IsPaused)
			return;

		/** Old
		Vector3 speed = DataBase.GetSpeedAtPoint(transform.position);

		if (speed == Vector3.zero) {
			Destroy(gameObject);
			return;
		}

		transform.position += speed * 0.1f;
		*/

		/**
		//For sprite renderer only
		//transform.LookAt(Camera.main.transform);
		*/

		/**
		var newPosition = TrajectoriesManager.Instance.GetNextTrajectoryPosition(Trajectory.Points, ref _currentTrajectoryIndex);

		if (newPosition == Vector3.zero) {
			Destroy(gameObject);
			return;
		}

		transform.position = newPosition;
		*/

		MoveToNextPoint(transform, Trajectory, ref _currentTrajectoryIndex, () => Destroy(gameObject));
	}

	public static void MoveToNextPoint(Transform objectToMove, Trajectory trajectory, ref int currentTrajectoryIndex, Action exitCondition) {
		var newPosition = TrajectoriesManager.Instance.GetNextTrajectoryPosition(trajectory.Points, ref currentTrajectoryIndex);

		if (newPosition == Vector3.zero) {
			exitCondition();
			return;
		}

		objectToMove.position = newPosition;
	}
}
