using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class DeleteParticules : MonoBehaviour {
	public List<GameObject> Holders;

	public TracerManualInjectionBuilder TracerManualInjectionBuilder;
	public TracerInjectionGridBuilder TracerInjectionGridBuilder;
	public VisualEffect GridSpawnerVfxBatch;

	private bool _keyIsDown = false;

	private void OnGUI () {
		if (!_keyIsDown && (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) && !PauseManager.IsPaused) {
			_keyIsDown = true;

			//Delete all spawners
			TracerManualInjectionBuilder.DeleteSpawners();

			//Delete all particules
			foreach (var parent in Holders) {
				for (int i = 0; i < parent.transform.childCount; i++) {
					Destroy(parent.transform.GetChild(i).gameObject);
				}
			}

			//Re-build static spawn grid
			TracerInjectionGridBuilder.AskRebuild();

			//Reset Vfx
			GridSpawnerVfxBatch.Reinit();
		} else if (_keyIsDown && (Input.GetKeyUp(KeyCode.Delete) || Input.GetKeyUp(KeyCode.Backspace))) {
			_keyIsDown = false;
		}
	}
}
