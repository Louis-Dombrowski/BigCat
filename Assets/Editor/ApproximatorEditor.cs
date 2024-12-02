using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Util;
using Color = UnityEngine.Color;

	[CustomPropertyDrawer(typeof(Approximator))]
	public class ApproximatorEditor : PropertyDrawer {

		private float responseFrequency;
		private float dampingCoefficient;
		private float responseBehavior;
		private GraphElement graph;
		private SerializedProperty approximatorProp;
		private SerializedProperty k1Prop;
		private SerializedProperty k2Prop;
		private SerializedProperty k3Prop;
	
		// https://discussions.unity.com/t/how-do-i-draw-lines-in-a-custom-inspector/188422/2
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			approximatorProp = property;
			k1Prop = approximatorProp.FindPropertyRelative("k1");
			k2Prop = approximatorProp.FindPropertyRelative("k2");
			k3Prop = approximatorProp.FindPropertyRelative("k3");
		
			// Grab these values from the serialized approximator...
			float k1 = k1Prop.floatValue;
			float k2 = k2Prop.floatValue;
			float k3 = k3Prop.floatValue;

			// ...and derive the 3 readable parameters from them to show in the editor
			if (k1 == 0 && k2 == 0 && k3 == 0) { // Special case so we dont get a bunch of NaN's and infinities by default
				responseFrequency = 1;
				dampingCoefficient = 1;
				responseBehavior = 0;
			} else { // otherwise, derive as normal
				float sqrtK2 = Mathf.Sqrt(k2);
				responseFrequency = 1 / (2 * Mathf.PI * sqrtK2);
				dampingCoefficient = k1 / (2 * sqrtK2);
				responseBehavior = 2 * k3 / k1;
			}

			var root = new VisualElement();

			var header = new Label(ObjectNames.NicifyVariableName(approximatorProp.name));
			root.Add(header);
		
			var infoBox = new Box();
			root.Add(infoBox);
		
			// Fix indents
			// TODO: There HAS to be a better way to do this
			//header.style.paddingLeft = infoBox.style.paddingLeft; // Set to the default indent
			//infoBox.style.marginLeft = EditorStyles.inspectorDefaultMargins.padding.left;
		
			var responseFrequencyField = new FloatField("Response Frequency");
			responseFrequencyField.value = responseFrequency;
			responseFrequencyField.RegisterValueChangedCallback((ChangeEvent<float> evt) => {
				responseFrequency = evt.newValue;
				UpdateApproximator();
			});
			var dampingCoefficientField = new FloatField("Damping Coefficient");
			dampingCoefficientField.value = dampingCoefficient;
			dampingCoefficientField.RegisterValueChangedCallback((ChangeEvent<float> evt) => {
				dampingCoefficient = evt.newValue;
				UpdateApproximator();
			});
			var responseBehaviorField = new FloatField("Response Behavior");
			responseBehaviorField.value = responseBehavior;
			responseBehaviorField.RegisterValueChangedCallback((ChangeEvent<float> evt) => {
				responseBehavior = evt.newValue;
				UpdateApproximator();
			});
		
			infoBox.Add(responseFrequencyField);
			infoBox.Add(dampingCoefficientField);
			infoBox.Add(responseBehaviorField);

			graph = new GraphElement();
			infoBox.Add(graph);
			UpdateApproximator();
		
			return root;
		}

		private void UpdateApproximator() {
			k1Prop.floatValue = dampingCoefficient / (Mathf.PI * responseFrequency);
			k2Prop.floatValue = 1 / (4 * Mathf.PI * Mathf.PI * responseFrequency * responseFrequency);
			k3Prop.floatValue = responseBehavior * dampingCoefficient / (2 * Mathf.PI * responseFrequency);

			approximatorProp.serializedObject.ApplyModifiedProperties();
			//Debug.Log($"Set approximator params to k1:{dampingCoefficient / (Mathf.PI * responseFrequency)} k2:{1 / (4 * Mathf.PI * Mathf.PI * responseFrequency * responseFrequency)} k3:{responseBehavior * dampingCoefficient / (2 * Mathf.PI * responseFrequency)}");
		
			UpdateGraph();
		}
		private void UpdateGraph() {
			const int CurveSamplePoints = 256;
			const float DemoLength = 6f;
			const float Timestep = DemoLength / CurveSamplePoints;
			const float StepTime = 1f;
		
			var approximator = (Approximator)approximatorProp.boxedValue; // create a duplicate of the approximator to draw a graph

			Vector2[] stepFunc = new Vector2[CurveSamplePoints];
			Vector2[] curve = new Vector2[CurveSamplePoints];
		
			// Draw the step func
			for (int i = 0; i < CurveSamplePoints; i++) {
				float t = (float)i * Timestep;
				float y = t > StepTime ? 1 : 0;

				stepFunc[i] = new Vector2(t, y);
			}
		
			// Draw the approximator curve
			approximator.Initialize(1, new[] {0f});
			//Debug.Log($"Running approximator with params k1:{approximator.k1} k2:{approximator.k2} k3:{approximator.k3}");
			float[] targets = new float[1];
			float max = 1, min = 0;
			for (int i = 0; i < CurveSamplePoints; i++) {
				float t = (float)i * Timestep;
			
				targets[0] = stepFunc[i].y;
				float y = approximator.Update(Timestep, targets)[0];

				if (y > max) max = y;
				if (y < min) min = y;
			
				curve[i] = new Vector2(t, y);
			}
			max += 0.1f;
			min -= 0.1f;
		
			// Normalize the points on both curves
			float delta = max - min;
			for (int i = 0; i < CurveSamplePoints; i++) {
				stepFunc[i] = new(stepFunc[i].x / DemoLength, (1 - (stepFunc[i].y - min) / delta));
				curve[i] = new(curve[i].x / DemoLength, (1 - (curve[i].y - min) / delta));
			}

			graph.lines = new() { (stepFunc, Color.green), (curve, Color.white) };
			graph.Dirty();
			//Debug.Log("Marked graph as dirty!");
		}
	}
