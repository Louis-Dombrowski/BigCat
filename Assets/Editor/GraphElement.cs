using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

	public class GraphElement : VisualElement {
		public new class UxmlFactory : UxmlFactory<GraphElement, UxmlTraits> { }
		public new class UxmlTraits : BindableElement.UxmlTraits {
			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
				base.Init(ve, bag, cc);
			}
		}

		private VisualElement graph;
		public GraphElement() {
			style.alignItems = Align.Stretch;

			graph = new VisualElement();
			graph.style.minWidth = 200;
			graph.style.maxWidth = 1000;
			graph.style.minHeight = 200;
			graph.style.maxHeight = 800;
			graph.generateVisualContent += DrawCanvas; // adds DrawCanvas as a callback
			
			Add(graph);
		}

		// [(0, 0), (1, 1)]
		public List<(Vector2[] points, Color color)> lines;
		
		void DrawCanvas(MeshGenerationContext ctx) {
			if (lines == null)
				return;
			
			var painter = ctx.painter2D;
			foreach (var l in lines) {
				painter.strokeColor = l.color;
				painter.lineWidth = 2f;
				
				painter.BeginPath();
				painter.MoveTo(l.points[0]);
				painter.ClosePath();
				
				painter.BeginPath();
				for (int i = 1; i < l.points.Length; i++) painter.LineTo(new (l.points[i].x * graph.layout.width, l.points[i].y * graph.layout.height));
				painter.Stroke();
				painter.ClosePath();
			}
		}

		public void Dirty() {
			graph.MarkDirtyRepaint();
		}
	}
