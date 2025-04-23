using System;
using UnityEngine;
using UnityEngine.Serialization;

public class LegIK : MonoBehaviour
{
	[System.Serializable]
	private struct Bone
	{
		public Bone(Transform transform)
		{
			end = transform;
			start = end.parent;
			length = (end.position - end.parent.position).magnitude;
		}

		public Transform start;
		public Transform end;
		public float length;
	}
	
	[Header("Properties")]
	[SerializeField] private Transform pawTarget;
	[SerializeField] private float thighOffset = -0.75f;
	[Header("State")]
	[SerializeField] public Quaternion catDirection;
	[SerializeField] private Bone thigh;
	[SerializeField] private Bone calf;
	[SerializeField] private Bone foot; // Digitigrade, not plantigrade, so this is the lower segment of the leg
	[SerializeField] private Bone toes;
	[SerializeField] private Vector3 localToesOffset;
	
	private void Start()
	{
		thigh = new Bone(transform);
		calf = new Bone(thigh.end.GetChild(0));
		foot = new Bone(calf.end.GetChild(0));

		if (foot.end.childCount > 0)
		{
			toes = new Bone(foot.end.GetChild(0));
			localToesOffset = toes.end.localPosition;
		}
	}

	private void LateUpdate()
	{
		
		// Point the thigh straight towards the paw target
		Vector3 thighVec = (pawTarget.position + catDirection * Vector3.back * thighOffset - thigh.start.position).normalized * thigh.length;
		thigh.end.position = thigh.start.position + thighVec;

		Vector3 toPaw = (pawTarget.position - foot.end.transform.TransformVector(localToesOffset) - calf.start.position); // from the start of the calf
		
		Vector3 ikPlaneBasis = toPaw;
		ikPlaneBasis.y = 0;
		ikPlaneBasis.Normalize();
		if (ikPlaneBasis == Vector3.zero) ikPlaneBasis = Vector3.forward;
		if (Vector3.Dot(ikPlaneBasis, catDirection * Vector3.forward) < 0) ikPlaneBasis *= -1; // Make sure the paws always point forward
		
		// In a vertical 2D plane relative with the hip at the origin
		Vector2 T = new(Vector3.Dot(toPaw, ikPlaneBasis), toPaw.y);
		float L = calf.length, l = foot.length, d = T.magnitude;
		
		// Handle cases where the limb is too long/short
		const float Epsilon = 0.01f;
		if (d > L + l)
		{
			float missingLength = d - (L + l);
			L += missingLength / 2 + Epsilon;
			l += missingLength / 2 + Epsilon;
		}
		else if (d < Mathf.Abs(L - l))
		{
			if (L > l)
			{
				float diff = L - l - d;
				L -= diff + Epsilon;
			}
			else if (l > L)
			{
				float diff = l - L - d;
				l -= diff + Epsilon;
			}
		}

		float A = -Mathf.Acos((L * L + d * d - l * l) / (2 * L * d));
		float B = -Mathf.Acos((L * L + l * l - d * d) / (2 * L * l)) + Mathf.PI;

		float cosA = Mathf.Cos(A), sinA = Mathf.Sin(A), cosB = Mathf.Cos(B), sinB = Mathf.Sin(B);
		Vector2 planeCalf = (L / d) * new Vector2(T.x * cosA - T.y * sinA, T.x * sinA + T.y * cosA);
		Vector2 planeFoot  = (l / L) * new Vector2(planeCalf.x * cosB - planeCalf.y * sinB, planeCalf.x * sinB + planeCalf.y * cosB);

		Vector3 calfV = planeCalf.x * ikPlaneBasis;
		calfV.y = planeCalf.y;
		Vector3 footV = planeFoot.x * ikPlaneBasis;
		footV.y = planeFoot.y;
		
		//calf.start.rotation = Quaternion.LookRotation(calfV);
		calf.end.position = calf.start.position + calfV;
		
		//foot.start.rotation = Quaternion.LookRotation(footV);
		foot.end.position = foot.start.position + footV;

		//paw.rotation = Quaternion.LookRotation(hip.forward);
	}
}
