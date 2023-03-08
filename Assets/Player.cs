using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public enum Merc
{
	Scout,
	Soldier,
	Pyro,
	Demoman,
	Heavy,
	Engineer,
	Medic,
	Sniper,
	Spy
}

public static class MercExtensions
{
	// Only 4 classes in the sixesID setup
	public static int SixesID(this Merc merc)
	{
		if (merc == Merc.Scout)
			return 0;
		else if (merc == Merc.Soldier)
			return 1;
		else if (merc == Merc.Demoman)
			return 2;
		else
			return 3;
	}

	public static float MaxHealth(this Merc merc)
	{
		switch (merc)
		{
			case Merc.Scout: return 125;
			case Merc.Soldier: return 200;
			case Merc.Pyro: return 175;
			case Merc.Demoman: return 175;
			case Merc.Heavy: return 300;
			case Merc.Engineer: return 125;
			case Merc.Medic: return 150;
			case Merc.Sniper: return 125;
			case Merc.Spy: return 125;
			default: return 0;
		}
	}

	public static float MoveSpeed(this Merc merc)
	{
		switch (merc)
		{
			case Merc.Scout: return 4.00f * Player.HAMMER_SCALER;
			case Merc.Soldier: return 2.40f * Player.HAMMER_SCALER;
			case Merc.Pyro: return 3.00f * Player.HAMMER_SCALER;
			case Merc.Demoman: return 2.80f * Player.HAMMER_SCALER;
			case Merc.Heavy: return 2.30f * Player.HAMMER_SCALER;
			case Merc.Engineer: return 3.00f * Player.HAMMER_SCALER;
			case Merc.Medic: return 3.20f * Player.HAMMER_SCALER;
			case Merc.Sniper: return 3.00f * Player.HAMMER_SCALER;
			case Merc.Spy: return 3.20f * Player.HAMMER_SCALER;
			default: return 0;
		}
	}

	public static float CameraHeight(this Merc merc)
	{
		switch (merc)
		{
			case Merc.Scout: return 0.65f * Player.HAMMER_SCALER;
			case Merc.Soldier:
			case Merc.Pyro: 
			case Merc.Demoman: 
			case Merc.Engineer: return 0.68f * Player.HAMMER_SCALER;
			case Merc.Heavy:
			case Merc.Medic:
			case Merc.Sniper:
			case Merc.Spy: return 0.75f * Player.HAMMER_SCALER;
			default: return 0;
		}
	}

	public static SoundType PainSound(this Merc merc)
	{
		switch (merc)
		{
			case Merc.Scout: return SoundType.PainScout;
			case Merc.Soldier: return SoundType.PainSoldier;
			case Merc.Demoman: return SoundType.PainDemoman;
			case Merc.Medic: return SoundType.PainMedic;
			default: return 0;
		}
	}
}

public class Player : MonoBehaviour
{
	public const float HAMMER_SCALER = 2.2f;
	public const float SKIN = 0.005f * HAMMER_SCALER;


	public const float MAX_BUNNY_HOP = 1.1f; // Severely limit bunny hopping (tf2 does 1.2f?)
	public const float AIR_SPEED_LIMIT = 0.3f * HAMMER_SCALER; // Demoknight apparantly has a higher value for this which is 750!!!
	public const float GRAVITY = 8 * HAMMER_SCALER; // Yes- this is correct, gravity is SIGNIFICANTLY higher than earth's gravity
	public const float MAX_SPEED = 4.00f * HAMMER_SCALER;
	private const float CROUCH_DIST = 0.2f * HAMMER_SCALER;
	public const float DEFAULT_HEIGHT = 0.82f * HAMMER_SCALER;
	public const float STEP_HEIGHT = 0.16f * HAMMER_SCALER;
	public const float JUMP_VELOCITY = 2.89f * HAMMER_SCALER;
	public const float DOUBLE_JUMP_VELOCITY = 2.65f * HAMMER_SCALER;
	public const float TERMINAL_VELOCITY = -35 * HAMMER_SCALER;
	public const int WEAPON_SLOTS = 3;
	public const float MAX_RESPAWN_TIMER = 30.0f;

