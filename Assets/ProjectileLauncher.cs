using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileLauncher : Weapon
{
	public GameObject projectilePrefabBlue;
	public GameObject projectilePrefabRed;

	public override void Fire()
	{
		base.Fire();

		GameObject projectile = Instantiate(GetComponent<Player>().team == 0 ? projectilePrefabBlue : projectilePrefabRed, transform.parent);
		projectile.GetComponent<Projectile>().Init(GetComponent<Player>());
	}
}
