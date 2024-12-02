using System;
using UnityEngine;

public abstract class BaseCard : MonoBehaviour
{
	[SerializeField] private Material editorMat;

	public virtual void ChangeProperty(int delta)
	{
		
	}

	public void DisableDistractions()
	{
		var distractions = GetComponentsInChildren<Distraction>();
		foreach (var d in distractions)
		{
			Destroy(d);
		}
	}
	// Disable any behavior of this object that would cause problems in edit mode
	public abstract void DisablePlayFunctionality();
	
	// Enable the previews and things for edit mode. Implementing this is optional.
	public virtual void EnableEditFunctionality()
	{
		
	}
	
	public void SetEditModeShaders()
	{
		var renderers = GetComponentsInChildren<Renderer>();
		foreach (var r in renderers)
		{
			if (r.CompareTag("Debug")) continue; // Skip over things like DebugMarkers
			
			var newMats = new Material[r.materials.Length];
			Array.Fill(newMats, editorMat);
			r.materials = newMats;
		}
	}
}