	public const float MAX_GROUND_SPEED = 5.20f * HAMMER_SCALER;


	public Quaternion lookDir;
	public Quaternion playerDir;
	public float upDownRotation; // Degrees
	public bool usedDoubleJump; // Scout only
	public bool isCrouched;
	private float crouchState;
	private int numCrouchesInAir;
	private bool jumped; // For animation
	public bool isGrounded;


	public float respawnTimer; // if respawnTimer == 0, then they are alive... and you should be able to tell from other observations that are 0'd out that they are alive or not
	public float timeAlive;
	public int team; // 0 = blue, 1 = red
	public int playerID;
	public Merc merc;
	public int equipped;
	public int nextEquipped;
	public float switchTime;


	public InputInfo input;
	public bool isPlayer;
	public bool isSpectator;


	public Game game;
	public Weapon[] weapons;
	// public Rigidbody rb;
	public Vector3 velocity;
	public Animator anim;
	private BoxCollider c;
	// public RenderTexture nearbyMap;
	// public Texture2D nearbyMapEdit;
	public Transform lookTransform;
	public Health health;
	public PlayerAgent agent;

	// Render only:
	private float hitGroundAt;
	private float hitGroundVel;

	public Vector3 Center(bool step = false)
	{
		//if (local)
		//	return transform.TransformPoint(c.center) - game.transform.position;
		//else
		//{
		if (step)
			return transform.TransformPoint(c.center + Vector3.up * STEP_HEIGHT * 0.5f);
		else
			return transform.TransformPoint(c.center);
		//}
	}

	// The "origin"
	public Vector3 CenterBottom()
	{
		return transform.TransformPoint(c.center - new Vector3(0, HalfExtents().y, 0));
	}

	public Vector3 CameraPos()
	{
		return transform.position + new Vector3(0, merc.CameraHeight() - CROUCH_DIST * crouchState, 0);
	}

	public Vector3 DamageNumberPos()
	{
		return Center() + new Vector3(0, HalfExtents().y * 1.3f, 0);
	}

	public Vector3 HalfExtents(bool step = false)
	{
		if (step)
			return (c.size - new Vector3(0, STEP_HEIGHT, 0)) * 0.5f;
		else
			return c.size * 0.5f;
	}

	public BoundingBox BoundingBox()
	{
		return new BoundingBox(Center() - HalfExtents(), Center() + HalfExtents());
	}

	public float MaxHealth()
	{
		return merc.MaxHealth();
	}

	public Weapon Equipped()
	{
		if (equipped == nextEquipped)
			return weapons[equipped];
		else
			return null;
	}

	public void Init(Game game, int team, int playerID)
	{
		this.game = game;
		this.team = team;
		GetComponent<BehaviorParameters>().TeamId = team;
		this.playerID = playerID;
		lookDir = Quaternion.identity;
		upDownRotation = 0;
		playerDir = Quaternion.identity;
		usedDoubleJump = merc != Merc.Scout;
		lookTransform = transform.Find("Look");
		lookTransform.position = CameraPos();
		if (isPlayer || isSpectator)
		{
			GameObject.Find("Main Camera").transform.parent = lookTransform;
			GameObject.Find("Main Camera").transform.localPosition = Vector3.zero;
			GameObject.Find("Main Camera").transform.localRotation = Quaternion.identity;
			Transform ts1 = transform.Find("Model");
			foreach (Renderer r in ts1.GetComponentsInChildren<Renderer>())
				r.enabled = false;
		}
		// rb = GetComponent<Rigidbody>();
		anim = transform.Find("Model").GetComponent<Animator>();
		health = GetComponent<Health>();
		agent = GetComponent<PlayerAgent>();
		c = transform.GetComponent<BoxCollider>();
		// nearbyMapEdit = new Texture2D(ObserveNearbyMap.WIDTH, ObserveNearbyMap.LENGTH);
		// nearbyMap = new RenderTexture(ObserveNearbyMap.WIDTH, ObserveNearbyMap.LENGTH, 0);
		// if (Game.VERSION < 3)
		//	GetComponent<RenderTextureSensorComponent>().RenderTexture = nearbyMap;
	}

