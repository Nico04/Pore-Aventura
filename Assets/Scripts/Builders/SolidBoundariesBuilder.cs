using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SolidBoundariesBuilder : Builder {
    public TextAsset CsvFile; // Reference of CSV file
    public GameObject ObjectToPopulate;

	private string _csvText;
    private void Start() {
	    _csvText = CsvFile.text;
    }

    protected override async Task Build (CancellationToken cancellationToken) {
		var positions = await Task.Run(() => DataBase.GetSolidBoundaries(), cancellationToken).ConfigureAwait(true);

		foreach (Vector3 position in positions) {
	        Instantiate(ObjectToPopulate, position, Quaternion.identity, transform);
        }
	}

    protected override void SetVisibility(bool isVisible) {
	    BasicDispatcher.RunOnMainThread(() =>
			gameObject.SetActive(!gameObject.activeInHierarchy)        //SetActive must be called in the Update() and NOT in OnGUI()
		);
	}
}
