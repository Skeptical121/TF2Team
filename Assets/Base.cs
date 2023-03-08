using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PerceptionSettings
{
	public int playersPerTeam;
	public int oppProjectiles;
	public int teamProjectiles; // Each class can have seperate stores for how many of their own projectiles they stay aware of (1 for soldier, 9 for demoman, probably 0 for everything else)
}

public enum GameType
{
	OrigSoldier,
	SoldierScout
}

public class Base : MonoBehaviour
{
	public static Base self;
	public Game gamePrefab;
	public static GameType gameType = GameType.SoldierScout;

	public List<Map> maps;

	private Game[] games;
	public bool renderStart;
	public int playerControl;
	public int numArenas;
	public int arenaType; // if renderStart

	public PerceptionSettings perceptionSettings;

	public Player[] playerPrefabsBlue;
	public Player[] playerPrefabsRed;


	public Map GetMap(string name)
	{
		foreach (Map map in maps)
		{
			if (map.name.Equals(name))
				return map;
		}
		Debug.LogError("Map " + name + " does not exist!");
		return null;
	}

	// Start is called before the first frame update
	void Start()
    {
		self = this;
		AudioListener.volume = renderStart ? 0.25f : 0.0f;
		Weapon.ammoPanel = GameObject.Find("AmmoText").GetComponent<Text>();
		Weapon.totalAmmoPanel = GameObject.Find("TotalText").GetComponent<Text>();
		Health.healthPanel = GameObject.Find("HealthText").GetComponent<Text>();
		Health.healthFill = GameObject.Find("HealthFill").GetComponent<Image>();
		TeamFight.scorePanel = new Text[] { GameObject.Find("BlueScoreText").GetComponent<Text>(), GameObject.Find("RedScoreText").GetComponent<Text>() };

		games = new Game[numArenas];
		for (int i = 0; i < numArenas; i++)
		{
			if (i == 0 || !renderStart)
			{
				GameObject gameObj = Instantiate(gamePrefab.gameObject);
				gameObj.name = gamePrefab.gameObject.name;
				games[i] = gameObj.GetComponent<Game>();
				games[i].Init(renderStart ? arenaType : i, new Vector3(0, i * 200, 0));
			}
		}
		GameObject.Find("MapImage").GetComponent<RawImage>().enabled = false;
    }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			Time.timeScale *= 0.5f;
			Debug.Log("Timescale = " + Time.timeScale);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			Time.timeScale *= 2f;
			Debug.Log("Timescale = " + Time.timeScale);
		}
	}
}
