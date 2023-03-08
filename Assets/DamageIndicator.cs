using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour
{
	public Player player;
	public Quaternion worldDir;
	private float timeAlive;

	private void Start()
	{
		timeAlive = 0;
	}

	// Thickness is decided by damage total
	private void Update()
	{
		// Quaternion lookDir = ps.lookDir;

		Quaternion rel = Quaternion.Inverse(player.playerDir) * worldDir; // Quaternion.(lookDir * Vector3.forward, worldDir * Vector3.forward); // worldDir * Quaternion.Inverse(lookDir);

		Vector3 relEuler = rel.eulerAngles;

		// float angle = Mathf.Atan2(relEuler.x, relEuler.y);
		float angle = relEuler.y * Mathf.Deg2Rad;
		angle = -angle - Mathf.PI / 2;
		// angle += Mathf.PI;

		transform.eulerAngles = new Vector3(0, 0, 90 + angle * Mathf.Rad2Deg);
		transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(-Mathf.Cos(angle) * 300, -Mathf.Sin(angle) * 300);

		timeAlive += Time.deltaTime;

		if (timeAlive >= 1.0f)
			Destroy(gameObject);
		else
		{
			Color c = GetComponent<RawImage>().color;
			c.a = 1 - timeAlive;
			GetComponent<RawImage>().color = c;
		}
	}
}
