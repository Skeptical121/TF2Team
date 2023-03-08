using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : Projectile
{

	public override void Tick()
	{
		int team = owner.GetComponent<Player>().team;
		int pl = LayerHandler.OppProjectile(team);

		if (type == ProjectileType.SyringeArrow)
		{
			pl = LayerHandler.BothProjectile();
			vel += Physics.gravity * Time.fixedDeltaTime;
		}

		float speed = vel.magnitude;
		Vector3 dir = vel / speed;
		if (Physics.Raycast(transform.position, dir, out RaycastHit hitInfo, speed * Time.fixedDeltaTime, pl))
		{
			transform.position = hitInfo.point + hitInfo.normal * 0.01f * Player.HAMMER_SCALER; // Rocket gets moved 1 unit out (https://www.reddit.com/r/truetf2/comments/ogqho6/how_does_rocket_jumping_work_from_a_technical/)
			GameObject direct = null;
			// Explode...
			if (hitInfo.transform.gameObject.layer == LayerHandler.Player(1 - team) || hitInfo.transform.gameObject.layer == LayerHandler.Player(team))
				direct = hitInfo.transform.gameObject;
			Explode(direct);
		}
		else
		{
			Collider[] directCheck = Physics.OverlapSphere(transform.position, 0, 1 << LayerHandler.Player(1 - team) | (type == ProjectileType.SyringeArrow ? 1 << LayerHandler.Player(team) : 0));
			if (directCheck != null && directCheck.Length > 0)
			{
				Explode(directCheck[0].gameObject);
			}
			else
			{
				transform.position += dir * speed * Time.fixedDeltaTime;
			}
		}

		LifeTimeCheck();
	}
}
