using System;
using UnityEngine;

public class SolidBoundariesBuilder : Builder {
    public TextAsset CsvFile; // Reference of CSV file
    public GameObject ObjectToPopulate;
	
    private Vector3[] _coordinates;

	private string _csvText;
    private void Start() {
	    _csvText = CsvFile.text;
    }

    protected override void Build () {
        _coordinates = DataBase.CsvToVector3List(_csvText)[0].ToArray();

        foreach (Vector3 position in _coordinates) {
			BasicDispatcher.RunOnMainThread(() => 
				Instantiate(ObjectToPopulate, position, Quaternion.identity, transform)
			);
		}
    }

    protected override void SetVisibility(bool isVisible) {
	    BasicDispatcher.RunOnMainThread(() =>
			gameObject.SetActive(!gameObject.activeInHierarchy)        //SetActive must be called in the Update() and NOT in OnGUI()
		);
	}
}
