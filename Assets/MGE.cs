using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class MGE : Game
{
	int[] wins = new int[2];
	public GameObject[] arenas;
	public int arenaIndex;

	protected override void StartGame()
	{
		RoundInit();
		base.StartGame();
		stats.startPos[0] = players[0].Center();
		stats.startPos[1] = players[1].Center();
	}

	public void RoundInit()
	{
		for (int a = 0; a < arenas.Length; a++)
			arenas[a].SetActive(false);
		arenaIndex = Random.Range(0, arenas.Length);
		arenas[arenaIndex].SetActive(true);
	}

	public override SpawnInfo GetSpawnInfo(Player player, bool randomSpawn)
	{
		Transform spawnPoints = arenas[arenaIndex].transform.Find("MGESpawn");
		Transform spawnPoint = spawnPoints.GetChild(UnityEngine.Random.Range(0, spawnPoints.childCount));
		BoundingBox spawnZone = new BoundingBox(spawnPoint.position - spawnPoint.lossyScale / 2, spawnPoint.position + spawnPoint.lossyScale / 2);
		return new SpawnInfo { position = spawnZone.RandPos(), rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0),
			healthChangePercent = Random.Range(-0.5f, 0.5f) * Random.Range(0.0f, 1.0f), velocity = new Vector3(
				Random.Range(-Player.MAX_GROUND_SPEED, Player.MAX_GROUND_SPEED) * Random.Range(0.0f, 1.0f),
				Random.Range(-Player.MAX_GROUND_SPEED, Player.MAX_GROUND_SPEED) * Random.Range(0.0f, 1.0f),
				Random.Range(-Player.MAX_GROUND_SPEED, Player.MAX_GROUND_SPEED) * Random.Range(0.0f, 1.0f))
			};
	}

	// Assumes 1v1
	public Player GetPlayer(int team)
	{
		foreach (Player p in players)
		{
			if (p.team == team)
				return p;
		}
		return null;
	}

	private void GatherTickStats()
	{
		stats.numTicks++;
		for (int t = 0; t < 2; t++)
		{
			Vector3 delta = GetPlayer(1 - t).Center() - GetPlayer(t).CameraPos();
			stats.lookAtOppHorizontal[t] += Quaternion.Angle(Quaternion.LookRotation(new Vector3(delta.x, 0, delta.z), Vector3.up), GetPlayer(t).playerDir);
			stats.lookAtOppVertical[t] += Quaternion.Angle(Quaternion.LookRotation(new Vector3(0, delta.y, Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z)), Vector3.up), Quaternion.Euler(GetPlayer(t).upDownRotation, 0, 0));
			stats.verticalRot[t] += GetPlayer(t).upDownRotation;
			stats.maxUpVelocity[t] = Mathf.Max(stats.maxUpVelocity[t], GetPlayer(t).velocity.y);
			stats.grounded[t] += GetPlayer(t).isGrounded ? 1 : 0;
		}
		stats.distToOpp += Vector3.Distance(GetPlayer(0).Center(), GetPlayer(1).Center());
		stats.verticalDistToOpp += GetPlayer(0).Center().y - GetPlayer(1).Center().y;
		stats.absVerticalDistToOpp += Mathf.Abs(GetPlayer(0).Center().y - GetPlayer(1).Center().y);
	}

	private void FixedUpdate()
	{
		GatherTickStats();
		Tick();
		bool[] dead = { false, false };
		foreach (Player p in DeadPlayers())
		{
			stats.deaths[p.team]++;
			dead[p.team] = true;
		}

		time += Time.fixedDeltaTime;
		
		if ((dead[0] && dead[1]) || time >= 30) // Max 30 seconds per MGE; forces the player with the advantage to close it out
			GameOver(-1);
		else if (dead[1])
			GameOver(0);
		else if (dead[0])
			GameOver(1);
	}

	private void GameOver(int winner)
	{
		if (winner == 0 || winner == 1)
		{
			GetPlayer(winner).agent.SetReward(1);
			GetPlayer(1 - winner).agent.SetReward(-1);

			wins[winner]++;
			if (Base.self.renderStart)
			{
				GameObject.Find("BlueScoreText").GetComponent<Text>().text = winner == 0 ? "<b>" + wins[0].ToString() + "</b>" : wins[0].ToString();
				GameObject.Find("RedScoreText").GetComponent<Text>().text = winner == 1 ? "<b>" + wins[1].ToString() + "</b>" : wins[1].ToString();
			}
		}
		// Ensure tie gets reported:
		if (winner == -1)
		{
			GetPlayer(0).agent.SetReward(0);
			GetPlayer(1).agent.SetReward(0);
		}
		GetPlayer(0).agent.EndEpisode();
		GetPlayer(1).agent.EndEpisode();

		ReportStats(winner);

		// Remove projectiles:
		for (int i = projectiles.Count - 1; i >= 0; i--)
		{
			projectiles[i].Explode(null);
		}

		StartGame();
	}

	private void ReportStats(int winner)
	{
		if (GetPlayer(0).merc != GetPlayer(1).merc)
		{
			if (winner != -1) // Note how it doesn't record '0' for ties here
				Academy.Instance.StatsRecorder.Add("Scout/EndInWinRatio", GetPlayer(winner).merc == Merc.Scout ? 1 : 0);
			Academy.Instance.StatsRecorder.Add("Scout/EndInTieRatio", winner == -1 ? 1 : 0);
		}

		float timeMult = 1.0f; // 60.0f / time; // time mult is problematic as it makes short sessions count really high #s for the average)

		Academy.Instance.StatsRecorder.Add("Damage/ScattergunDamage", stats.scatterGunDamage * timeMult);
		Academy.Instance.StatsRecorder.Add("Damage/ScattergunShots", stats.scatterGunShotsFired * timeMult);
		Academy.Instance.StatsRecorder.Add("Damage/PistolDamage", stats.pistolDamage * timeMult);
		Academy.Instance.StatsRecorder.Add("Damage/PistolShots", stats.pistolShotsFired * timeMult);
		Academy.Instance.StatsRecorder.Add("Damage/RocketShots", stats.rocketsExploded * timeMult);
		Academy.Instance.StatsRecorder.Add("Damage/DirectRocketDamage", stats.directRocketDamage * timeMult);
		Academy.Instance.StatsRecorder.Add("Damage/RocketSelfDamage", stats.rocketSelfDamage * timeMult);
		Academy.Instance.StatsRecorder.Add("Damage/OppRocketDamage", stats.oppRocketDamage * timeMult);

		if (stats.rocketsExploded > 0)
		{
			Academy.Instance.StatsRecorder.Add("Soldier/RocketsExploded", stats.rocketsExploded, StatAggregationMethod.Sum);
			Academy.Instance.StatsRecorder.Add("Soldier/Directs", stats.directRockets, StatAggregationMethod.Sum);
			Academy.Instance.StatsRecorder.Add("Soldier/HitOpp", stats.rocketsHitOpp, StatAggregationMethod.Sum);
			Academy.Instance.StatsRecorder.Add("Soldier/RocketDistToOpp", stats.rocketDistToOpp, StatAggregationMethod.Sum); // Obviously rocket jumping would bring this number up
		}


		for (int t = 0; t < 2; t++)
		{
			Player player = GetPlayer(t);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/Jumps", stats.jumps[t,0] * timeMult);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/HealthKits", stats.healthKits[t] * timeMult);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/Grounded", stats.grounded[t] / (float)stats.numTicks);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/LookAtOppHorizontal", stats.lookAtOppHorizontal[t] / (float)stats.numTicks);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/LookAtOppVertical", stats.lookAtOppVertical[t] / (float)stats.numTicks);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/VerticalRot", stats.verticalRot[t] / (float)stats.numTicks);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/DistanceToOpp", stats.distToOpp / (float)stats.numTicks);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/VerticalDistToOpp", stats.verticalDistToOpp / (float)stats.numTicks);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/AbsVerticalDistToOpp", stats.absVerticalDistToOpp / (float)stats.numTicks);
			Academy.Instance.StatsRecorder.Add(player.merc.ToString() + "/MaxUpVelocity", stats.maxUpVelocity[t]);
		}
	}
}
