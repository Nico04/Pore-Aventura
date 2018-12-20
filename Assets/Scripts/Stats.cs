using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Stats : MonoBehaviour {
	public List<GameObject> Holders = new List<GameObject>();

	private Text _text;

	private void Start () {
		_text = GetComponent<Text>();
		UpdateStats();
	}
	
	private Stopwatch _stopwatch = Stopwatch.StartNew();

	private void Update () {
		if (PauseManager.IsPaused || _stopwatch.ElapsedMilliseconds < 1000)
			return;

		UpdateStats();
	}

	private void UpdateStats() {
		_text.text = Holders.Sum(h => h.transform.childCount) + " tracers";
		_stopwatch.Restart();
	}
}
