using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Util;
using Color = UnityEngine.Color;

	[CustomPropertyDrawer(typeof(QuaternionApproximator))]
	public class QuaternionApproximatorEditor : PropertyDrawer {

		private GraphElement graph;
		private SerializedProperty approximatorProp;
		private SerializedProperty approachSpeedProp;
		private float demoLength = 1f;
	
		// https://discussions.unity.com/t/how-do-i-draw-lines-in-a-custom-inspector/188422/2
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			approximatorProp = property;
			approachSpeedProp = approximatorProp.FindPropertyRelative("approachSpeed");

			if (approachSpeedProp.floatValue == 0) {
				approachSpeedProp.floatValue = 50f;
				approximatorProp.serializedObject.ApplyModifiedProperties();
			}
			
			var root = new VisualElement();

			var header = new Label(ObjectNames.NicifyVariableName(approximatorProp.name));
			root.Add(header);
		
			var infoBox = new Box();
			root.Add(infoBox);
		
			//// Fix indents
			//// TODO: There HAS to be a better way to do this
			//header.style.paddingLeft = infoBox.style.paddingLeft; // Set to the default indent
			//infoBox.style.marginLeft = EditorStyles.inspectorDefaultMargins.padding.left;
		
			var approachSpeedField = new FloatField("Approach Speed");
			approachSpeedField.value = approachSpeedProp.floatValue;
			approachSpeedField.RegisterValueChangedCallback((evt) => {
				approachSpeedProp.floatValue = evt.newValue;
				approximatorProp.serializedObject.ApplyModifiedProperties();
		
				UpdateGraph();
			});
			var demoLengthField = new FloatField("Demo Length");
			demoLengthField.value = demoLength;
			demoLengthField.RegisterValueChangedCallback((evt) => {
				demoLength = evt.newValue;
				UpdateGraph();
			});
		
			infoBox.Add(approachSpeedField);
			infoBox.Add(demoLengthField);

			graph = new GraphElement();
			infoBox.Add(graph);
			UpdateGraph();
		
			return root;
		}

		private void UpdateGraph() {
			const int CurveSamplePoints = 256;
			float timestep = demoLength / CurveSamplePoints;
			float approachSpeed = approachSpeedProp.floatValue;

			Vector2[] curve = new Vector2[CurveSamplePoints];
		
			// Draw the exponential decay
			float state = 1f;
			for (int i = 0; i < CurveSamplePoints; i++) {
				float t = (float)i * timestep;
				curve[i] = new Vector2(t, 1f - state); // GUI coords are upside-down

				float blend = Mathf.Pow(2, -timestep * approachSpeed); 
				state = Mathf.Lerp(0f, state, blend);
			}
			
			graph.lines = new() { (curve, Color.white) };
			graph.Dirty();
		}
	}
