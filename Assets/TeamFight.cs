using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class TeamFight : Game
{
	public Team[] teams; // Always 2 teams, blue & red, teams[0] & teams[1]

	public ControlPoint[] controlPoints;
	public ControlPoint[] defendingCP;
	public float roundTimer;
	public const float MAX_ROUND_TIMER = 180; // 3 minutes, tf2 uses 10 minutes... if max round timer is reached + overtime is done, the game ends in a draw since we play 1 round at a time

	public static Text[] scorePanel;
	private static int[] wins = new int[2];

	public const float CAP_MID_REWARD = 1.0f; // Group (Capping mid wins the game currently)
	public const float CAP_PERCENTAGE_REWARD = 0.3f; // Group
	public const float DAMAGE_REWARD = 0.2f; // As a % of merc's max health
	public const float UBER_REWARD = 0.4f;
	public const float MOVE_TO_CENTER_REWARD_PER_UNIT = 0.1f;
	public static readonly float[] MERC_DEATH_REWARD = { 0.2f, 0.2f, 0, 0.3f, 0, 0, 0.25f, 0, 0 };

	public int ENV_reactionTicks = 9;
	public float ENV_lookAtRewardMult = 0.0f;

	public override void Init(int arenaType, Vector3 offset)
	{
		teams = new Team[] { new Team(), new Team() };
		base.Init(arenaType, offset);
	}

	public void GameOver(int winTeam)
	{
		if (winTeam == -1)
		{
			teams[0].agentGroup.GroupEpisodeInterrupted();
			teams[1].agentGroup.GroupEpisodeInterrupted();
		}
		else
		{
			teams[winTeam].agentGroup.SetGroupReward(CAP_MID_REWARD);
			teams[1 - winTeam].agentGroup.SetGroupReward(-CAP_MID_REWARD);
			teams[winTeam].agentGroup.EndGroupEpisode();
			teams[1 - winTeam].agentGroup.EndGroupEpisode();
		}

		/*foreach (Player player in players)
		{
			if (winTeam == -1)
			{
				player.agent.EpisodeInterrupted();
			}
			else
			{
				if (player.team == winTeam)
					player.agent.SetReward(1.0f);
				else
					player.agent.SetReward(-1.0f);
				player.agent.EndEpisode();
			}
		}*/

		if (winTeam != -1)
			wins[winTeam]++;
		scorePanel[0].text = wins[0].ToString();
		scorePanel[1].text = wins[1].ToString();

		if (Base.self.renderStart)
		{
			scorePanel[0].transform.parent.GetComponent<Image>().color = winTeam == -1 ? new Color(0.5f, 0.5f, 0.5f, 1) : (winTeam == 0 ? new Color(0, 0, 1, 1) : new Color(1, 0, 0, 1));
		}

		Academy.Instance.StatsRecorder.Add("Custom/EndInWinRatio", winTeam != -1 ? 1 : 0);
		// Academy.Instance.StatsRecorder.Add("Custom/KillDistance", Vector3.Distance(winner.transform.position, loser.transform.position) * 100 / PlayerState.HAMMER_SCALER);
		Academy.Instance.StatsRecorder.Add("Custom/DistanceTravelledToCenter", stats.distanceTravelled);

		Academy.Instance.StatsRecorder.Add("Damage/ScattergunDamage", stats.scatterGunDamage);
		Academy.Instance.StatsRecorder.Add("Damage/DirectRocketDamage", stats.directRocketDamage);
		Academy.Instance.StatsRecorder.Add("Damage/RocketSelfDamage", stats.rocketSelfDamage);
		Academy.Instance.StatsRecorder.Add("Damage/OppRocketDamage", stats.oppRocketDamage);
		Academy.Instance.StatsRecorder.Add("Damage/MedigunHealing", stats.medicHealing);

		Academy.Instance.StatsRecorder.Add("DM/TotalDeaths", stats.deaths[0] + stats.deaths[1]);
		Academy.Instance.StatsRecorder.Add("DM/TotalUbercharges", stats.numUbercharges[0] + stats.numUbercharges[1]);
		if (winTeam != -1)
		{
			Academy.Instance.StatsRecorder.Add("DM/WinnerDeaths", stats.deaths[winTeam]);
			Academy.Instance.StatsRecorder.Add("DM/LoserDeaths", stats.deaths[1 - winTeam]);
			Academy.Instance.StatsRecorder.Add("DM/WinnerUbercharges", stats.numUbercharges[winTeam]);
			Academy.Instance.StatsRecorder.Add("DM/LoserUbercharges", stats.numUbercharges[1 - winTeam]);
		}

		// Reset arena instantly
		StartGame();
	}

	public int GetCPControlState(int team)
	{
		if (defendingCP[0].CapPoint() == defendingCP[1].CapPoint())
			return 2;
		else
		{
			int val = team == 0 ? defendingCP[0].CapPoint() : 4 - defendingCP[1].CapPoint();
			return val >= 2 ? val + 1 : val;
		}
	}

	public float GetCapPercentage(int team, bool thisTeam)
	{
		if (!thisTeam)
			team = 1 - team;
		return defendingCP[1 - team].CapPercentage();
	}

	public int GetNumCappers(int team, bool thisTeam)
	{
		if (!thisTeam)
			team = 1 - team;
		return defendingCP[1 - team].NumCappers(team);
	}

	private void FixedUpdate()
	{
		Tick();
		foreach (Player p in DeadPlayers())
		{
			if (p.gameObject.activeSelf)
			{
				p.GoToSpawnLocation(false);
				// p.agent.EndEpisode();
				p.gameObject.SetActive(false);
				// TODO: Teleport the player to where they're going to spawn so records are more accurate
				p.respawnTimer = 16; // TODO: Respawn waves
				stats.deaths[p.team]++;
			}
			p.respawnTimer -= Time.fixedDeltaTime;
			if (p.respawnTimer <= 0)
			{
				p.Spawn(false, false);
			}
		}

		time += Time.fixedDeltaTime;
		roundTimer += Time.fixedDeltaTime;
		// End at the midfight for now
		if (controlPoints[2].TeamOwner() == 1)
			GameOver(1);
		else if (controlPoints[2].TeamOwner() == 0)
			GameOver(0);
		else if (time >= 120)
			GameOver(-1);
	}

	protected override void StartGame()
	{
		int stage = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("stage", 3);
		if (stage == 0)
		{
			ENV_reactionTicks = 3;
			ENV_lookAtRewardMult = 1.0f;
		}
		else if (stage == 1)
		{
			ENV_reactionTicks = 6;
			ENV_lookAtRewardMult = 0.5f;
		}
		else if (stage == 2)
		{
			ENV_reactionTicks = 9;
			ENV_lookAtRewardMult = 0.2f;
		}
		else
		{
			ENV_reactionTicks = 12;
			ENV_lookAtRewardMult = 0;
		}

		for (int i = 0; i < controlPoints.Length; i++)
			controlPoints[i].Init(this, i);
		defendingCP = new ControlPoint[] { controlPoints[2], controlPoints[2] };

		roundTimer = 0;
		base.StartGame();
	}

	public override SpawnInfo GetSpawnInfo(Player player, bool randomSpawn)
	{
		if (!randomSpawn)
		{
			string spawnName = "Spawn_";
			int cpControlState = GetCPControlState(player.team);
			if (cpControlState == 4)
				spawnName += "ForwardLast";
			else if (cpControlState == 3)
				spawnName += "Forward2nd";
			else
				spawnName += "Initial";

			Transform spawnPoints = transform.Find(player.TeamName()).Find(spawnName).transform;
			Transform spawnPoint = spawnPoints.GetChild(UnityEngine.Random.Range(0, spawnPoints.childCount));
			return new SpawnInfo { position = spawnPoint.position, rotation = spawnPoint.rotation };
		}
		else
		{
			return new SpawnInfo { position = RandomSpawn(player.team), rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0) };
		}
	}
}
