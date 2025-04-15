using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
	public UnityEvent whileHiding = new(); // Things to do once the screen is occluded

	public void OnHidden()
	{
		whileHiding.Invoke();
		GetComponent<Animation>().Play("TransitionFade");
	}
	
	public void UnloadTransition()
	{
		SceneManager.UnloadSceneAsync(gameObject.scene);
	}
}
