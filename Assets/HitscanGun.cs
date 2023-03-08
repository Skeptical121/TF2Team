using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.UI;

public abstract class HitscanGun : Weapon
{
	public float baseDamage;
	public DamageRampUpType damageRampUpType = DamageRampUpType.Default;

	protected class DamageInfo
	{
		public float total = 0;
		public HashSet<Vector3> hitPositions = new HashSet<Vector3>();
	}

	protected void FireShot(Vector3 dir, DamageInfo dmgInfo)
	{
		Player p = GetComponent<Player>();
		if (Physics.Raycast(p.CameraPos(), dir, out RaycastHit hit, 4000f, LayerHandler.OppHitscan(p.team)))
		{
			// Create effects:
			if (Base.self.renderStart)
			{
				GameObject hHit = (GameObject)Instantiate(Resources.Load("HitscanHit"));
				GameObject hLine = (GameObject)Instantiate(Resources.Load("HitscanLine"));

				hHit.transform.position = hit.point; // + hit.normal * 0.01f;
				hHit.transform.forward = hit.normal;
				hLine.GetComponent<HitscanLine>().Init(p.CameraPos() - new Vector3(0, 0.08f, 0), hit.point);

				// hLine.GetComponent<LineRenderer>().SetPosition(0, p.CameraPos() - new Vector3(0, 0.08f, 0));
				// hLine.GetComponent<LineRenderer>().SetPosition(1, hit.point);
				Destroy(hHit, 0.5f);
				Destroy(hLine, 0.5f);


				if (hit.collider.gameObject.layer == 0)
				{
					GameObject hDecal = (GameObject)Instantiate(Resources.Load("HitscanDecal"));
					hDecal.transform.forward = -hit.normal;
					hDecal.transform.position = hit.point + hit.normal * 0.00001f;
					Destroy(hDecal, 1.5f);
				}
			}
			if (hit.collider.gameObject.layer == LayerHandler.Hitbox(1 - p.team))
			{
				// Deal damage:
				Health health = GetHealth(hit.transform);
				// float damage = 6 * DamageRamp(DamageRampUpType.Scattergun, hit.distance);
				dmgInfo.total += health.DealDamage(new Damage(p, health.GetComponent<Player>(), p.CameraPos(), baseDamage, damageRampUpType)); // damage, 0.025f * damage * Vector3.Normalize(hit.point - p.CameraPos())));
				// p.game.stats.scatterGunDamage += damage;
				if (p.isPlayer)
					dmgInfo.hitPositions.Add(health.GetComponent<Player>().DamageNumberPos());

			}
			if (GetType() == typeof(Pistol))
			{
				GetComponent<Player>().game.stats.pistolShotsFired++;
				GetComponent<Player>().game.stats.pistolDamage += dmgInfo.total;
			}
		}
	}

	public Health GetHealth(Transform transform)
	{
		if (transform.GetComponent<Health>() != null)
			return transform.GetComponent<Health>();
		else
			return GetHealth(transform.parent);
	}
}