	public string TeamName()
	{
		return team == 0 ? "Blue" : "Red";
	}

	public void GoToSpawnLocation(bool randomSpawn)
	{
		// Try 1000 times to spawn in a "valid" location
		for (int i = 0; i < 1000; i++)
		{
			SpawnInfo spawnInfo = game.GetSpawnInfo(this, randomSpawn);
			transform.position = spawnInfo.position;
			lookDir = spawnInfo.rotation;
			playerDir = spawnInfo.rotation;
			velocity = spawnInfo.velocity;
			health.health = Mathf.RoundToInt((1.0f + spawnInfo.healthChangePercent) * health.health);
			if (!IsStuck())
				break;
		}
		Physics.SyncTransforms();
	}

	public bool IsStuck()
	{
		return Physics.CheckBox(Center(), HalfExtents(), Quaternion.identity, LayerHandler.PlayerMoveClip(team));
	}

	public void Spawn(bool roundReset, bool randomSpawn)
	{
		gameObject.SetActive(true);
		// game.teams[team].agentGroup.RegisterAgent(agent);

		//if (roundReset)
		//{
		//	lastDistanceToMid = -1;
		//}

		upDownRotation = 0;
		equipped = 0;
		nextEquipped = 0;
		switchTime = 0;
		respawnTimer = 0;
		timeAlive = 0;
		health.OnSpawn();
		agent.OnSpawn();
		input.Primary_Fire = false;
		input.ClassAbility = false;
		input.Jump = false;
		usedDoubleJump = merc != Merc.Scout;
		velocity = Vector3.zero;
		GoToSpawnLocation(randomSpawn); // Also sets velocity / health
		for (int i = 0; i < weapons.Length; i++)
		{
			if (weapons[i] != null)
				weapons[i].OnSpawn();
		}
	}

	public float MoveSpeed()
	{
		return merc.MoveSpeed();
	}

	private Vector3 GetMovementDir()
	{
		Vector3 movementDir = Vector3.zero;
		if (input.Forward)
			movementDir.z += 1;
		if (input.Back)
			movementDir.z -= 1;
		if (input.Left)
			movementDir.x -= 1;
		if (input.Right)
			movementDir.x += 1;
		if (movementDir.Equals(Vector3.zero))
			return Vector3.zero;
		return playerDir * movementDir.normalized;
	}

	private void Update()
	{
		if (isPlayer)
		{
			// Debug.Log("Mouse at time (" + Time.time * 1000 + "ms) " + playerDir.eulerAngles);
			input.SetPlayerInputs(this);
			Weapon.ammoPanel.text = weapons[equipped].state.ammo.ToString();
			Weapon.totalAmmoPanel.text = weapons[equipped].state.totalAmmo.ToString();
			Health.healthPanel.text = Mathf.RoundToInt(health.health).ToString();
			Health.healthFill.fillAmount = Mathf.Clamp01(health.health / MaxHealth());
			if (Input.GetKeyDown(KeyCode.O))
			{
				if (health.health < MaxHealth())
					health.HealthChange(null, Mathf.Min(MaxHealth(), health.health + MaxHealth()));
				foreach (Weapon w in weapons)
				{
					w.state.ammo = w.state.type.Ammo();
					w.state.totalAmmo = w.state.type.TotalAmmo();
				}
			}

			Transform mainCam = GetComponent<Player>().lookTransform.Find("Main Camera");
			Vector3 euler = mainCam.localEulerAngles;
			if (hitGroundVel >= 6.0f * HAMMER_SCALER && Time.time - hitGroundAt <= 0.5f)
			{
				float degrees = -13.0f * (hitGroundVel / (10.0f * HAMMER_SCALER)) * (1.0f - (Time.time - hitGroundAt) / 0.5f);
				mainCam.localEulerAngles = new Vector3(euler.x, euler.y, degrees);
			}
			else
			{
				mainCam.localEulerAngles = new Vector3(euler.x, euler.y, 0); // Ensure the screen is fully reset...
			}
		}

		UpdateAnim();
	}

