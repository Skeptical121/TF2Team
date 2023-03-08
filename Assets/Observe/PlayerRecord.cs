
// Per player on each team [2 teams * 6 players]
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PlayerRecord : Record
{
	public Player playerObj;

	public Merc merc; // Mercenary / class
	public int team;
	public int playerID;

	public bool alive;
	public float health;
	public Vector3 globalCenterPos;
	public Vector3 velocity;
	public Quaternion playerDir;
	public Quaternion lookDir;
	public bool crouched;
	public float ubered; // Time left on uber (when you get flashed, it lasts for 1 second I think)


	public WeaponState primary;
	public WeaponState secondary;
	public WeaponState melee;
	public int equippedWeapon; // 0 = primary, 1 = secondary, 2 = melee

	public bool usedDoubleJump; // SCOUT only

	public bool isGrounded;

	public float respawnTimer;
	public bool isAlive;

	// public float distanceAboveGround;


	public PlayerRecord(Player player, Player obs) : base(obs.game.time)
	{
		playerObj = player;
		team = player.team;
		playerID = player.playerID;
		merc = player.merc;
		alive = player.health.isAlive;
		health = player.health.health;
		globalCenterPos = player.Center();
		velocity = player.velocity;
		playerDir = player.playerDir;
		lookDir = player.lookDir;
		crouched = player.isCrouched;
		ubered = player.health.ubered;


		primary = player.weapons[0].state;
		if (player.weapons[1] != null)
			secondary = player.weapons[1].state;
		else
			secondary = new WeaponState();
		melee = player.weapons[2].state;
		equippedWeapon = player.equipped;
		usedDoubleJump = player.usedDoubleJump;

		isGrounded = player.isGrounded;

		respawnTimer = player.respawnTimer;
		isAlive = player.health.isAlive;
		// distanceAboveGround = player.distanceAboveGround;
	}

	public bool IsPlayer(Player obs)
	{
		return team == obs.team && playerID == obs.playerID;
	}

	// Relative position (x, y, z, dist?)
	// Absolute position???
	// Relative velocity (x, y, z, magnitude?) (relative to our rotation, not to our velocity)

	// Look angle (Quaternion.Inverse(playerDir) * opp.lookDir * Vector3.forward)

	// Time since fire
	// Time since reload
	// Estimated ammo (for each weapon, including extra ammo?), health?
	// Weapon currently equipped
	// Ubered
	// Being healed?
	// Crouching? does it matter...
	// Used double jump?

	// Time since seen

	// Is alive?
	// Respawn timer (if dead...)

	// Is team member or opponent... we have different slots for this, we sort the teammates / enemies by how close they are, however, with dead players listed last

	// Time since taken damage (for crit heals..)

	public void Observe(VectorSensor sensor, Player obs, float gameTime, bool target)
	{
		Obs.ObserveString(obs, "Player " + (team == 0 ? "Blue" : "Red") + "[" + playerID + "]");

		if (!IsPlayer(obs))
		{
			sensor.Observe(obs, "RelPos", Obs.SignedSqrtMax(Obs.RelPos(obs, globalCenterPos), 30.72f * Player.HAMMER_SCALER, 10.24f * Player.HAMMER_SCALER));

			// TODO: Add more position stuff like angle to crosshair, we have ObserveAsTarget in this file for that
		}

		sensor.Observe(obs, "RelVel / GlobalPos / LookDir", Obs.RelVel(obs, velocity) / Player.MAX_SPEED);
		Obs.GlobalPosObservation(sensor, obs, null, globalCenterPos);

		Vector3 oppLookDir;
		if (IsPlayer(obs))
			oppLookDir = Obs.GlobalLook(obs, lookDir * Vector3.forward);
		else
			oppLookDir = Quaternion.Inverse(obs.playerDir) * lookDir * Vector3.forward;
		sensor.Observe(obs, null, oppLookDir);

		sensor.AddOneHotObservation(merc.SixesID(), 4);
		
		if (IsPlayer(obs) || target)
		{
			sensor.AddOneHotObservation(equippedWeapon, Player.WEAPON_SLOTS);
			WeaponState equipped = equippedWeapon == 0 ? primary : equippedWeapon == 1 ? secondary : melee;
			sensor.Observe(obs, "TimeSinceFire/Reload", Mathf.InverseLerp(0, equipped.type.FireTime(), equipped.timeSinceFire));
			sensor.Observe(obs, null, Mathf.InverseLerp(0, equipped.type.ReloadTime(equipped.firstReload), equipped.timeSinceReload));

			sensor.Observe(obs, "Ammo P/S", primary.ammo / (float)primary.type.Ammo());
			sensor.Observe(obs, null, secondary.ammo / (float)secondary.type.Ammo());
			sensor.Observe(obs, "Grounded", isGrounded);
		}


		if (IsPlayer(obs))
		{
			sensor.Observe(obs, "Total Ammo P/S", primary.totalAmmo / (float)primary.type.TotalAmmo());
			sensor.Observe(obs, null, secondary.totalAmmo / (float)secondary.type.TotalAmmo());
		}



		sensor.Observe(obs, "Health / IsAlive / RespawnTimer", health / (merc.MaxHealth() * 1.5f));
		sensor.Observe(obs, null, isAlive);
		sensor.Observe(obs, null, respawnTimer / Player.MAX_RESPAWN_TIMER); // 0 when alive

		if (!IsPlayer(obs))
		{
			sensor.Observe(obs, "IsCurrentData", this.gameTime == gameTime);
			sensor.Observe(obs, null, Mathf.Clamp01((gameTime - this.gameTime) / 15.0f)); // Up to 15 seconds
			if (target)
			{
				Vector3 euler = obs.lookDir.eulerAngles;
				Vector3 diffEuler = Quaternion.LookRotation(Vector3.Normalize(globalCenterPos - obs.CameraPos())).eulerAngles; // diffAngle.eulerAngles;

				float pdaX = CorrectEulerX(diffEuler.x) - CorrectEulerX(euler.x);
				float pdaY = diffEuler.y - euler.y;
				if (pdaY > 180)
					pdaY -= 360;
				else if (pdaY < -180)
					pdaY += 360;

				// 74 vertical, 106 horizontal
				sensor.Observe(obs, "Angle", SignedSqrt(pdaX / 180.0f));
				sensor.Observe(obs, null, SignedSqrt(pdaY / 180.0f));
			}
		}
	}

	private float SignedSqrt(float val)
	{
		return Mathf.Sign(val) * Mathf.Sqrt(Mathf.Abs(val));
	}

	private static float CorrectEulerX(float eulerX)
	{
		if (eulerX > 180f)
			eulerX -= 360f;
		return eulerX;
	}

	public static void FakeObserve(VectorSensor sensor, Player obs, bool target)
	{
		int num = target ? 31 : 21;
		Obs.ObserveString(obs, $"Fake observe [{num}]"); // Fake observe always has IsPlayer as false
		for (int i = 0; i < num; i++)
		{
			sensor.AddObservation(0.0f);
		}
	}

	public void ObserveAsTarget(VectorSensor sensor, Player obs)
	{
		// I think angles are really the main thing here, maybe some raycasts and like their relation to their environment, idk
	}
}
