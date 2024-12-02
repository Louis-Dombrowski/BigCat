
using Unity.VisualScripting;

[System.Serializable]
public struct FloatRange
{
	public float min, max;

	public float Lerp(float t)
	{
		return (max - min) * t + min;
	}

	public float ClampWithin(float x)
	{
		if (x < min) return min;
		if (x > max) return max;
		return x;
	}

	public float Size()
	{
		return max - min;
	}
}
