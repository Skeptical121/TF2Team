using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumber : MonoBehaviour
{
	public Vector3 worldPosition;
	private float timeSinceSpawn = 0;

	// Update is called once per frame
	void Update()
	{
		timeSinceSpawn += Time.deltaTime;
		if (timeSinceSpawn >= 2.0f)
		{
			Color c = GetComponent<Text>().color;
			c.a = Mathf.InverseLerp(3.0f, 2.0f, Mathf.Clamp(timeSinceSpawn, 2.0f, 3.0f));
			GetComponent<Text>().color = c;
		}
		SetPos();
	}

	public void SetPos()
	{
		Vector2 viewportPoint = Camera.main.WorldToViewportPoint(worldPosition);
		GetComponent<RectTransform>().anchorMin = viewportPoint;
		GetComponent<RectTransform>().anchorMax = viewportPoint;
		GetComponent<RectTransform>().anchoredPosition = new Vector2(0, timeSinceSpawn * 20f);
	}
}
