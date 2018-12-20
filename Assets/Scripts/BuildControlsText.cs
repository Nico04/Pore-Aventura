#if UNITY_EDITOR

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// Get mapped key names to show them on screen.
/// /!\ Works only in DEBUG mode as UnityEditor package is not available in RELEASE

/** Old UI text with tags :

Controls 
- Look around : mouse
- Move forward/backward : <Vertical>
- Sidewalk : <Horizontal>
- Elevation : <Elevation>
- Spawn : <Shoot>
- Rollercoaster mode : <ToggleRollercoasterMode>

*/

public class BuildControlsText : MonoBehaviour {
	// Use this for initialization
	private void Start () {
		BuildInputControlText();
	}

	//Build Input Control Text
	private void BuildInputControlText() {
		//Get Unity's inputManager object
		SerializedObject inputManagerObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);

		//To string
		string inputManagerString = SerializedObjectToString(inputManagerObject);

		//Get UI Text
		var textComponent = GetComponent<Text>();
		string text = textComponent.text;

		//Replace all the <...> tags by the value
		Match match;
		while ((match = Regex.Match(text, @"<(.+)>")).Success) {
			string mappedKey = GetMappedKeyName(ref inputManagerString, match.Groups[1].Value);
			text = text.Replace(match.Value, mappedKey);
		}

		//Apply value
		textComponent.text = text;
	}

	//
	private string GetMappedKeyName(ref string inputManager, string inputName) {
		//Search for the inputName
		int inputNameIndex = inputManager.IndexOf(inputName);

		//Search for the main assigned key
		string negativeButton = GetNextValue(ref inputManager, inputManager.IndexOf("negativeButton", inputNameIndex));

		//Search for the main assigned key
		string positiveButton = GetNextValue(ref inputManager, inputManager.IndexOf("positiveButton", inputNameIndex));

		//Search for the main assigned key
		string altNegativeButton = GetNextValue(ref inputManager, inputManager.IndexOf("altNegativeButton", inputNameIndex));

		//Search for the main assigned key
		string altPositiveButton = GetNextValue(ref inputManager, inputManager.IndexOf("altPositiveButton", inputNameIndex));

		//Build output
		string mainButtons = (!string.IsNullOrWhiteSpace(negativeButton) ? negativeButton + " and " : "") + positiveButton;
		string altButtons = (!string.IsNullOrWhiteSpace(altNegativeButton) ? altNegativeButton + " and " : "") + altPositiveButton;
		return mainButtons + (!string.IsNullOrWhiteSpace(altButtons) ? " or " + altButtons : "");
	}

	private string GetNextValue(ref string inputText, int startIndex = 0) {
		int valueKeyIndex = inputText.IndexOf("Value:", startIndex);
		int valueStartIndex = inputText.IndexOf(" ", valueKeyIndex) + 1;
		int valueEndIndex = inputText.IndexOf(System.Environment.NewLine, valueStartIndex);
		return inputText.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
	}

	/**
     * Returns a formatted string with all properties in serialized object.
     */
	private static string SerializedObjectToString(SerializedObject serializedObject) {
		System.Text.StringBuilder sb = new System.Text.StringBuilder();

		if (serializedObject == null) {
			sb.Append("NULL");
			return sb.ToString();
		}

		SerializedProperty iterator = serializedObject.GetIterator();

		iterator.Next(true);

		while (iterator.Next(true)) {
			string tabs = "";
			for (int i = 0; i < iterator.depth; i++) tabs += "\t";

			sb.AppendLine(tabs + iterator.name + (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.type.Contains("Component") && iterator.objectReferenceValue == null ? " -> NULL" : ""));

			tabs += "  - ";

			sb.AppendLine(tabs + "Type: (" + iterator.type + " / " + iterator.propertyType + " / " + " / " + iterator.name + ")");
			sb.AppendLine(tabs + iterator.propertyPath);
			sb.AppendLine(tabs + "Value: " + SerializedPropertyValue(iterator));
		}

		return sb.ToString();
	}

	/**
     * Return a string from the value of a SerializedProperty.
     */
	private static string SerializedPropertyValue(SerializedProperty sp) {
		switch (sp.propertyType) {
			case SerializedPropertyType.Integer:
				return sp.intValue.ToString();

			case SerializedPropertyType.Boolean:
				return sp.boolValue.ToString();

			case SerializedPropertyType.Float:
				return sp.floatValue.ToString();

			case SerializedPropertyType.String:
				return sp.stringValue.ToString();

			case SerializedPropertyType.Color:
				return sp.colorValue.ToString();

			case SerializedPropertyType.ObjectReference:
				return (sp.objectReferenceValue == null ? "null" : sp.objectReferenceValue.name);

			case SerializedPropertyType.LayerMask:
				return sp.intValue.ToString();

			case SerializedPropertyType.Enum:
				return sp.enumValueIndex.ToString();

			case SerializedPropertyType.Vector2:
				return sp.vector2Value.ToString();

			case SerializedPropertyType.Vector3:
				return sp.vector3Value.ToString();

			// Not public api as of 4.3?
			// case SerializedPropertyType.Vector4:
			//  return sp.vector4Value.ToString();

			case SerializedPropertyType.Rect:
				return sp.rectValue.ToString();

			case SerializedPropertyType.ArraySize:
				return sp.intValue.ToString();

			case SerializedPropertyType.Character:
				return "Character";

			case SerializedPropertyType.AnimationCurve:
				return sp.animationCurveValue.ToString();

			case SerializedPropertyType.Bounds:
				return sp.boundsValue.ToString();

			case SerializedPropertyType.Gradient:
				return "Gradient";

			default:
				return "Unknown type";
		}
	}
}

#endif	