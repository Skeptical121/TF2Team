using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsProjectile : Projectile
{
	public const float STICKY_ARM_TIME = 0.7f;

	private bool grenadeHitSomething = false;

	public override void Init(Player owner, float chargeAmount = 0)
	{
		base.Init(owner, chargeAmount);
		GetComponent<Rigidbody>().velocity = vel;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (type != ProjectileType.Sticky)
		{
			if (collision.gameObject.layer == LayerHandler.Player(1 - owner.team))
			{
				if (!grenadeHitSomething)
					Explode(collision.gameObject);
			}
			else if (type == ProjectileType.SyringeArrow)
			{
				Explode(null);
			}
			else if (type == ProjectileType.Grenade)
			{
				grenadeHitSomething = true;
			}
		}
	}

	public override void Tick()
	{
		LifeTimeCheck();
	}
}
