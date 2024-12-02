using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Stopwatch))]
public class StopwatchEditor : PropertyDrawer
{
	private SerializedProperty stopwatchProp;
	private SerializedProperty lengthProp;
	private SerializedProperty stateProp;
	
	public override VisualElement CreatePropertyGUI(SerializedProperty property)
	{
		stopwatchProp = property;
		lengthProp = property.FindPropertyRelative("length");
		stateProp = property.FindPropertyRelative("state");

		var root = new VisualElement();

		var header = new Label(ObjectNames.NicifyVariableName(property.name));
		root.Add(header);
		
		var infoBox = new Box();
		root.Add(infoBox);
		
		//// Fix indents
		//// TODO: There HAS to be a better way to do this
		//header.style.paddingLeft = infoBox.style.paddingLeft; // Set to the default indent
		//infoBox.style.marginLeft = EditorStyles.inspectorDefaultMargins.padding.left;
		
		var lengthField = new FloatField("Length");
		lengthField.BindProperty(lengthProp);
			
		var progressBar = new ProgressBar();
		progressBar.title = "Progress";
		progressBar.lowValue = 0;
		progressBar.highValue = 1;
		progressBar.BindProperty(stopwatchProp.FindPropertyRelative("progress"));
		
		var stateField = new EnumField("State");
		stateField.BindProperty(stateProp);
		
		infoBox.Add(lengthField);
		infoBox.Add(progressBar);
		infoBox.Add(stateField);
		
		return root;
	}
}
