using System;
using UnityEngine;

public class CardPickup : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private GameObject poofVfx;
	[Header("Properties")]
	[SerializeField] private GameObject prefab;
	
	public void PickUpCard()
	{
		Sfx.PlaySound(Sfx.ClipId.LevelComplete);
		var cardHand = FindObjectsByType<CardHand>(FindObjectsSortMode.None)[0];
		cardHand.DrawCard(prefab);
		Instantiate(poofVfx, transform.position + Vector3.up * 5, transform.rotation);
		gameObject.SetActive(false);
	}
}
