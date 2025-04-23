using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = System.Random;

public class ModeSwitcher : MonoBehaviour
{
	public enum Mode
	{
		Null,
		Edit,
		Play
	}
	
	public static ModeSwitcher instance;
	
	[Header("Properties")]
	[SerializeField] private string levelName = "You forgot to set the level name";
	[Tooltip("Objects that should only be active in Edit Mode")]
	[SerializeField] private List<GameObject> editMode;
	[Tooltip("Objects that should only be active in Play Mode")]
	[SerializeField] private List<GameObject> playMode;

	[Header("State")]
	[SerializeField] private Mode mode = Mode.Null;
	
	private void Start()
	{
		if(instance != null) Destroy(instance);
		instance = this;
		
		EnterEditMode();
	}
	
	public async void EnterEditMode()
	{
		if (mode != Mode.Play && mode != Mode.Null)
		{
			if(mode == Mode.Edit) Debug.LogWarning("Tried to enter edit mode while already editing");
			return;
		}
		
		string reloadableName = levelName + "_Reloadable";
		
		// Check if the Reloadable portion of this scene is loaded. If it is, reload it
		bool foundReloadable = false;
		for (int i = 0; i < SceneManager.loadedSceneCount; i++)
		{
			foundReloadable |= SceneManager.GetSceneAt(i).name == reloadableName;
		}

		UnityAction closure = () =>
		{
			SceneManager.LoadScene(reloadableName, LoadSceneMode.Additive);

			foreach (var g in instance.editMode) g.SetActive(true);
			foreach (var g in instance.playMode) g.SetActive(false);

			mode = Mode.Edit;
		};

		if (foundReloadable)
		{
			await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync("FadeTransition", LoadSceneMode.Additive));

			var transition = FindAnyObjectByType<SceneTransition>();
			transition.whileHiding.AddListener(async () =>
			{
				await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(reloadableName));
			});
			transition.whileHiding.AddListener(closure);
		}
		else closure();
	}

	// When entering play mode from edit mode, the reloadable scene will always be loaded.
	public void EnterPlayMode()
	{
		if (mode != Mode.Edit)
		{
			if(mode == Mode.Play) Debug.LogWarning("Tried to enter play mode while already playing");
			if(mode == Mode.Null) Debug.LogWarning("Tried to enter play mode before edit mode");
			return;
		}
		
		GuiData.instance.score = 0;
		
		foreach (var g in instance.editMode) g.SetActive(false);
		foreach (var g in instance.playMode) g.SetActive(true);

		// Copy over all the configured card GameObjects from edit mode
		Transform src = GameObject.Find("PlayModeAssets_ToCopy").transform;
		Transform dst = GameObject.Find("PlayModeAssets").transform; // Will crash here if this is run before EnterEditMode or if the _Reloadable scene isnt open
		for (int i = 0; i < src.childCount; i++)
		{
			Instantiate(src.GetChild(i).gameObject, dst, true).SetActive(true);
		}

		mode = Mode.Play;
	}

	public static bool IsPlaying()
	{
		return instance.mode == Mode.Play;
	}

	public static bool IsEditing()
	{
		return instance.mode == Mode.Edit;
	}
	
	public void QuitGame()
	{
		Application.Quit();
	}
}
