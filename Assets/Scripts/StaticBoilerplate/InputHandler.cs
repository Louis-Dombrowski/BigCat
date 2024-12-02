using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton responsible for defining the types of inputs scripts can access.
/// </summary>
public class InputHandler : MonoBehaviour {

	private enum State
	{
		Playing,
		PauseMenuOpen
	}
	
	private static InputHandler instance = null;

	public static void OpenPauseMenu()
	{
		if(instance.state != State.Playing) Debug.LogWarning($"Tried to open pause menu while InputHandler state was {instance.state}");
		instance.state = State.PauseMenuOpen;
	}

	public static void ClosePauseMenu()
	{
		if(instance.state != State.PauseMenuOpen) Debug.LogWarning($"Tried to close pause menu while InputHandler state was {instance.state}");
		instance.state = State.Playing;
	}
	
	public static CameraInputs PollCameraInput()
		=> new(instance);

	public static CardHandInputs PollCardHandInput()
		=> new(instance);

	public static CardInputs PollCardInput()
		=> new(instance);
	
	private PlayerInput playerInput;
	private InputAction move;
	private InputAction look;
	private InputAction modifyCard;
	private InputAction pivot;
	private InputAction pointerDelta;
	private InputAction pointerPosition;
	private InputAction interact;

	private State state = State.Playing; // Framework for moving away from timescale = 0 later.
	
	void Awake() {
		playerInput = GetComponent<PlayerInput>();
		move = playerInput.actions["Move"];
		look = playerInput.actions["Look"];
		modifyCard = playerInput.actions["modifyCard"];
		pivot = playerInput.actions["Pivot"];
		pointerDelta = playerInput.actions["PointerDelta"];
		pointerPosition = playerInput.actions["PointerPosition"];
		interact = playerInput.actions["Interact"];
		
		if(instance != null) Destroy(instance);
		instance = this;
	}

	public readonly struct CameraInputs {
		public CameraInputs(InputHandler instance) {

			if (instance.state == State.PauseMenuOpen)
			{
				MoveDirection = Vector3.zero;
				Look = false;
				Pivot = false;
				MouseDelta = Vector2.zero;
			}
			else
			{
				MoveDirection = instance.move.ReadValue<Vector3>().normalized;
				Look = instance.look.ReadValue<float>() > 0;
				Pivot = instance.pivot.ReadValue<float>() > 0;
				MouseDelta = instance.pointerDelta.ReadValue<Vector2>();
			}
		}
        
		public readonly Vector3 MoveDirection; // normalized
		public readonly bool Look;
		public readonly bool Pivot;
		public readonly Vector2 MouseDelta;
	}

	public readonly struct CardHandInputs
	{
		public CardHandInputs(InputHandler instance) {
			MousePos = instance.pointerPosition.ReadValue<Vector2>();
			Interact = instance.interact.ReadValue<float>() > 0;
		}
		
		public readonly Vector2 MousePos;
		public readonly bool Interact;
	}

	public readonly struct CardInputs
	{
		public CardInputs(InputHandler instance) {
			ModifyCard = Mathf.CeilToInt(instance.modifyCard.ReadValue<float>());
			
			if(ModifyCard != 0) print(ModifyCard);
			
			Interact = instance.interact.ReadValue<float>() > 0;
		}
		
		public readonly bool Interact;
		public readonly int ModifyCard;
	}
}
