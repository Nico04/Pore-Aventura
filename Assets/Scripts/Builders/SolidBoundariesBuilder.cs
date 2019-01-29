using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SolidBoundariesBuilder : Builder {
    public TextAsset CsvFile; // Reference of CSV file
    public GameObject ObjectToPopulate;
	
    private Vector3[] _coordinates;

	private string _csvText;
    private void Start() {
	    _csvText = CsvFile.text;
    }

    protected override async Task Build (CancellationToken cancellationToken) {
		_coordinates = await Task.Run(() => DataBase.CsvToVector3List(_csvText)[0].ToArray(), cancellationToken).ConfigureAwait(true);

		foreach (Vector3 position in _coordinates) {
	        Instantiate(ObjectToPopulate, position, Quaternion.identity, transform);
        }
	}

    protected override void SetVisibility(bool isVisible) {
	    BasicDispatcher.RunOnMainThread(() =>
			gameObject.SetActive(!gameObject.activeInHierarchy)        //SetActive must be called in the Update() and NOT in OnGUI()
		);
	}
}