	private void UpdateAnim()
	{
		Vector3 dir = Quaternion.Inverse(playerDir) * velocity;
		anim.SetFloat("X", dir.x / MoveSpeed());
		anim.SetFloat("Z", dir.z / MoveSpeed());
		anim.SetFloat("UpDown", -upDownRotation);
		transform.Find("Model").rotation = playerDir * Quaternion.Euler(-90, 0, 0);
	}


	public void Tick()
	{
		GetComponent<Health>().Tick();
		if (transform.localPosition.y < -100)
		{
			Debug.Log("Y pos too low, killing player!");
			health.DealDamage(new Damage(this, this, CameraPos(), 500.0f, DamageRampUpType.None));
			velocity = Vector3.zero;
		}

		// For now, don't reward getting closer to mid, random spawns should allow for this to be learned:

		/*float newDistance = Vector3.Distance(Center(false), game.controlPoints[2].transform.position);
		if (lastDistanceToMid != -1 && newDistance < lastDistanceToMid) // Only reward forward progress, don't penalize backwards progress
		{
			game.stats.distanceTravelled += (lastDistanceToMid - newDistance);
			agent.AddReward((lastDistanceToMid - newDistance) * TeamFight.MOVE_TO_CENTER_REWARD_PER_UNIT);
			game.stats.teams[1 - team].agentGroup.AddGroupReward((newDistance - lastDistanceToMid) * TeamFight.MOVE_TO_CENTER_REWARD_PER_UNIT);
		}
		if (lastDistanceToMid == -1 || newDistance < lastDistanceToMid)
		 	lastDistanceToMid = newDistance;*/


		timeAlive += Time.fixedDeltaTime;

		if (equipped != nextEquipped)
		{
			switchTime += Time.deltaTime;
			if (switchTime >= weapons[equipped].SwitchTime() + weapons[nextEquipped].SwitchTime()) // 0.5 seconds switch time
			{
				equipped = nextEquipped;
				weapons[equipped].OnSwitchTo();
			}
		}
		if (equipped == nextEquipped)
		{
			switchTime = 0;
			if (input.SwitchToSlot != 0 && input.SwitchToSlot - 1 != nextEquipped && weapons[input.SwitchToSlot - 1] != null)
				nextEquipped = input.SwitchToSlot - 1;
		}

		// InputInfo input = new InputInfo();
		if (isPlayer)
		{

		}
		else if (equipped == nextEquipped)
		{
			weapons[equipped].Tick(ref input); // Player does this after the mouse movement...

			playerDir *= Quaternion.Euler(0, input.rotChange * Time.fixedDeltaTime, 0);
			upDownRotation = Mathf.Clamp(upDownRotation + input.upDownChange * Time.fixedDeltaTime, -89.0f, 89.0f);
			lookDir = playerDir * Quaternion.Euler(upDownRotation, 0, 0);
		}

		for (int i = 0; i < weapons.Length; i++)
		{
			if (weapons[i] != null)
				weapons[i].PassiveTick();
		}

		UpdateCrouch();

		isGrounded = GroundedCheck();

		Vector3 actualMoveDir = GetMovementDir();

		// isGrounded = Physics.BoxCast(Center(false), HalfExtents() - new Vector3(0, SKIN, 0), Vector3.down, Quaternion.identity, SKIN * 2, LayerHandler.PlayerMoveClip(team));
		if (jumped && isGrounded)
		{
			jumped = false;
		}

		if (isGrounded && !isCrouched && input.Jump)
		{
			// Limit bunny hopping:
			float horizontalSpeedSqr = math.lengthsq(new float2(velocity.x, velocity.z));
			if (horizontalSpeedSqr > MoveSpeed() * MAX_BUNNY_HOP * MoveSpeed() * MAX_BUNNY_HOP)
				velocity *= MoveSpeed() * MAX_BUNNY_HOP / math.sqrt(horizontalSpeedSqr);
			velocity = new Vector3(velocity.x, JUMP_VELOCITY, velocity.z);
			if (crouchState == 0)
				velocity.y -= 0.5f * GRAVITY * Time.fixedDeltaTime; // Weird quirk where you lose half a tick of gravity if your not mid-crouch before jumping
			isGrounded = false;
			jumped = true;
			anim.SetTrigger("Jump");
			game.stats.jumps[team,playerID]++;
		}
		else if (!isGrounded && !usedDoubleJump && input.Jump)
		{
			usedDoubleJump = true;
			velocity = actualMoveDir * MoveSpeed() + Vector3.up * JUMP_VELOCITY;
		}
		
		if (!isGrounded)
		{
			// rb.velocity = new Vector3(rb.velocity.x, math.max(TERMINAL_VELOCITY, rb.velocity.y - GRAVITY * Time.fixedDeltaTime), rb.velocity.z);
			velocity.y -= GRAVITY * Time.fixedDeltaTime;
			if (velocity.y < TERMINAL_VELOCITY)
				velocity = new Vector3(velocity.x, TERMINAL_VELOCITY, velocity.z); // Terminal velocity of -3500
		}

		anim.SetBool("Grounded", isGrounded);
		anim.SetBool("Moving", Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z) > 0.005f * HAMMER_SCALER);

