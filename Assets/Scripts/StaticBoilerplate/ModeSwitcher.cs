using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

public class ModeSwitcher : MonoBehaviour
{
	public static ModeSwitcher instance;
	
	[Header("Properties")]
	[SerializeField] private string levelName = "You forgot to set the level name";
	[Tooltip("Objects that should only be active in Edit Mode")]
	[SerializeField] private List<GameObject> editMode;
	[Tooltip("Objects that should only be active in Play Mode")]
	[SerializeField] private List<GameObject> playMode;
	
	private void Start()
	{
		if(instance != null) Destroy(instance);
		instance = this;

		EnterEditMode();
	}
	
	public async void EnterEditMode()
	{
		string reloadableName = levelName + "_Reloadable";
		
		// Check if the Reloadable portion of this scene is loaded. If it is, reload it
		bool foundReloadable = false;
		for (int i = 0; i < SceneManager.loadedSceneCount; i++)
		{
			foundReloadable |= SceneManager.GetSceneAt(i).name == reloadableName;
		}
		if (foundReloadable) await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(reloadableName));
		SceneManager.LoadScene(reloadableName, LoadSceneMode.Additive);
		
		foreach (var g in instance.editMode) g.SetActive(true);
		foreach (var g in instance.playMode) g.SetActive(false);
}

	// When entering play mode from edit mode, the reloadable scene will always be loaded.
	public void EnterPlayMode()
	{
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
	}

	public void QuitGame()
	{
		Application.Quit();
	}
}
