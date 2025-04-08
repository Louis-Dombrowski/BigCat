using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sfx : MonoBehaviour
{
	public enum ClipId
	{
		Null,
		CatFootstep,
		BuildingCrumble,
		WoodCrack,
		LevelComplete,
		Pop
	}
	
	[Serializable]
	private class ClipData
	{
		public ClipId id;
		public AudioClip[] clips;
		public float volume = 1;
	};
	
	private static Sfx instance;

	public static void PlaySound(ClipId clipId, Vector3? position = null, float volumeMultiplier = 1f)
	{
		foreach (var clip in instance.data)
		{
			if (clip.id == clipId)
			{
				var sound = clip.clips[Random.Range(0, clip.clips.Length)];
				float soundVolume = instance.volume * clip.volume * volumeMultiplier;
				
				if(position == null) instance.cameraSource.PlayOneShot(sound, soundVolume);
				else
				{
					var source = Instantiate(instance.soundEffectPlayer, position.Value, Quaternion.identity, instance.transform).GetComponent<AudioSource>();
					source.PlayOneShot(sound, soundVolume);
					Destroy(source.gameObject, sound.length + 0.5f);
				}

				return;
			}
		}
		
		Debug.LogWarning($"Scene Sfx prefab is missing clip {clipId}");
	}

	[Header("Parts")]
	[SerializeField] private GameObject soundEffectPlayer;
	
	[Header("Properties")]
	[SerializeField] private float volume;
	[SerializeField] private List<ClipData> data;

	[Header("State")]
	[SerializeField] private AudioSource cameraSource;
	
	private void Start()
	{
		if(instance != null) Destroy(instance);
		instance = this;

		cameraSource = FindAnyObjectByType<Camera>().GetComponent<AudioSource>();
	}
	
}
