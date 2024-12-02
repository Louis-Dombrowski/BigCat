using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Util {
	[Serializable]
	public struct QuaternionApproximator {
		[SerializeField] public float approachSpeed;
		
		private Quaternion state;
		
		public void Initialize(Quaternion initialState) {
			state = initialState;
		}

		// https://www.youtube.com/watch?v=yGhfUcPjXuE&t=1014s
		public Quaternion Update(float timestep, Quaternion target) {
			float blend = Mathf.Pow(2, -timestep * approachSpeed);
			state = Quaternion.Slerp(target, state, blend);
			return state;
		}
	}
}
