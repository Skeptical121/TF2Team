using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

// We are aware of our own state at the current time, (position, rotation(this is done as an approximation though since we don't know how much the variance effected it), ammo, not health)
// For other things we need to use old data to account for reaction time

public struct PlayerStateHistory
{
	public Vector3 pos;
	public Quaternion lookDir;
	public Vector3 vel;
	public float health;

	public bool usedDoubleJump; // SCOUT only
								// Give no hints that the opponent has fired...

	public Weapon weapon;
	public int ammo;
	public float timeSinceFire;
	public float timeSinceReload;

	public InputInfo input;

	// Weapon stuff is stored in the weapons maybe? (hmm)
	// public int ammo;
}

public enum PlayerAction
{
	Strafe,
	Forward,
	Jump,
	RotateHorizontal,
	RotateVertical,
	SpecialAction, // 0 = none, 1 = jump, 2 = fire, 3 = jump & fire for soldier (or jump & class ability for demoman and just jump for all other classes?)
	Crouch // Not implemented yet

	// Scout: 0 = none, 1 = fire, 2 = N/A, 3 = alternate primary / melee
	// Soldier: 0 = none, 1 = fire, 2 = N/A, 3 = alternate primary / melee
	// Demoman: 0 = none, 1 = fire, 2 = right click, 3 = switch weapon (scroll)
	// Medic: 0 = none, 1 = fire, 2 = right click, 3 = switch weapon (scroll)
}

public enum SpecialAction
{
	None,
	Fire,
	ClassAbility,
	SwitchWeapon // This is done by scrolling... Primary -> Secondary -> Melee -> Primary
}

public enum MGEGoal
{
	Default,
	EndRoundSoon,
	EndRoundLate,
	UseMinimalRockets,
	UseMaximalRockets,
	SurfOpponentRockets,
	UseRocketJumps,
	Directs,
	Airshots,
	Flicks,
	LookUpMore,
	LookDownMore,
	PreciseFarAwaySplash,
	MoveSmoothly,
	SmoothRotations,
	LookAway,
	PlayHighGround,
	PlayLowGround,
	PlayClose,
	PlayFar,
	TrackOpponent
}

public class PlayerAgent : Agent
{
	private static readonly float[] rots = { 20f, 50f, 80f, 120f, 180f, 250f, 325f, 485f }; // { 20f, 55f, 100f, 150f, 240f, 405f }; // { 10f, 25f, 45f, 75f, 110f, 150f, 240f, 405f }; // { 7.5f, 17.5f, 30f, 42.5f, 57.5f, 80f, 105f, 140f, 210f, 350f }; // { 10f, 25f, 60f, 100f, 150f, 225f, 350f }; // { 5f, 12.5f, 20f, 30f, 40f, 50f, 62.5f, 75f, 90f, 105f, 135f, 180f, 300f }; // { 30f, 105f }
	public int currentRotIndex;
	public int currentUpDownRotIndex;
	private const int MAX_ROT_CHANGE = 7;

	// public const int NUM_STEPS_PER_DECISION = 3;

	private List<MGEPerception> history;

	private void Start()
	{
		currentRotIndex = rots.Length;
		currentUpDownRotIndex = rots.Length;
	}

	public override void OnEpisodeBegin()
	{
		base.OnEpisodeBegin();
	}

