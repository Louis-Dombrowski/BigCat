using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfVision : MonoBehaviour
{
	[SerializeField] private CatController cat;

	private void Start()
	{
		cat = GetComponentInParent<CatController>();
	}

	private void OnTriggerEnter(Collider other)
	{
		var distraction = other.GetComponent<Distraction>();
		if (distraction == null) return;

		cat.visibleDistractions.Add(distraction);
	}	

	private void OnTriggerExit(Collider other)
	{
		var distraction = other.GetComponent<Distraction>();
		if (distraction == null) return;

		cat.visibleDistractions.Remove(distraction);
	}
}
