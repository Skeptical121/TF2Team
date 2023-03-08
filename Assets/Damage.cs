using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// tf_debug_damage command in tf2!!!!!

public class Damage
{
	// From https://www.reddit.com/r/truetf2/comments/gtgdbx/some_damage_knockback_mechanics_multipliers_class/
	// This is useful too: https://developer.valvesoftware.com/wiki/List_of_TF2_console_commands_and_variables
	public const float DAMAGEFORCESCALE_OTHER = 0.06f * Player.HAMMER_SCALER;
	public const float DAMAGEFORCESCALE_SELF_SOLDIER_RJ = 0.10f * Player.HAMMER_SCALER;
	public const float DAMAGEFORCESCALE_SELF_SOLDIER_BADRJ = 0.05f * Player.HAMMER_SCALER;
	public const float DAMAGEFORCESCALE_DEFAULT = 0.09f * Player.HAMMER_SCALER;
	public const float KNOCKBACK_OFFSET = -0.10f * Player.HAMMER_SCALER; // TF2 pretends the direction of attacks comes from 10 Hu lower than they really do
	public const float CROUCH_KNOCKBACK_MULT = 82.0f / 62.0f;
	public const float CROUCH_SELF_DAMAGE_KNOCKBACK_MULT = 82.0f / 55.0f;

	public Player source;
	public Player receiver;
	Vector3 knockbackOrigin;
	public float actualDamageForKnockback;
	public float actualDamage;
	DamageRampUpType damageRampUpType;

	public float knockbackMult = DAMAGEFORCESCALE_DEFAULT;

	public Damage(Player source, Player receiver, Vector3 knockbackOrigin, float baseDamage, DamageRampUpType damageRampUpType)
	{
		this.source = source;
		this.receiver = receiver;
		this.knockbackOrigin = knockbackOrigin;
		actualDamage = baseDamage;
		actualDamageForKnockback = actualDamage;
		this.damageRampUpType = damageRampUpType;
	}

	// Self damage
	public Damage(Player player, float damage)
	{

	}

	public virtual void ApplyModifiers()
	{
		if (source != receiver)
		{
			float dist = BoundingBox.MinDistance(source.BoundingBox(), receiver.BoundingBox()); // Distance is calculated based on the current distance
			actualDamage *= Weapon.DamageRamp(damageRampUpType, dist);
			actualDamageForKnockback = actualDamage;
		}
	}

	public virtual void SetKnockback()
	{
		if (receiver.merc == Merc.Heavy)
			knockbackMult *= 0.5f;
		if (receiver.isCrouched)
			knockbackMult *= source == receiver ? CROUCH_SELF_DAMAGE_KNOCKBACK_MULT : CROUCH_KNOCKBACK_MULT;
	}

	public Vector3 GetKnockback()
	{
		// Debug.Log("Knockback mult = " + 100 * knockbackMult / Player.HAMMER_SCALER + ", damage = " + actualDamageForKnockback);
		// Apparantly knockback should be capped at 1000 velocity.. but I don't think that's even possible to reach (https://www.reddit.com/r/truetf2/comments/ogqho6/how_does_rocket_jumping_work_from_a_technical/)
		return knockbackMult * actualDamageForKnockback * Vector3.Normalize(receiver.Center() - (knockbackOrigin + new Vector3(0, KNOCKBACK_OFFSET, 0))); // Which way to knock the player
	}
}

public class RocketDamage : Damage
{
	private Projectile p;
	private float splashDist;
	private bool hitOpponent;
	public RocketDamage(Projectile p, Player hit, float splashDist, bool hitOpponent = false) : base(p.owner, hit, p.transform.position, p.type.BaseDamage(), p.type.GetDamageRampUpType())
	{
		this.p = p;
		this.splashDist = splashDist;
		this.hitOpponent = hitOpponent;
	}

	public override void SetKnockback()
	{
		if (source == receiver)
		{
			if (receiver.isGrounded)
				knockbackMult = DAMAGEFORCESCALE_SELF_SOLDIER_BADRJ;
			else
				knockbackMult = DAMAGEFORCESCALE_SELF_SOLDIER_RJ;
		}
		else
		{
			knockbackMult = DAMAGEFORCESCALE_OTHER;
		}
		base.SetKnockback();
	}

	public override void ApplyModifiers()
	{
		base.ApplyModifiers();
		if (splashDist > 0)
		{
			actualDamage *= 1.0f - 0.5f * Mathf.Clamp01(splashDist / p.type.SplashRange(source == receiver));
			actualDamageForKnockback = actualDamage;
		}

		if (source == receiver)
		{
			// Assume gunboats...
			if (!receiver.isGrounded)
			{
				actualDamage *= 0.6f;
				actualDamageForKnockback = actualDamage;
			}
			if (!hitOpponent)
			{
				// Actually this part doesn't get applied for gunboats still:
				// if (receiver.isGrounded)
				//	actualDamage *= 0.6f; // Still gets applied; but not for knockback.. because it uses the BadRJ thing when grounded
				actualDamage *= 0.4f; // Gunboats
			}

			p.owner.game.stats.rocketSelfDamage += actualDamage;
		}
		else
		{
			p.owner.game.stats.oppRocketDamage += actualDamage;
			if (splashDist == 0)
			{
				p.owner.game.stats.directRocketDamage += actualDamage;
			}
		}




		/*if (splashDist == 0)
			owner.game.stats.directRocketDamage += actualDamage;
		else if (owner == oppPS)
			owner.game.stats.rocketSelfDamage += actualDamage;
		else
			owner.game.stats.oppRocketDamage += actualDamage;*/

		if (Base.self.renderStart && source.isPlayer && source != receiver)
			source.health.PlayerDealtDamage(actualDamage, new List<Vector3> { receiver.DamageNumberPos() });
	}
}
