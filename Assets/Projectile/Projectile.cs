using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum ProjectileType
{
	Rocket,
	Grenade,
	Sticky,
	SyringeArrow
}

public static class ProjectileExtensions
{
	public static float SplashRange(this ProjectileType type, bool selfDamage)
	{
		switch (type)
		{
			case ProjectileType.Rocket: return selfDamage ? 1.21f * Player.HAMMER_SCALER : 1.46f * Player.HAMMER_SCALER;
			case ProjectileType.Grenade: return 1.46f * 0.85f * Player.HAMMER_SCALER; // iron bomber has a smaller explosion radius
			case ProjectileType.Sticky: return 1.46f * Player.HAMMER_SCALER;
			default: return 0; // No splash
		}
	}

	public static float BaseDamage(this ProjectileType type)
	{
		switch (type)
		{
			case ProjectileType.Rocket: return 90;
			case ProjectileType.Grenade: return 100;
			case ProjectileType.Sticky: return 120;
			case ProjectileType.SyringeArrow: return 50;
			default: return 0;
		}
	}

	public static DamageRampUpType GetDamageRampUpType(this ProjectileType type)
	{
		switch (type)
		{
			case ProjectileType.Rocket: return DamageRampUpType.Rocket;
			case ProjectileType.Grenade: return DamageRampUpType.None;
			case ProjectileType.Sticky: return DamageRampUpType.Sticky;
			case ProjectileType.SyringeArrow: return DamageRampUpType.SyringeArrow;
			default: return DamageRampUpType.Default;
		}
	}

	// Charge amount is for stickies, from 0 -> 1
	public static float FireSpeed(this ProjectileType type, float chargeAmount = 0)
	{
		switch (type)
		{
			case ProjectileType.Rocket: return 11f * Player.HAMMER_SCALER;
			case ProjectileType.Grenade: return 12.16f * Player.HAMMER_SCALER;
			case ProjectileType.Sticky: return Mathf.Lerp(8.05f, 8.05f * 2.3f, chargeAmount) * Player.HAMMER_SCALER; // These numbers may be wrong, alternative numbers on https://wiki.teamfortress.com/wiki/Projectiles are 9.2538f -> 24.092f
			case ProjectileType.SyringeArrow: return 24f * Player.HAMMER_SCALER;
			default: return 0;
		}
	}

	public static float LifeTime(this ProjectileType type)
	{
		switch (type)
		{
			case ProjectileType.Grenade: return 2.3f * 0.7f; // Iron bomber fuse time..
			default: return 300;
		}
	}
}

public abstract class Projectile : MonoBehaviour
{


	public ProjectileType type;
	public Player owner;
	public GameObject particles;
	public GameObject explosionParticles;
	public AudioClip explosionSound;
	public Vector3 vel;
	public float timeAlive;

	public void LifeTimeCheck()
	{
		timeAlive += Time.fixedDeltaTime;
		if (timeAlive >= type.LifeTime())
			Explode(null);
	}

	public virtual void Init(Player owner, float chargeAmount = 0)
	{
		transform.position = owner.CameraPos() + owner.lookDir * new Vector3(0, -0.03f, 0.235f) * Player.HAMMER_SCALER; // Original fires slightly below the crosshair
		vel = owner.lookDir * new Vector3(0, 0, type.FireSpeed(chargeAmount));
		this.owner = owner;
		if (Base.self.renderStart)
		{
			particles.SetActive(true);
			if (particles.transform.childCount > 0)
				particles.transform.GetChild(0).gameObject.SetActive(true);
		}
		owner.game.projectiles.Add(this);
		transform.rotation = owner.lookDir;
		timeAlive = 0;
	}


	/*private float SplashDist(Player obj)
	{
		Vector3 low = obj.Center() - obj.HalfExtents();
		Vector3 high = obj.Center() + obj.HalfExtents();
		Vector3 diff = Vector3.zero;
		if (transform.position.x < low.x)
			diff.x = transform.position.x - low.x;
		else if (transform.position.x > high.x)
			diff.x = transform.position.x - high.x;

		if (transform.position.y < low.y)
			diff.y = transform.position.y - low.y;
		else if (transform.position.y > high.y)
			diff.y = transform.position.y - high.y;

		if (transform.position.z < low.z)
			diff.z = transform.position.z - low.z;
		else if (transform.position.z > high.z)
			diff.z = transform.position.z - high.z;

		return diff.magnitude;
	}*/

	private void SplashCheck(Player check, ref bool hitOpp)
	{
		float splashDist = check.BoundingBox().MinDistance(transform.position); // SplashDist(check);
		Vector3 dirDiff = Vector3.Normalize(check.CameraPos() - transform.position);
		float dist = Vector3.Distance(transform.position, check.CameraPos());
		if (splashDist < type.SplashRange(check == owner) && !Physics.Raycast(transform.position, dirDiff, dist, 1 << 0) && !Physics.Raycast(check.CameraPos(), -dirDiff, dist, 1 << 0))
		{
			if (check.team != owner.team)
				hitOpp = true;
			// if (check == owner)
			// Seems like splash dist is done this way for both the owner & opponents: https://www.reddit.com/r/truetf2/comments/ogqho6/how_does_rocket_jumping_work_from_a_technical/
			splashDist = Mathf.Sqrt(Mathf.Min(Vector3.SqrMagnitude(transform.position - check.Center()), Vector3.SqrMagnitude(transform.position - check.CenterBottom())));
			check.GetComponent<Health>().DealDamage(new RocketDamage(this, check, splashDist, hitOpp));
		}
	}