	public override void OnActionReceived(ActionBuffers actions)
	{
		base.OnActionReceived(actions);

		Player ps = GetComponent<Player>();
		if (!ps.isPlayer)
		{
			ps.input.Left = ps.input.LeftBuffer;
			ps.input.Right = ps.input.RightBuffer;
			ps.input.Forward = ps.input.ForwardBuffer;
			ps.input.Back = ps.input.BackBuffer;
			ps.input.Jump = ps.input.JumpBuffer;

			ps.input.LeftBuffer = false;
			ps.input.RightBuffer = false;
			ps.input.ForwardBuffer = false;
			ps.input.BackBuffer = false;
			if (actions.DiscreteActions[(int)PlayerAction.Strafe] == 0)
				ps.input.LeftBuffer = true;
			else if (actions.DiscreteActions[(int)PlayerAction.Strafe] == 2)
				ps.input.RightBuffer = true;
			if (actions.DiscreteActions[(int)PlayerAction.Forward] == 0)
				ps.input.BackBuffer = true;
			else if (actions.DiscreteActions[(int)PlayerAction.Forward] == 2)
				ps.input.ForwardBuffer = true;
			ps.input.JumpBuffer = actions.DiscreteActions[(int)PlayerAction.Jump] == 1;
			ps.input.Primary_Fire = actions.DiscreteActions[(int)PlayerAction.SpecialAction] == (int)SpecialAction.Fire;

			ps.input.rotChange = GetRot(actions.DiscreteActions[(int)PlayerAction.RotateHorizontal]);
			ps.input.upDownChange = GetRot(actions.DiscreteActions[(int)PlayerAction.RotateVertical]);
			currentRotIndex = actions.DiscreteActions[(int)PlayerAction.RotateHorizontal];
			currentUpDownRotIndex = actions.DiscreteActions[(int)PlayerAction.RotateVertical];
			ps.input.SwitchToSlot = 0;
			if (actions.DiscreteActions[(int)PlayerAction.SpecialAction] == (int)SpecialAction.SwitchWeapon)
				ps.input.SwitchToSlot = 1 + ps.NextWeaponNO_MELEE();

			ps.input.ClassAbility = actions.DiscreteActions[(int)PlayerAction.SpecialAction] == (int)SpecialAction.ClassAbility;
		}
	}

	public override void Heuristic(in ActionBuffers actionsOut)
	{
		// base.Heuristic(actionsOut);
	}

	public float GetRot(int action)
	{
		if (action == rots.Length)
			return 0;
		int mult = action < rots.Length ? -1 : 1;
		int rotVal;
		if (action < rots.Length)
			rotVal = rots.Length - action - 1;
		else
			rotVal = action - rots.Length - 1;
		return mult * rots[rotVal];
	}

	public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
	{
		base.WriteDiscreteActionMask(actionMask);
		Player player = GetComponent<Player>();
		if (!player.isGrounded && player.usedDoubleJump)
			actionMask.SetActionEnabled((int)PlayerAction.Jump, 1, false);
		if (player.Equipped() == null || !player.Equipped().CanFire())
			actionMask.SetActionEnabled((int)PlayerAction.SpecialAction, (int)SpecialAction.Fire, false);
		if (player.Equipped() == null)
		{
			actionMask.SetActionEnabled((int)PlayerAction.SpecialAction, (int)SpecialAction.SwitchWeapon, false);
			// actionMask.SetActionEnabled((int)PlayerAction.SwitchWeapon, (player.nextEquipped + 1) % 3, false);
			// actionMask.SetActionEnabled((int)PlayerAction.SwitchWeapon, (player.nextEquipped + 2) % 3, false);
		}
		else
		{
			if (player.merc == Merc.Soldier)
				actionMask.SetActionEnabled((int)PlayerAction.SpecialAction, (int)SpecialAction.SwitchWeapon, false); // No secondary for soldier as they use gunboats
		}
		if (player.merc == Merc.Demoman)
		{
			bool foundValidStickyToDet = false;
			foreach (Projectile p in player.game.projectiles)
			{
				if (p.type == ProjectileType.Sticky && p.owner == player && p.timeAlive >= PhysicsProjectile.STICKY_ARM_TIME)
				{
					foundValidStickyToDet = true;
					break;
				}
			}
			if (!foundValidStickyToDet)
				actionMask.SetActionEnabled((int)PlayerAction.SpecialAction, (int)SpecialAction.ClassAbility, false);
		}
		else if (player.merc == Merc.Medic)
		{
			if (((Medigun)player.weapons[1]).ubercharge < 100)
				actionMask.SetActionEnabled((int)PlayerAction.SpecialAction, (int)SpecialAction.ClassAbility, false);
		}
		else
		{
			actionMask.SetActionEnabled((int)PlayerAction.SpecialAction, (int)SpecialAction.ClassAbility, false);
		}

		RotActionMask((int)PlayerAction.RotateHorizontal, actionMask);
		RotActionMask((int)PlayerAction.RotateVertical, actionMask);
	}

