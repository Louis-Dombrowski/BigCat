using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

public class CatLeg : MonoBehaviour
{
	public delegate void Callback();
	
	[Header("Pieces")]
	[SerializeField] public Transform hip;
	[SerializeField] private Transform thigh;
	[SerializeField] private Transform calf;
	[SerializeField] private Transform paw;
	[SerializeField] private Transform pawHome;
	
	[Header("Properties")]
	[SerializeField] public float pawRaiseHeight = 1.5f;
	[SerializeField] public float maxLength = 2f;
	[SerializeField] private float thighLength = 3;
	[SerializeField] private float calfLength = 5;
	[SerializeField] private float pawForwardOffset = 0.75f;
	
	[Header("State")]
	[SerializeField] public Vector3 target;
	[SerializeField] private Vector3 pawHomeOffset; // Vector from hip to home

	private QuadraticBezier? toDraw;
	
	private void Awake()
	{
		target = transform.position;
		pawHomeOffset = hip.InverseTransformVector(pawHome.position - hip.position);
	}
	private void FixedUpdate()
	{
		// Move pawHome with hip
		UpdatePawHome();

		// Inverse kinematics (https://www.desmos.com/calculator/juhjmhqmln)
		Vector3 planeBasis = paw.position - hip.position;
		planeBasis.y = 0;
		if (planeBasis == Vector3.zero) planeBasis = Vector3.forward;
		planeBasis.Normalize();
		if (Vector3.Dot(planeBasis, hip.forward) < 0) planeBasis *= -1; // Make sure the paws always point forward
		
		// In a vertical 2D plane relative with the hip at the origin
		Vector2 T = new(Vector3.Dot(paw.position - hip.position, planeBasis) - pawForwardOffset, paw.position.y - hip.position.y);
		float L = thighLength, l = calfLength, d = T.magnitude;
		
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
		Vector2 planeThigh = (L / d) * new Vector2(T.x * cosA - T.y * sinA, T.x * sinA + T.y * cosA);
		Vector2 planeCalf  = (l / L) * new Vector2(planeThigh.x * cosB - planeThigh.y * sinB, planeThigh.x * sinB + planeThigh.y * cosB);

		Vector3 thighV = planeThigh.x * planeBasis;
		thighV.y = planeThigh.y;
		Vector3 calfV = planeCalf.x * planeBasis;
		calfV.y = planeCalf.y;
		
		thigh.rotation = Quaternion.LookRotation(thighV);
		thigh.position = hip.position;
		thigh.localScale = new(1, 1, L);

		calf.rotation = Quaternion.LookRotation(calfV);
		calf.position = hip.position + thighV;
		calf.localScale = new(1, 1, l);
		
		paw.rotation = Quaternion.LookRotation(hip.forward);
	}

	void UpdatePawHome()
	{
		pawHome.position = hip.TransformPoint(pawHomeOffset);
	}
	
	private Vector3 FindFoothold(Vector3 delta)
	{
		Vector3 origin = pawHome.position + delta;
		origin.y = hip.position.y;

		bool steppingUp = Physics.Raycast(
			origin,
			Vector3.down,
			out var hit,
			9999, // TODO: Find a sensible value for this
			LayerMask.GetMask("Ground")
		);
		
		if (steppingUp) return hit.point;
		else return hip.position + pawHomeOffset;
	}
	public IEnumerator AnimateStep(Vector3 delta, float startupDelay = 0, float animationLength = 1f, Callback onStepFinish = null)
	{
		target = FindFoothold(delta);
		
		yield return new WaitForSeconds(startupDelay);
		//print($"{gameObject.name} stepped");
		
		var path = new QuadraticBezier(paw.position, paw.position + Vector3.up * (paw.position - target).magnitude / 2, target);
		
		toDraw = path;
		float pathLength = path.Length();
		
		float t = 0;
		while (pathLength > 0 && t < 1)
		{
			t += Time.deltaTime / animationLength;
			float eased = (2 * t) / (t * t + 1);
			paw.position = path.Position(eased);

			yield return null;
		}
		
		if(onStepFinish != null) onStepFinish();
		Sfx.PlaySound(Sfx.ClipId.CatFootstep, paw.position);
	}

	public void TeleportHome()
	{
		UpdatePawHome();
		paw.position = FindFoothold(Vector3.zero);
		target = paw.position;
	}

	public float TargetPawHeight()
	{
		return target.y;
	}
	
	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;
		Gizmos.color = Color.green;
		Gizmos.DrawLine(hip.position, paw.position);
		
		if (toDraw == null) return;
		toDraw.Value.Draw(Color.red);
	}
}
