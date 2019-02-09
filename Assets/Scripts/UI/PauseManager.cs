using UnityEngine;

public class PauseManager : MonoBehaviour {
    public GameObject PauseUi;
    public GameObject ToBeHidden;

    public TracerInjectionGridGpuBuilder TracerInjectionGridGpuBuilder;

    private static PauseManager _instance;

    private void Start() {
        _instance = this;
    }

    private bool _keyIsDown = false;

    private void OnGUI() {  //Called several times per frame
        if (!_keyIsDown && Input.GetButtonDown("Pause")) {      
            IsPaused = !IsPaused;
            _keyIsDown = true;
        }
        else if (_keyIsDown && Input.GetButtonUp("Pause")) {
            _keyIsDown = false;
        }
    }

    private void OnApplicationFocus(bool hasFocus) {
        if (!hasFocus) IsPaused = true;
    }

    private void OnApplicationPause(bool pauseStatus) {
        if (pauseStatus) IsPaused = true;
    }

    private static bool _isPaused = false;
    public static bool IsPaused {
        get => _isPaused;
        private set {
            if (_isPaused == value) return;

            _isPaused = value;
            _instance.PauseUi.SetActive(_isPaused);
            Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;           //Warning : escape key doesn't work well in the editor (disable the screen lock)

			//Hide some UI
            _instance.ToBeHidden.SetActive(!_isPaused);

			//Handle Vfx
			_instance.TracerInjectionGridGpuBuilder.Pause(_isPaused);
		}
	}
}