	private void RotActionMask(int action, IDiscreteActionMask actionMask)
	{
		int prev = action == (int)PlayerAction.RotateHorizontal ? currentRotIndex : currentUpDownRotIndex;
		for (int i = 0; i < rots.Length * 2 + 1; i++)
		{
			if (Mathf.Abs(i - prev) > MAX_ROT_CHANGE)
				actionMask.SetActionEnabled(action, i, false);
		}
	}

	public void OnSpawn()
	{
		history = new List<MGEPerception>();
	}

	private MGEPerception History(int numBack)
	{
		return history[Mathf.Max(0, history.Count - 1 - numBack)];
	}

	public override void CollectObservations(VectorSensor sensor)
	{
		Player p = GetComponent<Player>();
		if (p.game is MGE && Base.gameType == GameType.SoldierScout)
		{
			int numBack = 5; // 30ms * 5
			if (history.Count > Mathf.Max(1, numBack)) // Ensure we store at least 1 so "memory" gets copied over
				history.RemoveAt(0);
			history.Add(new MGEPerception(history.Count > 0 ? history[history.Count - 1] : null, p));

			MGEPerception perc = History(numBack);
			perc.Observe(sensor, p);
		}
		else if (p.game is MGE)
		{
			Player opp = ((MGE)p.game).GetPlayer(1 - p.team);

			Player[] players = { p, opp };

			foreach (Player player in players)
			{
				sensor.Observe(p, "RelVel / GlobalPos / LookDir", Obs.RelVel(p, player.velocity) / Player.MAX_SPEED);
				Obs.GlobalPosObservation(sensor, p, null, player.Center());
				Vector3 lookDir = Obs.GlobalLook(p, player.lookDir * Vector3.forward);
				sensor.Observe(p, null, lookDir);
				sensor.Observe(p, "IsGrounded", player.isGrounded);
				sensor.Observe(p, "Health", player.health.health / (float)player.MaxHealth());
				sensor.Observe(p, "Ammo", player.weapons[0].state.ammo / (float)player.weapons[0].state.type.Ammo());
				sensor.Observe(p, "TotalAmmo", player.weapons[0].state.totalAmmo / (float)player.weapons[0].state.type.TotalAmmo()); // Arguably not needed
				sensor.Observe(p, "TimeSinceFire", Mathf.Clamp01(player.weapons[0].state.timeSinceFire / (float)player.weapons[0].state.type.FireTime()));
				sensor.Observe(p, "TimeSinceReload", Mathf.Clamp01(player.weapons[0].state.timeSinceReload / (float)player.weapons[0].state.type.ReloadTime(player.weapons[0].state.firstReload)));
			}

			// These aren't needed by the player since they'll be the same always
			sensor.Observe(p, null, Obs.RelPos(p, opp.Center()));
			Vector3 relOppLookDir = Obs.RelVel(p, opp.lookDir * Vector3.forward);
			sensor.Observe(p, null, relOppLookDir.x);
			sensor.Observe(p, null, relOppLookDir.z);


			// sensor.Observe(p, "RelVel / GlobalPos / LookDir", Obs.RelVel(p, p.velocity) / Player.MAX_SPEED);
			// Obs.GlobalPosObservation(sensor, p, null, p.Center(false));
			//Vector3 lookDir = Obs.GlobalLook(p, p.lookDir * Vector3.forward);
			// sensor.Observe(p, null, lookDir); // p.lookDir * Vector3.forward);

			// sensor.Observe(p, "IsGrounded", p.isGrounded);
			// sensor.Observe(p, "Health", p.health.health / (float)p.MaxHealth());
			// sensor.Observe(p, "Ammo", p.weapons[0].state.ammo / (float)p.weapons[0].state.type.Ammo());
			// sensor.Observe(p, "TotalAmmo", p.weapons[0].state.totalAmmo / (float)p.weapons[0].state.type.TotalAmmo()); // Arguably not needed
			// sensor.Observe(p, "TimeSinceFire", Mathf.Clamp01(p.weapons[0].state.timeSinceFire / (float)p.weapons[0].state.type.FireTime()));
			// sensor.Observe(p, "TimeSinceReload", Mathf.Clamp01(p.weapons[0].state.timeSinceReload / (float)p.weapons[0].state.type.ReloadTime(p.weapons[0].state.firstReload)));
			// CROUCH OBSERVATION eventually



			// sensor.Observe(p, "Opp RelVel / GlobalPos / RelPos / LookDir", Obs.RelVel(p, opp.velocity) / Player.MAX_SPEED);
			// Obs.GlobalPosObservation(sensor, p, null, p.Center(false)); // TODO: Should be opp.Center(false)
			// Vector3 oppLookDir = Obs.GlobalLook(p, opp.lookDir * Vector3.forward);
			// sensor.Observe(p, null, oppLookDir); // Obs.RelVel(p, opp.lookDir * Vector3.forward));

			// sensor.Observe(p, "IsGrounded", opp.isGrounded);
			// sensor.Observe(p, "Health", opp.health.health / (float)opp.MaxHealth());
			// CROUCH OBSERVATION eventually


			Projectile[] closest = new Projectile[2];
			foreach (Projectile proj in p.game.projectiles)
			{
				int index = proj.owner.team == p.team ? 0 : 1;
				if (closest[index] == null || Vector3.SqrMagnitude(proj.transform.position - p.Center()) < Vector3.SqrMagnitude(closest[index].transform.position - p.Center()))
					closest[index] = proj;
			}
			for (int index = 0; index < closest.Length; index++)
			{
				if (closest[index] != null)
				{
					sensor.Observe(p, "Projectile RelPos / RelVel", Obs.SignedSqrtMax(Obs.RelPos(p, closest[index].transform.position), 15.36f * Player.HAMMER_SCALER, 10.24f * Player.HAMMER_SCALER));
					sensor.Observe(p, null, Obs.RelVel(p, closest[index].vel) / ProjectileType.Rocket.FireSpeed());
				}
				else
				{
					for (int n = 0; n < 6; n++)
						sensor.AddObservation(0.0f);
				}
			}

			// ObserveNearbyMap.Observe(p);
			base.CollectObservations(sensor);
		}
		else if (p.game is Jump)
		{

		}

		/*if (Game.VERSION < 3)
		{
			ObservePreviousActions.Observe(sensor, p);
			ObserveNearbyMap.ObserveRaycasts(sensor, p);
			ObserveNearbyMap.ObserveNearbyItems(sensor, p);

			int numBack = 0; // Just use instant reaction time for now
			if (history.Count > Mathf.Max(1, numBack)) // Ensure we store at least 1 so "memory" gets copied over
				history.RemoveAt(0);
			history.Add(new GamePerception(history.Count > 0 ? history[history.Count - 1] : null, p));

			GamePerception perc = History(numBack);

			perc.Observe(sensor, p);
		}
		else
		{
			// For testing purposes, we observe a tiny subset:
			sensor.Observe(p, "RelVel / GlobalPos / LookDir", Obs.RelVel(p, p.velocity) / Player.MAX_SPEED);
			Obs.GlobalPosObservation(sensor, p, null, p.Center(false));
			Vector3 oppLookDir = Obs.GlobalLook(p, p.lookDir * Vector3.forward);
			sensor.Observe(p, null, oppLookDir);
			sensor.Observe(p, "Mid RelPos", Obs.SignedSqrtMaxNoHeight(Obs.RelPos(p, p.game.controlPoints[2].transform.position), 20.48f));
		}*/

		// for (int i = 0; i < 11; i++)
		//	sensor.Observe(p, "Test", 0.0f);

		Obs.Display(p);
	}

	private static float CorrectEulerX(float eulerX)
	{
		if (eulerX > 180f)
			eulerX -= 360f;
		return eulerX;
	}
}
