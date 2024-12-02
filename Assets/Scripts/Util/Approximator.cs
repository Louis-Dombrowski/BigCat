using System;
using UnityEngine;

namespace Util
{
	[Serializable]
	public struct Approximator
	{
		[Header("Properties")]
		[SerializeField] public float k1, k2, k3;

		[Header("State")]
		[SerializeField] public float[] prevTarget;
		[SerializeField] public float[] position, velocity;

		public void SetDefault()
		{
			k1 = 1 / Mathf.PI;
			k2 = 1 / (2 * Mathf.PI);
			k2 *= k2;
			k3 = 0;
		}

		public void SetConstants(float responseFrequency, float dampingCoefficient, float responseBehavior)
		{
			k1 = dampingCoefficient / (Mathf.PI * responseFrequency);
			k2 = 1 / (4 * Mathf.PI * Mathf.PI * responseFrequency * responseFrequency);
			k3 = responseBehavior * dampingCoefficient / (2 * Mathf.PI * responseFrequency);
		}

		public void Initialize(int dimensions, float[] startingPosition)
		{
			if (startingPosition.Length != dimensions)
				Debug.LogWarning("Dimensions and position array mismatch");

			prevTarget = new float[dimensions];
			position = startingPosition;
			velocity = new float[dimensions];
		}
		public void Initialize(Vector3 startingPosition)
		{
			Initialize(3, new[]{startingPosition.x, startingPosition.y, startingPosition.z});
		}
		public void Initialize(Vector2 startingPosition)
		{
			Initialize(2, new[]{startingPosition.x, startingPosition.y});
		}

		public void Initialize(float startingPosition)
		{
			Initialize(1, new[]{startingPosition});
		}
		
		public float[] Update(float timestep, float[] nextTarget)
		{
			if (timestep == 0) return position;
			
			float stableK2 =
				Mathf.Max(k2,
					timestep * (timestep + k1) / 2,
					timestep * k1); // prevents jitter and keeps stability at low framerates

			for (int d = 0; d < position.Length; d++)
			{
				float velocityOfTarget =
					(nextTarget[d] - prevTarget[d]) / timestep; // estimate target's velocity from delta
				position[d] += velocity[d] * timestep; // integrate position by velocity
				velocity[d] += timestep * (nextTarget[d] + k3 * velocityOfTarget - position[d] - k1 * velocity[d]) /
				               stableK2; // integrate velocity by acceleration
				prevTarget[d] = nextTarget[d];
			}

			return position;
		}

		public Vector3 Update(float timestep, Vector3 nextTarget)
		{
			var arr = Update(timestep, new[] { nextTarget.x, nextTarget.y, nextTarget.z });
			return new(arr[0], arr[1], arr[2]);
		}
		public Vector2 Update(float timestep, Vector2 nextTarget)
		{
			var arr = Update(timestep, new[] { nextTarget.x, nextTarget.y });
			return new(arr[0], arr[1]);
		}
		public float Update(float timestep, float nextTarget)
		{
			var arr = Update(timestep, new[] { nextTarget });
			return arr[0];
		}
	}
}
