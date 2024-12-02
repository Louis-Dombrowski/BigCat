using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
	private static PauseMenu instance;

	public static bool IsPaused()
	{
		return instance.paused;
	}
	
	[Header("Properties")]
	[SerializeField] private GameObject pauseMenuObj;
	
	[Header("State")]
	[SerializeField] private bool paused = false;
	
	private void Start()
	{
		if(instance != null) Destroy(instance);
		instance = this;
		
		pauseMenuObj.SetActive(false);
		paused = false;
	}
	public void OnPause(InputValue value)
	{
		paused = !paused;
		pauseMenuObj.SetActive(paused);
		
		if (paused)
		{
			Time.timeScale = 0;
			InputHandler.OpenPauseMenu();
		}
		else
		{
			InputHandler.ClosePauseMenu();
			Time.timeScale = 1;
		}
	}
}
