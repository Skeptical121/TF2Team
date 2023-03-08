using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class Pistol : HitscanGun
{
	public override void Fire()
	{
		base.Fire();
		Player p = GetComponent<Player>();
		Quaternion dir = p.lookDir;
		DamageInfo dmgInfo = new DamageInfo();
		Vector2 offset = Random.insideUnitCircle * 1.2f; // Pistol has just under 1.2 degrees spread.. TODO: First shot should be perfectly accurate
		FireShot(dir * Quaternion.Euler(offset.x, offset.y, 0) * Vector3.forward, dmgInfo);
		if (p.isPlayer)
		{
			if (dmgInfo.total > 0)
			{
				p.health.PlayerDealtDamage(dmgInfo.total, dmgInfo.hitPositions);
			}
		}
		Academy.Instance.StatsRecorder.Add("Scout/AveragePistolDamage", dmgInfo.total);
	}
}
