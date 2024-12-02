using UnityEngine;

public static class Extensions
{
	public static void ComplexMultiplyBy(this ref Vector2 a, Vector2 b)
	{
		a = new(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
	}
}