	public void Explode(GameObject direct)
	{
		if (this is Rocket)
			owner.game.stats.rocketsExploded++;
		bool hitOpp = false;
		if (direct != null)
		{
			if (this is Rocket)
				owner.game.stats.directRockets++;
			hitOpp = true;
			if (type == ProjectileType.SyringeArrow && direct.GetComponent<Player>().team == owner.team)
				direct.GetComponent<Health>().ArrowHeal(owner, 2 * type.BaseDamage() * Weapon.DamageRamp(type.GetDamageRampUpType(), 
					Vector3.Distance(owner.transform.position, direct.transform.position))); // Crusader's crossbow is actually based on distance travelled unlike the other projectiles, but it's a small difference to do it from the player
			else
				direct.GetComponent<Health>().DealDamage(new RocketDamage(this, direct.GetComponent<Player>(), 0, hitOpp));
		}
		if (type.SplashRange(false) > 0)
		{
			foreach (Player opp in owner.game.AlivePlayers(1 - owner.team))
			{
				if (opp.gameObject != direct)
				{
					SplashCheck(opp, ref hitOpp);
					/*float splashDist = SplashDist(opp);
					Vector3 dirDiff = Vector3.Normalize(opp.Center(false) - transform.position);
					float dist = Vector3.Distance(transform.position, opp.Center(false));
					if (splashDist < type.SplashRange() && !Physics.Raycast(transform.position, dirDiff, dist, 1 << 0) && !Physics.Raycast(opp.Center(false), -dirDiff, dist, 1 << 0))
					{
						hitOpp = true;
						DealDamage(opp, splashDist, hitOpp);
					}*/
				}
			}
			SplashCheck(owner, ref hitOpp);
			/*float playerSplashDist = SplashDist(owner);
			Vector3 playerDirDiff = Vector3.Normalize(owner.Center(false) - transform.position);
			float playerDist = Vector3.Distance(transform.position, owner.Center(false));
			if (playerSplashDist < type.SplashRange() && !Physics.Raycast(transform.position, playerDirDiff, playerDist, 1 << 0) && !Physics.Raycast(owner.Center(false), -playerDirDiff, playerDist, 1 << 0))
				DealDamage(owner, playerSplashDist, hitOpp);*/
		}
		if (this is Rocket && hitOpp)
			owner.game.stats.rocketsHitOpp++;
		if (this is Rocket && owner.game is MGE mge)
			owner.game.stats.rocketDistToOpp += mge.GetPlayer(1 - owner.team).BoundingBox().MinDistance(transform.position);

		enabled = false;
		owner.game.projectiles.Remove(this);
		if (Base.self.renderStart)
		{
			particles.GetComponent<ParticleSystem>().Stop();
			gameObject.GetComponent<Renderer>().enabled = false;
			explosionParticles.SetActive(true);
			explosionParticles.transform.GetChild(0).gameObject.SetActive(true);
			SoundHandler.SpawnSound(transform, transform.position, explosionSound, 1.0f);
		}
		Destroy(gameObject, 2.5f);
	}

	public abstract void Tick();

	// splashDist == 0 if is direct
	/*public void DealDamage(Player oppPS, float splashDist, bool hitOpponent)
	{
		float dist = BoundingBox.MinDistance(owner.BoundingBox(), oppPS.BoundingBox()); // Distance is calculated based on the current distance
		float damage = type.BaseDamage() * Weapon.DamageRamp(type.GetDamageRampUpType(), dist);

		if (splashDist > 0)
		{
			damage *= 1.0f - 0.5f * splashDist / type.SplashRange();
		}

		float actualDamage = damage;
		if (owner == oppPS)
		{
			// Assume gunboats...
			if (hitOpponent)
			{
				if (!oppPS.isGrounded)
					actualDamage *= 0.6f; // * 0.6f for being in the air
			}
			else
			{
				actualDamage *= 0.6f * 0.4f; // * 0.6f for being in the air, which always applies for gun boats, then the 60% damage reduction for the gunboats
			}
		}
		Vector3 dirDiff = Vector3.Normalize(oppPS.Center(false) - transform.position); // Which way to knock the player
		oppPS.health.DealDamage(owner, actualDamage, dirDiff * damage * 0.06f * Player.HAMMER_SCALER);
		if (splashDist == 0)
			owner.game.stats.directRocketDamage += actualDamage;
		else if (owner == oppPS)
			owner.game.stats.rocketSelfDamage += actualDamage;
		else
			owner.game.stats.oppRocketDamage += actualDamage;

		if (Base.self.renderStart && owner.isPlayer && owner != oppPS)
			owner.health.PlayerDealtDamage(actualDamage, new List<Vector3> { oppPS.DamageNumberPos() });
	}*/
}