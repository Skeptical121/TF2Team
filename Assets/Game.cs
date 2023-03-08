using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/* REWARD STRUCTURE

	// For now, just a mid fight
	
	// Winning (capping mid point) = Team [5]

	// Cap time gained for total cap time = Team [1]

	// Take damage as a % of total health = Solo [0.5]
	// Deal damage as a % of total health = Solo [0.5] (Gets distributed among team if it was self damage)

	// Healing falls under the damage category, but with most of that reward going to the healer

	// MERC_VAlUE = [Scout = 1, Soldier = 1, Demoman = 1.5, Medic = 1] (Medic has ubercharge portion, remember)
	// Die = Solo [MERC_VALUE]
	// Kill = Solo [MERC_VALUE]

	// Gained ubercharge = Team [2] for full ubercharge, the person who kills the medic gets the reward on the enemy team


	TODO:

	Add a reward for missing / aiming well with the scattergun / pistol
	We won't do it for projectiles

	

*/

public abstract class Game : MonoBehaviour
{
	public Map map;

	public Stats stats;
	
	public List<Player> players;
	public List<Projectile> projectiles;
	public List<GameObject> healthPacks;
	public List<GameObject> ammoPacks;

	public float time;

	// There is no match timer since we only play 1 round at a time

	// private int cpControlState; // 0 = red owns 4 points, 1 = red owns 3 points, 2 = mid fight, 3 = blue owns 3 points, 4 = blue owns 4 points

	// private float[] capPercentage = new float[2]; // cap percentage for blue of the opposing point & cap percentage for red of the opposing point, mid fight works the same way just as the mid point being the opposing point

	// private int[] numCapping = new int[2];

	public int arenaType;




	public virtual void Init(int arenaType, Vector3 offset)
	{
		this.arenaType = arenaType;
		stats = new Stats();
		transform.position = offset;
		//if (!Base.self.maps.ContainsKey(gameObject.name))
		//	Base.self.maps.Add(gameObject.name, new Map(transform));
		// Debug.Log("Num maps = " + Base.self.maps.Count + ", name = " + gameObject.name);
		map = Base.self.GetMap(gameObject.name);

		int[] classes = { arenaType / 2, arenaType % 2 }; // 6, 3, 1, 1, 0, 0 };
		players = new List<Player>();
		for (int t = 0; t < (this is Jump ? 1 : 2); t++)
		{
			int playerID = 0;

			GameObject addP = Instantiate((t == 0 ? Base.self.playerPrefabsBlue[classes[t]] : Base.self.playerPrefabsRed[classes[t]]).gameObject, transform);
			Player p = addP.GetComponent<Player>();
			if (Base.self.renderStart && offset == Vector3.zero) // && t == 0)
			{
				if (t == Base.self.playerControl)
					p.isPlayer = true;
				else if (-t - 2 == Base.self.playerControl)
					p.isSpectator = true;
			}
			p.Init(this, t, playerID);
			players.Add(p);
		}
		projectiles = new List<Projectile>();
		StartGame();
	}

	public void DetonateStickies(Player player)
	{
		for (int i = projectiles.Count - 1; i >= 0; i--)
		{
			Projectile p = projectiles[i];
			if (p.type == ProjectileType.Sticky && p.owner == player && p.timeAlive >= PhysicsProjectile.STICKY_ARM_TIME)
			{
				p.Explode(null);
			}
		}
	}

	public Vector3 RandomSpawn(int team)
	{
		return map.RandomSpawn(transform.position, team);
	}

	public GameObject ClosestPack(Vector3 pos, bool healthPack)
	{
		GameObject closest = null;
		float dist = float.MaxValue;
		foreach (GameObject item in (healthPack ? healthPacks : ammoPacks))
		{
			float itemDist = Vector3.Distance(pos, item.transform.position);
			if (itemDist < dist)
			{
				closest = item;
				dist = itemDist;
			}
		}
		return closest;
	}

	// -1 for both teams
	public IEnumerable<Player> AlivePlayers(int team = -1)
	{
		return players.Where((Player p) => p.health.isAlive && (team == -1 || p.team == team));
	}

	public IEnumerable<Player> DeadPlayers(int team = -1)
	{
		return players.Where((Player p) => !p.health.isAlive && (team == -1 || p.team == team));
	}

	protected virtual void StartGame()
	{
		time = 0;
		// float arenaWidth = resetParams.GetWithDefault("arena_width", 11.0f);

		foreach (Player player in players)
		{
			player.Spawn(true, true);
		}

		stats.Reset();
	}

	protected void Tick()
	{
		for (int i = projectiles.Count - 1; i >= 0; i--)
		{
			projectiles[i].Tick();
		}
		for (int i = players.Count - 1; i >= 0; i--)
		{
			players[i].Tick();
		}
	}

	public abstract SpawnInfo GetSpawnInfo(Player player, bool randomSpawn);
}

public struct SpawnInfo
{
	public Vector3 position;
	public Quaternion rotation; // Should not be looking up or down at all
	public Vector3 velocity;
	public float healthChangePercent; // At 0, spawn at full health... 1 = double health
}