		if (isGrounded)
		{
			velocity = new Vector3(velocity.x, 0, velocity.z);
			usedDoubleJump = merc != Merc.Scout;
			numCrouchesInAir = 0;
			// rb.velocity.y = 0;
			float oldSpeed = math.length(velocity);
			float speed = math.max(0, oldSpeed - math.max(1, oldSpeed) * 4 * Time.fixedDeltaTime);
			if (oldSpeed > 0)
			{
				velocity *= speed / oldSpeed;
			}

			float currMoveSpeed = isCrouched ? MoveSpeed() / 3.0f : MoveSpeed();
			velocity += 10 * currMoveSpeed * Time.fixedDeltaTime * actualMoveDir;

			float currSpeed = math.length(velocity);
			if (currSpeed > currMoveSpeed)
				velocity *= currMoveSpeed / currSpeed;
		}
		else
		{
			if (!actualMoveDir.Equals(float3.zero))
			{
				// Air movement... which turns out is the same speed as ground movement
				float3 horizontalVelocity = new float3(velocity.x, 0, velocity.z);
				float dp = math.dot(horizontalVelocity, actualMoveDir);
				if (dp < AIR_SPEED_LIMIT - 10 * MoveSpeed() * Time.fixedDeltaTime)
					velocity += 10 * MoveSpeed() * Time.fixedDeltaTime * actualMoveDir;
				else if (dp < AIR_SPEED_LIMIT)
					velocity += (AIR_SPEED_LIMIT - dp) * actualMoveDir;
			}
		}
		Move();

		// UpdateCrouch();

		if (isPlayer && equipped == nextEquipped) // I think it makes sense that the click happens after the mouse movement for player input, but this makes the shot be after we modify our aim...
			weapons[equipped].Tick(ref input);

		// Can detonate stickies on any weapon
		if (input.ClassAbility && merc == Merc.Demoman)
			game.DetonateStickies(this);

		input.Jump = false; // Reset
		input.ClassAbility = false;
		input.Primary_Fire = false;
		UpdateAnim();

		lookTransform.rotation = lookDir;
		// Vector3 lookEuler = lookDir.eulerAngles;
		// lookTransform.eulerAngles = new Vector3(lookEuler.x, lookEuler.y, 90);

		if (isPlayer)
		{
			GameObject.Find("DebugInfo").GetComponent<TextMeshProUGUI>().text = 
				100 * velocity.x / HAMMER_SCALER + "\n" + 100 * velocity.y / HAMMER_SCALER + "\n" + 100 * velocity.z / HAMMER_SCALER + "\n" + 
				(lookDir.eulerAngles.x >= 180 ? lookDir.eulerAngles.x - 360 : lookDir.eulerAngles.x) + "\n" + lookDir.eulerAngles.y;
		}

