using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using System.IO;

public class Loader : MonoBehaviour {
    private bool _loading = false;
    private bool _loadingError = false;
    private AsyncOperation _asyncSceneLoad;

    [SerializeField]
    private string _scene;
    [SerializeField]
    private Text _loadingText;
    [SerializeField]
    private Text _loadingDetailsText;

	private async void Start() {
        try {
            _loading = true;
            await Load(DataBase.BuildSpeedfield);
        } catch (Exception e) {
            _loadingError = true;

            if (_loadingText != null) {
                _loadingText.color = Color.red;
                _loadingText.text = e.Message;
            }
        } finally {
            _loading = false;
        }
    }

    // Updates once per frame
    private void Update() {
         _loadingText.color = new Color(_loadingText.color.r, _loadingText.color.g, _loadingText.color.b, 0.2f + Mathf.PingPong(Time.time, 0.8f));
    }

    private void OnGUI() {
        if (!_loading && !_loadingError) {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && Input.GetKeyDown(e.keyCode)) {
                StartGame();
            }
        }

        if (Messager.HasNewMessages)
            _loadingDetailsText.text += Messager.GetMessages();
	}

    private async Task Load(Action<string> action) {
        //Run main script       
        string dataBasePath = Path.Combine(Application.streamingAssetsPath, "SpeedField.mat");    //TODO move
        await Task.Run(() => action(dataBasePath));         //TODO handle errors        

		//Load scene
		MyExtensions.LogWithElapsedTime("Load main scene");
		await this.StartCoroutineAsync(PreLoadScene());
        MyExtensions.LogWithElapsedTime("");

		//Done
#if DEBUG
		StartGame();
#else
        _loading = false;
        _loadingText.text = "Press any key to start";
        _loadingText.color = Color.magenta;
#endif
	}

	private IEnumerator PreLoadScene() {
        //Pre-Load main scene
        _asyncSceneLoad = SceneManager.LoadSceneAsync(_scene);
        _asyncSceneLoad.allowSceneActivation = false;

        //Wait for scene load. 
        while (_asyncSceneLoad.progress < 0.89f) { //When allowSceneActivation is set to false then progress is stopped at 0.9. The isDone is then maintained at false.
            yield return null;
        }
    }

    private void StartGame() {
        //Finish the load of the main scene, allowing to destroy the current one
        _asyncSceneLoad.allowSceneActivation = true;
    }
}