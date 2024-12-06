using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	public static class ArrowGizmo
	{
		public const float HeadRadius = 0.1f;
		public const float HeadLength = 0.2f;

		public static void Draw(Vector3 origin, Vector3 arrow, Color color)
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			// Arrow
			Vector3 tip = origin + arrow;
			Gizmos.DrawLine(origin, tip);

			// Tip
			Vector3 tipBack = tip - arrow.normalized * HeadLength;
			Quaternion lookRot = arrow == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(arrow);
			Vector3 perpendicular1 = lookRot * Vector3.right * HeadRadius;
			Vector3 perpendicular2 = lookRot * Vector3.up * HeadRadius;

			Gizmos.DrawLine(tipBack, tipBack + perpendicular1);
			Gizmos.DrawLine(tip, tipBack + perpendicular1);

			Gizmos.DrawLine(tipBack, tipBack - perpendicular1);
			Gizmos.DrawLine(tip, tipBack - perpendicular1);

			Gizmos.DrawLine(tipBack, tipBack + perpendicular2);
			Gizmos.DrawLine(tip, tipBack + perpendicular2);

			Gizmos.DrawLine(tipBack, tipBack - perpendicular2);
			Gizmos.DrawLine(tip, tipBack - perpendicular2);

			Gizmos.color = oldColor;
		}

		public static void Draw(Vector2 origin, Vector2 arrow, float height, Color color)
		{
			Draw(new(origin.x, height, origin.y), new(arrow.x, 0, arrow.y), color);
		}
	}
}
