using UnityEngine;

public class FollowStreamCamera : MonoBehaviour {
    private bool _followStreamModeEnabled = false;
    public bool FollowStreamModeEnabled {
        get { return _followStreamModeEnabled; }
        set {
            _followStreamModeEnabled = value;
            doCenterCamera = true;

            //Build trajectory
            Trajectory = TrajectoriesManager.Instance.BuildTrajectory(transform.position);
            _currentTrajectoryIndex = 0;

			//Toggle enable state of the camera's collider to make sure it follows the stream particules
			var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = !_followStreamModeEnabled;
        }
    }

    [HideInInspector]
	public Trajectory Trajectory;
    private int _currentTrajectoryIndex = 0;

	[HideInInspector]
	public Vector3 CurrentSpeed;

	private bool doCenterCamera = false;

    // Update is called once per frame
    private void Update() {
        if (!PauseManager.IsPaused && FollowStreamModeEnabled) {
            /** Old
            CurrentSpeed = DataBase.GetSpeedAtPoint(transform.position);
            if (CurrentSpeed == Vector3.zero) {
                FollowStreamModeEnabled = false;
                return;
            }

            transform.position += CurrentSpeed * 0.1f;
            */

            Vector3 currentPosition = transform.position;

            FollowStream.MoveToNextPoint(transform, Trajectory, ref _currentTrajectoryIndex, () => FollowStreamModeEnabled = false);
            if (!FollowStreamModeEnabled) return;

            CurrentSpeed = transform.position - currentPosition;

			//Make camera look forward just once mode is activated
			if (doCenterCamera) {
                var controller = GetComponent<FlyController>();
                controller.TargetRotation = Quaternion.LookRotation(CurrentSpeed);
                controller.ForceFollowStream = true;
				doCenterCamera = false;
            }
        }
    }

    private bool _keyIsDown = false;
    private void OnGUI() {
        if (!_keyIsDown && Input.GetButtonDown("ToggleRollercoasterMode") && !PauseManager.IsPaused) {
            _keyIsDown = true;
			FollowStreamModeEnabled = !FollowStreamModeEnabled;
        }
        else if (_keyIsDown && Input.GetButtonUp("ToggleRollercoasterMode")) {
            _keyIsDown = false;
		} else if (FollowStreamModeEnabled && (Input.GetButtonDown("Vertical") || Input.GetButtonDown("Horizontal") || Input.GetButtonDown("Elevation") || Input.GetButtonDown("Pause"))) {
            FollowStreamModeEnabled = false;
        }
    }
}
