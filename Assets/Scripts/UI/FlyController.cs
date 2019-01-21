using UnityEngine;
using System.Diagnostics;

public class FlyController : MonoBehaviour {

    /*
	EXTENDED FLYCAM
		Desi Quintans (CowfaceGames.com), 17 August 2012.
		Based on FlyThrough.js by Slin (http://wiki.unity3d.com/index.php/FlyThrough), 17 May 2011.
 
	LICENSE
		Free as in speech, and free as in beer.
 
	FEATURES
		WASD/Arrows:    Movement
		          Q:    Climb
		          E:    Drop
                      Shift:    Move faster
                    Control:    Move slower
                        End:    Toggle cursor locking to screen (you can also press Ctrl+P to toggle play mode on and off).
	*/

    public float CameraSensitivity = 90;
    public float ClimbSpeed = 4;
    public float NormalMoveSpeed = 10;
    public float SlowMoveFactor = 0.25f;
    public float FastMoveFactor = 3;
    public float SmoothTime = 10f;

    [HideInInspector]
	public Quaternion TargetRotation;

    [HideInInspector]
    public bool ForceFollowStream = false;

	private FollowStreamCamera _followStreamCamera;
    private readonly Stopwatch _idleMouseStopwatch = new Stopwatch();

	private void Start() {
        _followStreamCamera = GetComponent<FollowStreamCamera>();

		//Set initial camera rotation
		TargetRotation = transform.localRotation;
	}

	private void Update() {
        if (!PauseManager.IsPaused) {       
            float mouseDeltaX = Input.GetAxis("Mouse X") * CameraSensitivity * Time.deltaTime;
            float mouseDeltaY = Input.GetAxis("Mouse Y") * CameraSensitivity * Time.deltaTime;

            if (mouseDeltaX != 0 || mouseDeltaY != 0) {
                ForceFollowStream = false;
				if (_idleMouseStopwatch.IsRunning)
                    _idleMouseStopwatch.Stop();

				Vector3 targetRotationEuler = TargetRotation.eulerAngles;
                TargetRotation = Quaternion.Euler(ClampEulerAngle(targetRotationEuler.x - mouseDeltaY, -90f, 90f), targetRotationEuler.y + mouseDeltaX, 0f);        //Force euler z to 0 to avoid camera side bend
                TargetRotation = ClampRotationAroundXAxis(TargetRotation);
            }
            else if (_followStreamCamera?.FollowStreamModeEnabled == true) {
                if (!_idleMouseStopwatch.IsRunning)
                    _idleMouseStopwatch.Restart();

                if (_idleMouseStopwatch.ElapsedMilliseconds > 3000 || ForceFollowStream)
				    TargetRotation = Quaternion.LookRotation(_followStreamCamera.CurrentSpeed);
            }            

            transform.localRotation = Quaternion.Slerp(transform.localRotation, TargetRotation, SmoothTime * Time.deltaTime);
            

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                transform.position += transform.forward * (NormalMoveSpeed * FastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (NormalMoveSpeed * FastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
                transform.position += transform.up * (ClimbSpeed * FastMoveFactor) * Input.GetAxis("Elevation") * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                transform.position += transform.forward * (NormalMoveSpeed * SlowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (NormalMoveSpeed * SlowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
                transform.position += transform.up * (ClimbSpeed * SlowMoveFactor) * Input.GetAxis("Elevation") * Time.deltaTime;
            }
            else {
                transform.position += transform.forward * NormalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * NormalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
                transform.position += transform.up * ClimbSpeed * Input.GetAxis("Elevation") * Time.deltaTime;
            }
        }
    }

	private float ClampEulerAngle(float angle, float min, float max) {         
        if (angle > 180f)
            angle -= 360;

        if (angle > max) return max;
        if (angle < min) return min;
        return angle;
    }

	private Quaternion ClampRotationAroundXAxis(Quaternion q) {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

        angleX = Mathf.Clamp(angleX, -90f, 90f);

        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }
}