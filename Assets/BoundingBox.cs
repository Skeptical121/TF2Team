using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct BoundingBox
{
	Vector3 low;
	Vector3 high;

	public Vector3 Bounds
	{
		get => high - low;
	}

	public Vector3 Center
	{
		get => (low + high) / 2;
	}

	public BoundingBox(Vector3 low, Vector3 high)
	{
		this.low = low;
		this.high = high;
	}

	public Vector3 RandPos()
	{
		return new Vector3(UnityEngine.Random.Range(low.x, high.x), UnityEngine.Random.Range(low.y, high.y), UnityEngine.Random.Range(low.z, high.z));
	}

	public Vector3 LowerCorner(int corner)
	{
		switch (corner)
		{
			case 0: return new Vector3(low.x, low.y, low.z);
			case 1: return new Vector3(low.x, low.y, high.z);
			case 2: return new Vector3(high.x, low.y, low.z);
			case 3: return new Vector3(high.x, low.y, high.z);
			default: throw new System.Exception("Lower corner invalid: " + corner);
		}
	}

	public static float MinDistance(BoundingBox a, BoundingBox b)
	{
		Vector3 outerSize = new Vector3(
			math.max(a.high.x, b.high.x) - math.min(a.low.x, b.low.x), 
			math.max(a.high.y, b.high.y) - math.min(a.low.y, b.low.y), 
			math.max(a.high.z, b.high.z) - math.min(a.low.z, b.low.z));
		Vector3 innerSize = outerSize - a.Bounds - b.Bounds;
		return innerSize.magnitude;
	}

	public float MinDistance(Vector3 point)
	{
		Vector3 outerSize = new Vector3(
			math.max(point.x, high.x) - math.min(point.x, low.x),
			math.max(point.y, high.y) - math.min(point.y, low.y),
			math.max(point.z, high.z) - math.min(point.z, low.z));
		Vector3 innerSize = outerSize - Bounds;
		return innerSize.magnitude;
	}
}
