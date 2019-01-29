using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Stats : MonoBehaviour {
	public List<GameObject> Holders = new List<GameObject>();
	public GridSpawnerVfx GridSpawnerVfx;
	public TracerInjectionGridGpuBuilder TracerInjectionGridGpuBuilder;

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
		_text.text = ToKMB(Holders.Sum(h => h.transform.childCount) + GridSpawnerVfx.GetTotalParticlesCount() + TracerInjectionGridGpuBuilder.GetTotalParticlesCount()) + " tracers";
		_stopwatch.Restart();
	}

	public static string ToKMB(int n) {
		if (n < 1000)
			return n.ToString();

		if (n < 10000)
			return $"{n - 5:#,.##}K";

		if (n < 100000)
			return $"{n - 50:#,.#}K";

		if (n < 1000000)
			return $"{n - 500:#,.}K";

		if (n < 10000000)
			return $"{n - 5000:#,,.##}M";

		if (n < 100000000)
			return $"{n - 50000:#,,.#}M";

		if (n < 1000000000)
			return $"{n - 500000:#,,.}M";

		return $"{n - 5000000:#,,,.##}B";
	}
}
