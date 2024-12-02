using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	[System.Serializable]
	public struct QuadraticBezier
	{
		[SerializeField] public Vector3 p1, p2, p3;
		
		public QuadraticBezier(Vector3 begin, Vector3 control, Vector3 end)
		{
			p1 = begin;
			p2 = control;
			p3 = end;
		}

		public Vector3 Position(float t)
		{
			return (1 - t) * ((1 - t) * p1 + t * p2) + t * ((1 - t) * p2 + t * p3);
		}

		public float Length()
		{
			var points = Approximation();
			float length = 0;
			for (int i = 1; i < points.Length; i++)
			{
				length += (points[i] - points[i - 1]).magnitude;
			}

			return length;
		}
		
		private Vector3[] Approximation(int nPoints = 10)
		{
			var points = new Vector3[nPoints];
			for (int i = 0; i < points.Length; i++)
			{
				float t = (float)i / (float)(points.Length - 1);
				points[i] = Position(t);
			}

			return points;
		}
		
		public void Draw(Color c)
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = c;

			var points = Approximation();
			
			Gizmos.DrawLineStrip(points, false);

			Gizmos.color = oldColor;
		}
	}
}
