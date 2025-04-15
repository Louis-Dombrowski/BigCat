using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
	[Header("Parts")]
	[SerializeField] private Animation animations;
	[Header("State")]
	[SerializeField] private bool buttonsFrozen = false;

	private void Start()
	{
		animations = GetComponent<Animation>();
	}

	public void QuitGame()
	{
		if (buttonsFrozen) return;

		Application.Quit();
		print("Quit game");
	}

	public void OpenOptions()
	{
		if (buttonsFrozen) return;

		animations.Play("OpenOptions");
	}
	public void CloseOptions()
	{
		if (buttonsFrozen) return;

		animations.Play("CloseOptions");
	}

	public async void PlayLevel()
	{
		if (buttonsFrozen) return;
		
		await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync("TsunamiTransition", LoadSceneMode.Additive)); // async so the scene is actually loaded when setting the callback
		FindAnyObjectByType<SceneTransition>().whileHiding.AddListener(async () =>
		{
			await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(gameObject.scene));
			SceneManager.LoadScene("DemoLevel", LoadSceneMode.Additive);
		});
	}

	public void FreezeButtons()
	{
		buttonsFrozen = true;
	}

	public void UnfreezeButtons()
	{
		buttonsFrozen = false;
	}
}
