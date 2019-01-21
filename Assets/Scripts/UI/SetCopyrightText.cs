using UnityEngine;
using UnityEngine.UI;

public class SetCopyrightText : MonoBehaviour {
	private void Start () {
		GetComponent<Text>().text = Global.Credits;
	}
}
