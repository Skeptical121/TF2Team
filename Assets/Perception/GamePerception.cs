using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MGEPerception
{
	const float MAX_DIST = 20.48f * Player.HAMMER_SCALER;

	PlayerRecord[] players;
	ProjectileRecord[] projectiles;
	public float healthPackSpawnTime;
	public float gameTime;
	public MGEPerception(MGEPerception prev, Player obs)
	{
		gameTime = obs.game.time;
		players = new PlayerRecord[] { new PlayerRecord(obs, obs), new PlayerRecord(((MGE)obs.game).GetPlayer(1 - obs.team), obs) };

		projectiles = new ProjectileRecord[2];

		Projectile[] closest = new Projectile[2];
		foreach (Projectile proj in obs.game.projectiles)
		{
			int index = proj.owner.team == obs.team ? 0 : 1;
			if (closest[index] == null || Vector3.SqrMagnitude(proj.transform.position - obs.Center()) < Vector3.SqrMagnitude(closest[index].transform.position - obs.Center()))
				closest[index] = proj;
		}

		// Only 1 projectile recorded currently
		for (int index = 0; index < closest.Length; index++)
		{
			if (closest[index] != null)
				projectiles[index] = new ProjectileRecord(closest[index], obs);
		}
		healthPackSpawnTime = obs.game.transform.Find("SmallHealthKit").GetComponent<ItemPack>().timeToSpawn;
	}

	public void Observe(VectorSensor sensor, Player p)
	{
		PlayerRecord myState = new PlayerRecord(p, p); // Observe current state of player (not effected by reaction time)
		sensor.OneHotObserve(p, "ArenaIndex", ((MGE)p.game).arenaIndex, 4);

		sensor.Observe(p, "HPTime", healthPackSpawnTime / 10.0f);

		// What you're looking at, distance + normal
		float dist = MAX_DIST;
		Vector3 normal = Vector3.zero;
		if (Physics.Raycast(p.CameraPos(), p.lookDir * Vector3.forward, out RaycastHit hit, MAX_DIST, 1 << 0))
		{
			dist = hit.distance;
			normal = Quaternion.Inverse(p.playerDir) * hit.normal;
		}
		sensor.Observe(p, "LookAtDist", dist);
		sensor.Observe(p, "LookAtNormal", normal);

		// 4 corners below:
		for (int c = 0; c < 4; c++)
		{
			dist = MAX_DIST;
			if (Physics.Raycast(p.BoundingBox().LowerCorner(c), Vector3.down, out hit, MAX_DIST, 1 << 0))
				dist = hit.distance;
			sensor.Observe(p, "GroundCorner" + c, dist / MAX_DIST);
		}

		// Observe ground next to you:
		Vector3[] deltas = { new Vector3(2, 0, 3), new Vector3(-2, 0, 3), new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(0, 0, 6), new Vector3(3, 0, 0), new Vector3(-3, 0, 0), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(0, 0, -3) };
		Vector3 boundsSize = p.BoundingBox().Bounds;
		foreach (Vector3 delta in deltas)
		{
			dist = MAX_DIST;
			Vector3 start = p.Center() + p.playerDir * new Vector3(delta.x * boundsSize.x, 1.5f * boundsSize.y, delta.z * boundsSize.z);
			if (Physics.CheckSphere(start, 0.01f * Player.HAMMER_SCALER, 1 << 0))
				dist = 0; // In collider...
			else if (Physics.Raycast(start, Vector3.down, out hit, MAX_DIST, 1 << 0))
				dist = hit.distance;
			sensor.Observe(p, "NextToPlayerGround", dist / MAX_DIST);
		}


		sensor.Observe(p, "MoveXBuffer", p.input.LeftBuffer && !p.input.RightBuffer ? -1 : p.input.RightBuffer && !p.input.LeftBuffer ? 1 : 0);
		sensor.Observe(p, "MoveZBuffer", p.input.BackBuffer && !p.input.ForwardBuffer ? -1 : p.input.ForwardBuffer && !p.input.BackBuffer ? 1 : 0);
		sensor.Observe(p, "Jump", p.input.JumpBuffer);
		ObservePlayer(sensor, p, myState);
		ObservePlayer(sensor, p, players[1]);
		ObserveProjectile(sensor, p, projectiles[0]);
		ObserveProjectile(sensor, p, projectiles[1]);
	}

	private void ObservePlayer(VectorSensor sensor, Player p, PlayerRecord player)
	{
		sensor.Observe(p, "Merc", p.merc == Merc.Soldier ? 1 : 0);
		sensor.Observe(p, "RelVel / GlobalPos / LookDir", Obs.RelVel(p, player.velocity) / Player.MAX_SPEED);
		Obs.GlobalPosObservation(sensor, p, null, player.globalCenterPos);
		Vector3 lookDir = Obs.GlobalLook(p, player.lookDir * Vector3.forward);
		sensor.Observe(p, null, lookDir);
		sensor.Observe(p, "IsCrouched", player.crouched);
		sensor.Observe(p, "IsGrounded", player.isGrounded);
		sensor.Observe(p, "Health", player.health / (float)player.merc.MaxHealth());
		sensor.Observe(p, "Ammo", player.primary.ammo / (float)player.primary.type.Ammo());
		sensor.Observe(p, "TotalAmmo", player.primary.totalAmmo / (float)player.primary.type.TotalAmmo()); // Arguably not needed
		sensor.Observe(p, "TimeSinceFire", Mathf.Clamp01(player.primary.timeSinceFire / (float)player.primary.type.FireTime()));
		sensor.Observe(p, "TimeSinceReload", Mathf.Clamp01(player.primary.timeSinceReload / (float)player.primary.type.ReloadTime(player.primary.firstReload)));
		sensor.Observe(p, "EquippedWeapon", player.equippedWeapon);

		float dist = MAX_DIST;
		if (Physics.Raycast(player.globalCenterPos, Vector3.down, out RaycastHit hit, MAX_DIST, 1 << 0))
			dist = hit.distance;
		sensor.Observe(p, "GroundBelow", dist / MAX_DIST);

		// Note how directions are using the observer p
		foreach (Vector3 dir in new Vector3[] { p.playerDir * Vector3.forward, p.playerDir * Vector3.back, p.playerDir * Vector3.right, p.playerDir * Vector3.left })
		{
			dist = MAX_DIST;
			float upNormal = 0;
			if (Physics.Raycast(player.globalCenterPos, dir, out hit, MAX_DIST, 1 << 0))
			{
				dist = hit.distance;
				upNormal = hit.normal.y;
			}
			sensor.Observe(p, "SurroundDist", dist / MAX_DIST);
			sensor.Observe(p, "SurroundUpNormal", upNormal);
		}

		if (p != player.playerObj) // Is it opponent
		{
			sensor.Observe(p, null, Obs.RelPos(p, player.globalCenterPos));
			Vector3 relOppLookDir = Obs.RelVel(p, player.lookDir * Vector3.forward);
			sensor.Observe(p, null, relOppLookDir.x);
			sensor.Observe(p, null, relOppLookDir.z);

			// How far are you off in vertical aim (in angle) to the center?
			Vector3 delta = Obs.RelPos(p, player.globalCenterPos);
			float oppUpDownRot = Mathf.Atan2(delta.y, new Vector3(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
			float upDownRot = p.upDownRotation;
			sensor.Observe(p, "Delta UpDownRot", (oppUpDownRot - upDownRot) / 180.0f);
		}
	}

	private void ObserveProjectile(VectorSensor sensor, Player p, ProjectileRecord proj)
	{
		if (proj != null)
		{
			sensor.Observe(p, "Projectile RelPos / RelVel", /*Obs.SignedSqrtMax(*/Obs.RelPos(p, proj.globalPos)/*, 15.36f * Player.HAMMER_SCALER, 10.24f * Player.HAMMER_SCALER)*/);
			sensor.Observe(p, null, Obs.RelVel(p, proj.vel) / ProjectileType.Rocket.FireSpeed());
		}
		else
		{
			for (int n = 0; n < 6; n++)
				sensor.AddObservation(0.0f);
		}
	}
}

public class GamePerception
{
	// We record everything we see, but then we only use stuff from previous iterations except for your own player?

	public float gameTime;
	GameStateRecord gameState;
	TeamInfoRecord[] teamInfo;
	PlayerRecord[] players;
	List<ProjectileRecord> projectiles;

	private const float HORIZONTAL_DEGREES = 53;
	private const float VERTICAL_DEGREES = 30;

	public GamePerception(GamePerception prev, Player obs)
	{
		gameTime = obs.game.time;
		gameState = new GameStateRecord((TeamFight)obs.game, obs);
		teamInfo = new TeamInfoRecord[2];
		for (int t = 0; t < 2; t++)
		{
			teamInfo[t] = new TeamInfoRecord(t, obs);
		}

		Vector3 lookEuler = obs.lookDir.eulerAngles;

		// TODO: Limit everything here to what you can actually see while playing the game!!! (That's why we have the "time" part of Record)
		players = new PlayerRecord[obs.game.players.Count];
		for (int i = 0; i < obs.game.players.Count; i++)
		{
			Player p = obs.game.players[i];
			if (p == obs || !p.health.isAlive || p.timeAlive <= 0.25f || (obs.timeAlive <= 0.25f && p.team == obs.team))
			{
				players[i] = new PlayerRecord(p, obs);
				continue;
			}

			Vector3[] checkPoints = { p.Center(), p.Center() + new Vector3(0, p.HalfExtents().y, 0), p.Center() - new Vector3(0, p.HalfExtents().y, 0) };
			foreach (Vector3 checkPoint in checkPoints)
			{
				Vector3 diffDir = Vector3.Normalize(checkPoint - obs.CameraPos());
				Vector3 diffEuler = Quaternion.LookRotation(diffDir).eulerAngles;

				if (Mathf.Abs(diffEuler.x - lookEuler.x) <= VERTICAL_DEGREES && (Mathf.Abs(diffEuler.y - lookEuler.y) <= HORIZONTAL_DEGREES || Mathf.Abs(diffEuler.y - lookEuler.y) >= 360 - HORIZONTAL_DEGREES)
					&& !Physics.Raycast(obs.CameraPos(), diffDir, Vector3.Distance(obs.CameraPos(), checkPoint), LayerHandler.Visual()))
				{
					players[i] = new PlayerRecord(p, obs);
					break;
				}
			}
			if (players[i] == null && prev != null)
				players[i] = prev.players[i]; // grab latest player record... if it exists
		}
		projectiles = new List<ProjectileRecord>();
		foreach (Projectile p in obs.game.projectiles)
		{
			Vector3 diffDir = Vector3.Normalize(p.transform.position - obs.CameraPos());
			Vector3 diffEuler = Quaternion.LookRotation(diffDir).eulerAngles;

			if (Mathf.Abs(diffEuler.x - lookEuler.x) <= VERTICAL_DEGREES && (Mathf.Abs(diffEuler.y - lookEuler.y) <= HORIZONTAL_DEGREES || Mathf.Abs(diffEuler.y - lookEuler.y) >= 360 - HORIZONTAL_DEGREES)
				&& !Physics.Raycast(obs.CameraPos(), diffDir, Vector3.Distance(obs.CameraPos(), p.transform.position), LayerHandler.Visual()))
				projectiles.Add(new ProjectileRecord(p, obs));
		}
	}

	public void Observe(VectorSensor sensor, Player obs)
	{
		gameState.Observe(sensor, obs);

		List<PlayerRecord> players = new List<PlayerRecord>();
		foreach (PlayerRecord pr in this.players)
		{
			if (pr != null)
				players.Add(pr);
		}
		players.Sort(new SortByClosePlayer { obs = obs });
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].IsPlayer(obs))
			{
				players[i].Observe(sensor, obs, gameTime, false);
				break;
			}
		}

		List<ProjectileRecord> projectiles = new List<ProjectileRecord>(this.projectiles);
		projectiles.Sort(new SortByCloseProjectile { obs = obs });
		for (int t = obs.team; t != -1; t = t == obs.team ? 1 - t : -1)
		{
			teamInfo[t].Observe(sensor, obs);

			// Sort by how close they are, and record up to Base.self.perceptionSettings.playersPerTeam
			int num = t == obs.team ? 1 : 0;
			for (int i = 0; i < players.Count; i++)
			{
				if (t == players[i].team && !players[i].IsPlayer(obs) && num++ < Base.self.perceptionSettings.playersPerTeam)
					players[i].Observe(sensor, obs, gameTime, num <= 1 && t != obs.team); // Only give target info for "most important" 2 enemies
			}
			for (; num < Base.self.perceptionSettings.playersPerTeam; num++)
				PlayerRecord.FakeObserve(sensor, obs, num <= 1 && t != obs.team); // Only give target info for "most important" 2 enemies


			int maxProjectiles = t == obs.team ? Base.self.perceptionSettings.teamProjectiles : Base.self.perceptionSettings.oppProjectiles;

			num = 0;
			for (int i = 0; i < projectiles.Count; i++)
			{
				if (t == projectiles[i].team && num++ < maxProjectiles)
					projectiles[i].Observe(sensor, obs);
			}
			for (; num < maxProjectiles; num++)
				ProjectileRecord.FakeObserve(sensor, obs);
		}
	}
}

// TODO: Sort these based on where you're looking as well, as well as how recent the data is from
public class SortByClosePlayer : IComparer<PlayerRecord>
{
	public Player obs;
	public int Compare(PlayerRecord x, PlayerRecord y)
	{
		if (!x.alive && !y.alive)
			return 0;
		if (!x.alive && y.alive)
			return 1;
		if (x.alive && !y.alive)
			return -1;
		else if (x.gameTime < y.gameTime)
			return 1;
		else if (x.gameTime > y.gameTime)
			return -1;
		else
			return Vector3.SqrMagnitude(x.globalCenterPos - obs.Center()).CompareTo(Vector3.SqrMagnitude(y.globalCenterPos - obs.Center()));
	}
}


public class SortByCloseProjectile : IComparer<ProjectileRecord>
{
	public Player obs;
	public int Compare(ProjectileRecord x, ProjectileRecord y)
	{
		if (x.gameTime < y.gameTime)
			return -1;
		else if (x.gameTime > y.gameTime)
			return 1;
		else
			return Vector3.SqrMagnitude(x.globalPos - obs.Center()).CompareTo(Vector3.SqrMagnitude(y.globalPos - obs.Center()));
	}
}