		Physics.SyncTransforms();
	}

	public void Move()
	{
		Vector3 startVel = velocity;
		if (isGrounded)
			velocity.y = 0;
		float remainingTime = Time.fixedDeltaTime;
		for (int i = 0; i < 3; i++) // 3 solver iterations
		{
			Vector3 move = velocity * remainingTime;
			float dist = math.length(move);
			if (dist < 0.000001f)
				break;
			Vector3 dir = move / dist;

			// Try just 1 hit for now
			RaycastHit[] hits = Physics.BoxCastAll(Center(isGrounded), HalfExtents(isGrounded), dir, Quaternion.identity, dist + SKIN * 0.5f, LayerHandler.PlayerMoveClip(team));
			if (hits.Length > 0)
			{
				float smallestDist = dist + SKIN * 0.5f;
				for (int j = 0; j < hits.Length; j++)
				{
					if (math.dot(move, hits[j].normal) >= 0)
						continue;
					smallestDist = math.min(smallestDist, hits[j].distance);
					// if (!p.isGrounded || !IsGroundNormal(hits[j].SurfaceNormal))
					velocity -= (Vector3)math.project(velocity, hits[j].normal); // Remove all velocity in hit normal... if not ground
					if (isGrounded)
						velocity.y = 0;
				}
				transform.position += (smallestDist - SKIN * 0.5f) * dir;
				remainingTime -= ((smallestDist - SKIN * 0.5f) / dist) * remainingTime;
			}
			else
			{
				transform.position += move;
				remainingTime = 0;
			}
			if (remainingTime <= 0.001f)
				break;
		}
		if (isGrounded)
			GroundedSnap();

		bool wasGrounded = isGrounded;
		// Set grounded at end of tick:
		isGrounded = GroundedCheck();
		if (!wasGrounded && isGrounded)
		{
			// Fall damage / fall rotate screen effect
			hitGroundAt = Time.time;
			hitGroundVel = -startVel.y;
			if (startVel.y < -6.5f * HAMMER_SCALER)
				health.DealDamage(new Damage(this, this, CameraPos(), -0.05f * merc.MaxHealth() * (startVel.y / (3.0f * HAMMER_SCALER)), DamageRampUpType.None));
		}
	}

	private bool GroundedCheck()
	{
		// This is the velocity that'll "knock" you off the ground
		if (velocity.y >= 1.8f * HAMMER_SCALER)
			return false;
		if (// velocity.x * velocity.x + velocity.z * velocity.z < MAX_GROUND_SPEED * MAX_GROUND_SPEED && // velocity.y <= 0 &&
			Physics.BoxCast(Center(true), HalfExtents(true), Vector3.down, out RaycastHit hit, Quaternion.identity, STEP_HEIGHT + SKIN, LayerHandler.PlayerMoveClip(team)))
		{
			return hit.normal.y * Mathf.Abs(hit.normal.y) + 0.01f >= hit.normal.x * hit.normal.x + hit.normal.z * hit.normal.z; // Only grounded if standing on slope less than 46 degrees ish
		}
		return false;
	}

	private void GroundedSnap()
	{
		// Snap to ground if you are less than step height... or going down a ramp
		// if (physicsWorld.BoxCast(GetCenter(in p, in t, true), quaternion.identity, GetHalfExtents(in p, true), new float3(0, -1, 0), STEP_HEIGHT * 2 + COLLISION_THRESHOLD, 
		// 	out ColliderCastHit stepHitInfo, LayerLogic.GetCollidable(team)))

		//ColliderCastInput cci = new ColliderCastInput
		//{
		//	Collider = (Collider*)playerColliderBuffer.PlayerCollider(playerInfo, p.isCrouched, true).GetUnsafePtr(),
		//	Orientation = quaternion.identity,
		//	Start = t.Value,
		//	End = t.Value - new float3(0, STEP_HEIGHT * 2, 0)
		//};
		if (Physics.BoxCast(Center(true), HalfExtents(true), Vector3.down, out RaycastHit hit, Quaternion.identity, STEP_HEIGHT * 2, LayerHandler.PlayerMoveClip(team)))
		{
			float adjustment = -(hit.distance - STEP_HEIGHT);
			// Make sure there's no collider above you... if there is just don't snap I guess, but still stay grounded
			//cci.End = t.Value + new float3(0, adjustment, 0);
			if (adjustment > 0 && Physics.BoxCast(Center(true), HalfExtents(true), Vector3.up, out RaycastHit upHit, Quaternion.identity, adjustment + SKIN, LayerHandler.PlayerMoveClip(team)))
			// physicsWorld.BoxCast(GetCenter(in p, in t, true), quaternion.identity, GetHalfExtents(in p, true), new float3(0, 1, 0), adjustment, out ColliderCastHit upStepHitInfo, LayerLogic.GetCollidable(team)))
			{
			}
			else
			{
				transform.position += Vector3.up * (adjustment + SKIN * 0.5f);
			}
			// p.velocity.y = 0;
		}
		else
		{
			// SetGrounded(ref p, false);
		}
	}

	// For now, this doesn't include melee since very rarely is using melee the "correct" decision
	public int NextWeaponNO_MELEE()
	{
		int weapon = equipped;
		do
		{
			weapon = (weapon + 1) % 2; // Change to 3 if we want to include melee again
		} while (weapons[weapon] == null);
		return weapon;
	}

	private void UpdateCrouch()
	{
		if (!isCrouched && !isGrounded && numCrouchesInAir >= 2)
		{
			crouchState = 0;
		}
		else
		{
			if (isGrounded)
				crouchState = Mathf.Clamp01(crouchState + (input.Crouch ? 5 * Time.fixedDeltaTime : -5 * Time.fixedDeltaTime));
			else
				crouchState = input.Crouch ? 1 : 0;
			if (isGrounded && isCrouched && crouchState < 1 && Physics.BoxCast(Center(), HalfExtents() - new Vector3(0, SKIN, 0),
				Vector3.up, Quaternion.identity, CROUCH_DIST + SKIN * 2, LayerHandler.PlayerMoveClip(team))) // Can't uncrouch on the ground if there's something above
			{
				crouchState = 1;
			}
			else if (!isGrounded && isCrouched && crouchState < 1 && Physics.BoxCast(Center(), HalfExtents() * 0.5f - new Vector3(0, SKIN, 0),
				Vector3.down, Quaternion.identity, CROUCH_DIST + SKIN * 2, LayerHandler.PlayerMoveClip(team))) // Can't uncrouch in the air if there's something underneath
			{
				crouchState = 1;
			}

			lookTransform.position = CameraPos();

			if (!isCrouched && crouchState == 1)
				SwitchCrouchState();
			else if (isCrouched && crouchState == 0)
				SwitchCrouchState();
		}
	}

	private void SwitchCrouchState()
	{
		isCrouched = !isCrouched;
		if (isCrouched)
		{
			anim.SetBool("Crouch", true);
			c.size = new Vector3(c.size.x, DEFAULT_HEIGHT - CROUCH_DIST, c.size.z);
			c.center = new Vector3(c.center.x, (DEFAULT_HEIGHT - CROUCH_DIST) * 0.5f, c.center.z);
			if (!isGrounded)
			{
				numCrouchesInAir++;
				transform.position += new Vector3(0, CROUCH_DIST, 0);
				// transform.Find("Model").localPosition = new Vector3(0, -CROUCH_DIST, 0);
			}
		}
		else
		{
			anim.SetBool("Crouch", false);
			c.size = new Vector3(c.size.x, DEFAULT_HEIGHT, c.size.z);
			c.center = new Vector3(c.center.x, DEFAULT_HEIGHT * 0.5f, c.center.z);

			if (!isGrounded)
			{
				transform.position -= new Vector3(0, CROUCH_DIST, 0);
			}
			// transform.Find("Model").localPosition = Vector3.zero;
		}
		// BoxCollider sub = transform.Find("CollisionBlocker").GetComponent<BoxCollider>();
		// sub.size = c.size;
		// sub.center = c.center;
	}
}
