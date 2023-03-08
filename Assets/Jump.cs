using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Jump : Game
{
	public override SpawnInfo GetSpawnInfo(Player player, bool randomSpawn)
	{
		string spawnName = "JumpSpawn";
		Transform spawnPoints = transform.Find(spawnName).transform;
		Transform spawnPoint = spawnPoints.GetChild(UnityEngine.Random.Range(0, spawnPoints.childCount));
		return new SpawnInfo { position = spawnPoint.position, rotation = spawnPoint.rotation };
	}

	private void FixedUpdate()
	{
		Tick();

		time += Time.fixedDeltaTime;
		if (time >= 20 || DeadPlayers().Count() > 0)
			GameOver();
	}

	private void GameOver()
	{
		time = 0;
		foreach (Player p in players)
		{
			p.Spawn(false, false);
		}
	}
}
