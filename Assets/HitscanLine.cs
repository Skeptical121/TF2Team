using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitscanLine : MonoBehaviour
{
	private Vector3 from;
	private Vector3 to;
	private float timeAlive;
	private float size = 2f;
	private float speed = 40f;
	private float totalDist;

	// Start is called before the first frame update
	public void Init(Vector3 from, Vector3 to)
	{
		this.from = from;
		this.to = to;
		totalDist = Vector3.Distance(from, to);
		timeAlive = 0;
		Update();
	}

	// Update is called once per frame
	void Update()
	{
		timeAlive += Time.deltaTime;
		GetComponent<LineRenderer>().SetPosition(0, Vector3.Lerp(from, to, Mathf.InverseLerp(0, totalDist, timeAlive * speed - size)));
		GetComponent<LineRenderer>().SetPosition(1, Vector3.Lerp(from, to, Mathf.InverseLerp(0, totalDist, timeAlive * speed)));
	}
}
