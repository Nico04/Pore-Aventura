using UnityEngine;

public class FPSDisplay : MonoBehaviour {
    private float _deltaTime = 0.0f;

    private void Update() {
        _deltaTime += (Time.deltaTime - _deltaTime) * 0.1f;
    }

    private void OnGUI() {
        if (Time.timeScale <= 0.01f)
            return;

		int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        float msec = _deltaTime * 1000.0f;
        float fps = 1.0f / _deltaTime;
        string text = $"{msec:0.0} ms ({fps:0.} fps)";
        GUI.Label(rect, text, style);
    }
}