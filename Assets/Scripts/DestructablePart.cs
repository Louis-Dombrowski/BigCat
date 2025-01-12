using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructablePart : MonoBehaviour
{
	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.CompareTag("Cat"))
		{
			var cat = other.gameObject.GetComponentInParent<CatController>();
			GetComponentInParent<Destructable>().Explode(cat.ExplosionDirection(), other.GetContact(0).point);
		}
	}
}
