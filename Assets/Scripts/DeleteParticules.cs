using System.Collections.Generic;
using UnityEngine;

public class DeleteParticules : MonoBehaviour {
	public List<GameObject> Holders;

	public StreamParticles StreamParticles;
	public GridSpawner GridSpawner;

	private bool _keyIsDown = false;

	private void OnGUI () {
		if (!_keyIsDown && Input.GetKeyDown(KeyCode.Delete) && !PauseManager.IsPaused) {
			_keyIsDown = true;

			//Delete all spawners
			StreamParticles.DeleteSpawners();

			//Delete all particules
			foreach (var parent in Holders) {
				for (int i = 0; i < parent.transform.childCount; i++) {
					Destroy(parent.transform.GetChild(i).gameObject);
				}
			}

			//Re-build static spawn grid
			GridSpawner.AskRebuild();
		} else if (_keyIsDown && Input.GetKeyUp(KeyCode.Delete)) {
			_keyIsDown = false;
		}
	}
}
