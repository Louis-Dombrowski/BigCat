using System.Collections.Generic;
using UnityEngine;
using Util;

public class CameraController : MonoBehaviour
{
	private delegate void GizmoDrawCall();
	
	public enum State
	{
		Moving,
		Pivoting
	}
	
	[Header("Parts")]
	[SerializeField] private new Transform camera;
	[SerializeField] private Transform target;
	[Header("Properties")]
	[SerializeField] private float moveSpeed = 10;
	[Tooltip("If you drag the mouse horizontally from one end of the screen to the other, you will turn this many degrees")]
	[SerializeField] private float lookSpeed = 200;
	[SerializeField] private Approximator posApprox;
	[SerializeField] private QuaternionApproximator rotApprox;
	[Header("State")]
	[SerializeField] private State state;
	[SerializeField] private Vector2 tgtRot;
	[SerializeField] public bool cursorIsOccupied = false;
	
	private void Start()
	{
		posApprox.Initialize(target.position);
		tgtRot = new(transform.eulerAngles.x, transform.eulerAngles.y);
	}
	
	private void Update()
	{
		var input = InputHandler.PollCameraInput();

		cursorIsOccupied = input.Look || input.Pivot;
		
		// The target is offset from the camera, like a carrot on a stick
		target.localPosition = input.MoveDirection * moveSpeed;
		camera.position = posApprox.Update(Time.deltaTime, target.position);
		
		if (input.Look)
		{
			Vector2 normalizedDelta = input.MouseDelta / Screen.width;
			tgtRot += new Vector2(-normalizedDelta.y * lookSpeed, normalizedDelta.x * lookSpeed);
			camera.rotation = Quaternion.Euler(tgtRot.x, tgtRot.y, 0);
		}
	}
	
	private List<GizmoDrawCall> gizmoDrawCalls = new();
	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;

		foreach (var c in gizmoDrawCalls) c();
	}
}
