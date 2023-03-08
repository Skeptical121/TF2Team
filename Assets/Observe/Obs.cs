using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;

public static class Obs
{
	public static Vector3 SignedSqrtMax(Vector3 vector, float max, float maxHeight)
	{
		return new Vector3(
			Mathf.Clamp(Mathf.Sign(vector.x) * Mathf.Sqrt(Mathf.Abs(vector.x / max)), -1, 1), 
			Mathf.Clamp(Mathf.Sign(vector.y) * Mathf.Sqrt(Mathf.Abs(vector.y / maxHeight)), -1, 1), 
			Mathf.Clamp(Mathf.Sign(vector.z) * Mathf.Sqrt(Mathf.Abs(vector.z / max)), -1, 1));
	}

	public static Vector2 SignedSqrtMaxNoHeight(Vector3 vector, float max)
	{
		return new Vector2(
			Mathf.Clamp(Mathf.Sign(vector.x) * Mathf.Sqrt(Mathf.Abs(vector.x / max)), -1, 1),
			Mathf.Clamp(Mathf.Sign(vector.z) * Mathf.Sqrt(Mathf.Abs(vector.z / max)), -1, 1));
	}

	public static float SignedSqrtMax(float value, float max)
	{
		return Mathf.Clamp(Mathf.Sign(value) * Mathf.Sqrt(Mathf.Abs(value / max)), -1, 1);
	}

	public static Vector3 RelPos(Player obs, Vector3 globalPos)
	{
		return Quaternion.Inverse(obs.playerDir) * (globalPos - obs.Center());
	}

	public static Vector3 RelVel(Player obs, Vector3 vel)
	{
		return Quaternion.Inverse(obs.playerDir) * vel;
	}

	// This should work for velocity as well...
	public static Vector3 GlobalLook(Player obs, Vector3 lookDir)
	{
		return obs.game is TeamFight && obs.team == 1 ? Quaternion.Euler(0, 180, 0) * lookDir : lookDir;
	}

	public static void GlobalPosObservation(VectorSensor sensor, Player obs, string prefix, Vector3 globalPos)
	{
		Vector3 localPos = globalPos - obs.game.transform.position; // The game is not scaled or rotated, and are stacked on top of eachother?

		// Red team (team 1) gets flipped, Blu team (team 0) does not
		if (obs.game is TeamFight && obs.team == 1)
		{
			localPos = Quaternion.Euler(0, 180, 0) * localPos;
		}

		Vector3 posOffsetFromCenter = localPos - obs.game.map.localCenter;

		sensor.Observe(obs, prefix, new Vector3(
			Mathf.Clamp(posOffsetFromCenter.x / (obs.game.map.size.x * 0.5f), -1, 1), 
			Mathf.Clamp(posOffsetFromCenter.y / (obs.game.map.size.y * 0.5f), -1, 1), 
			Mathf.Clamp(posOffsetFromCenter.z / (obs.game.map.size.z * 0.5f), -1, 1)));
	}

	private static string observeDebug = "";

	public static void ObserveString(Player obs, string str)
	{
		if (obs.isPlayer)
			observeDebug += "\n<b>" + str + "</b>";
	}

	public static void Observe(this VectorSensor sensor, Player obs, string prefix, float value)
	{
		if (obs.isPlayer)
			observeDebug += prefix == null ? ", " + value.ToString("0.##") : "\n" + prefix + ": " + value.ToString("0.##");
		sensor.AddObservation(value);
	}

	public static void Observe(this VectorSensor sensor, Player obs, string prefix, Vector3 value)
	{
		if (obs.isPlayer)
			observeDebug += prefix == null ? ", " + value : "\n" + prefix + ": " + value;
		sensor.AddObservation(value);
	}

	public static void Observe(this VectorSensor sensor, Player obs, string prefix, Vector2 value)
	{
		if (obs.isPlayer)
			observeDebug += prefix == null ? ", " + value : "\n" + prefix + ": " + value;
		sensor.AddObservation(value);
	}

	public static void Observe(this VectorSensor sensor, Player obs, string prefix, bool value)
	{
		if (obs.isPlayer)
			observeDebug += prefix == null ? ", " + value : "\n" + prefix + ": " + value;
		sensor.AddObservation(value);
	}

	public static void OneHotObserve(this VectorSensor sensor, Player obs, string prefix, int observation, int range)
	{
		if (obs.isPlayer)
			observeDebug += prefix == null ? ", " + observation + "/" + range : "\n" + prefix + ": " + observation + "/" + range;
		sensor.AddOneHotObservation(observation, range);
	}

	public static void Display(Player obs)
	{
		if (obs.isPlayer)
		{
			GameObject.Find("ObserveDebug").GetComponent<Text>().text = observeDebug;
			observeDebug = "";
		}
	}
}