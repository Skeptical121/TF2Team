using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Map
{
	// Everything needs to be public here because we create this in the editor

	public string name;
	public float[] elevations;

	// Bounds of playable space:
	public Vector3 localCenter;
	public Vector3 size;

	public Vector3Int totalSize;
	public Vector3 gridSize;

	public Map(Transform map)
	{
		name = map.gameObject.name;
		Transform bounds = map.Find("Bounds");
		localCenter = bounds.transform.localPosition;
		size = bounds.transform.localScale;

		gridSize = new Vector3(0.32f * Player.HAMMER_SCALER, 0.32f * Player.HAMMER_SCALER, 0.32f * Player.HAMMER_SCALER);

		totalSize = new Vector3Int(Mathf.CeilToInt(size.x / gridSize.x), Mathf.CeilToInt(size.y / gridSize.y), Mathf.CeilToInt(size.z / gridSize.z));
		// InitElevations(bounds);
	}

	public void InitElevations(Transform bounds)
	{
		elevations = new float[totalSize.x * totalSize.z];

		Texture2D tex = new Texture2D(totalSize.x, totalSize.z);
		tex.filterMode = FilterMode.Point;

		Vector3 globalCorner = bounds.transform.position - size / 2;
		HashSet<Vector3Int> valid = new HashSet<Vector3Int>();
		SetElevations(globalCorner, valid);
		for (int x = 0; x < totalSize.x; x++)
		{
			for (int y = 0; y < totalSize.z; y++)
			{
				SetElevation(x, y, size.y * 0.5f);
				// SetElevation(bounds.transform.position, globalCorner + new Vector3((x + 0.5f) * gridSize.x, size.y, (y + 0.5f) * gridSize.z), x, y);
			}
		}
		foreach (Vector3Int pos in valid)
		{
			SetElevation(pos.x, pos.z, Mathf.Min(GetElevation(pos.x, pos.z), pos.y * gridSize.y - size.y / 2));
		}

		// Set picture stuff:
		for (int x = 0; x < totalSize.x; x++)
		{
			for (int y = 0; y < totalSize.z; y++)
			{
				float rel = Mathf.Clamp01((GetElevation(x, y) / size.y) + 0.5f);
				tex.SetPixel(x, y, new Color(rel, rel, rel));
			}
		}
		tex.Apply();

		Debug.Log("Map size = " + totalSize.x + " x " + totalSize.y + " x " + totalSize.z);
		GameObject.Find("MapImage").GetComponent<RawImage>().texture = tex;
		GameObject.Find("MapImage").GetComponent<RawImage>().enabled = true;
	}

	private void SetElevation(int x, int y, float elevation)
	{
		elevations[x * totalSize.z + y] = elevation;
	}

	private float GetElevation(int x, int y)
	{
		return elevations[x * totalSize.z + y];
	}

	public Vector3 RandomSpawn(Vector3 offset, int team)
	{
		do
		{
			int x = UnityEngine.Random.Range(0, totalSize.x);
			int z = UnityEngine.Random.Range(0, totalSize.z);
			if (GetElevation(x, z) >= size.y * 0.49f)
				continue;
			Vector3 pos = offset + localCenter - size / 2 + new Vector3(x * gridSize.x, size.y * 0.5f + GetElevation(x, z), z * gridSize.z);
			if (!Physics.CheckBox(pos + new Vector3(0, 0.41f * Player.HAMMER_SCALER, 0), new Vector3(0.24f * Player.HAMMER_SCALER, 0.41f * Player.HAMMER_SCALER, 0.24f * Player.HAMMER_SCALER), Quaternion.identity, LayerHandler.PlayerMoveClip(team)))
			{
				return pos;
			}
		} while (true);
	}

	private void SetElevations(Vector3 globalCorner, HashSet<Vector3Int> valid)
	{
		HashSet<Vector3Int> openSet = new HashSet<Vector3Int> { new Vector3Int(totalSize.x / 2, totalSize.y / 2, totalSize.z / 2) };
		HashSet<Vector3Int> searched = new HashSet<Vector3Int>();
		while (openSet.Count > 0)
		{
			Vector3Int current = openSet.First();
			openSet.Remove(current);
			searched.Add(current);
			if (!Physics.CheckBox(globalCorner + new Vector3((current.x + 0.5f) * gridSize.x, (current.y + 0.5f) * gridSize.y, (current.z + 0.5f) * gridSize.z),
				gridSize / 2, Quaternion.identity, LayerHandler.PlayerMoveClip(0)))
				valid.Add(current);
			else
				continue;
			for (int dim = 0; dim < 3; dim++)
			{
				for (int val = -1; val <= 1; val += 2)
				{
					Vector3Int next = current;
					next[dim] += val;
					if (next[dim] >= 0 && next[dim] < totalSize[dim] && !searched.Contains(next))
						openSet.Add(next);
				}
			}
		}
		Debug.Log("Valid size = " + valid.Count + ", searched size = " + searched.Count);
	}


	// For now, just record the lowest height found...
	public void SetElevation(Vector3 globalCenter, Vector3 globalPos, int x, int y)
	{
		float precision = 0.16f * Player.HAMMER_SCALER;


		float lowestElevation = size.y * 0.5f;
		SetElevation(x, y, lowestElevation);
		while (globalPos.y > globalCenter.y + gridSize.y / 2 - size.y * 0.5f)
		{
			if (!Physics.CheckBox(globalPos, gridSize / 2, Quaternion.identity, LayerHandler.PlayerMoveClip(0)))
			{
				lowestElevation = globalPos.y - gridSize.y / 2 - globalCenter.y;
				globalPos.y -= precision;
			}
			else
			{
				SetElevation(x, y, lowestElevation);
				// globalPos.y -= gridSize.y;
				globalPos.y -= precision;
			}
		}
	}

	// Should be able to handle multiple elevations... including the ceiling...
	public float GetLocalElevation(Vector3 localPos, float radius)
	{
		Vector3 offsetFromCorner = localPos - (localCenter - size / 2);

		int x = Mathf.Clamp(Mathf.FloorToInt(offsetFromCorner.x / gridSize.x), 0, totalSize.x - 1);
		int y = Mathf.Clamp(Mathf.FloorToInt(offsetFromCorner.z / gridSize.z), 0, totalSize.z - 1);
		return GetElevation(x, y);
	}
}
