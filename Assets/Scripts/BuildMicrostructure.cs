using System;
using System.Collections;
using UnityEngine;

public class BuildMicrostructure : MonoBehaviour {
    public TextAsset CsvFile; // Reference of CSV file
    public GameObject ObjectToPopulate;

    [HideInInspector]
    public Vector3 Size = Vector3.zero;

    private Vector3[] _coordinates;

    //private DateTime tempTimer;

    // Use this for initialization
    private void Start() {
        //tempTimer = DateTime.Now;
        
        _coordinates = DataBase.CsvToVector3List(CsvFile.text)[0].ToArray();
        //StartCoroutine("GoPopulate_FixedByFrame");
        GoPopulate_AllAtOnce();
    }

    private void GoPopulate_AllAtOnce() { 
        foreach (Vector3 position in _coordinates) {
            Instantiate(ObjectToPopulate, position, Quaternion.identity, transform);

            Size.x = Math.Max(Size.x, position.x);
            Size.y = Math.Max(Size.y, position.y);
            Size.z = Math.Max(Size.z, position.z);
		}
        
        //Debug.Log("duration " + (DateTime.Now - tempTimer).TotalSeconds);
    }

    private IEnumerator GoPopulate_OneByFrame() {
        foreach (Vector3 vec in _coordinates) {              
            Instantiate(ObjectToPopulate, vec, Quaternion.identity, this.transform);
            yield return null; //OR new WaitForEndOfFrame()
        }
        
        //Debug.Log("duration " + (DateTime.Now - tempTimer).TotalSeconds);
    }

    private IEnumerator GoPopulate_FixedByFrame() {
        int i = 0;

        foreach (Vector3 vec in _coordinates) {
            if (i > 10) {
                Debug.Log("yield");
                yield return null; //OR new WaitForEndOfFrame()
                i = 0;
            }

            Instantiate(ObjectToPopulate, vec, Quaternion.identity, this.transform);
            i++;
        }
        
        //Debug.Log("duration " + (DateTime.Now - tempTimer).TotalSeconds);
    }

    private IEnumerator GoPopulate_MaxByFrame() {
        //Debug.Log("fixedDeltaTime " + Time.fixedDeltaTime);
        DateTime startFrameTime = DateTime.Now;

        foreach (Vector3 vec in _coordinates) {
            if ((DateTime.Now - startFrameTime).TotalSeconds >= 0.8 * Time.fixedDeltaTime) {
                Debug.Log((DateTime.Now - startFrameTime).TotalSeconds);
                yield return null; //OR new WaitForEndOfFrame()
                startFrameTime = DateTime.Now;
            }
            
            Instantiate(ObjectToPopulate, vec, Quaternion.identity, this.transform);       
        }
        
        //Debug.Log("duration " + (DateTime.Now - tempTimer).TotalSeconds);
    }
}
