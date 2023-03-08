using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.UI;

public class Scattergun : HitscanGun
{
	float timeFiredAt;
	int degreesRand;

	private void Update()
	{
		if (Base.self.renderStart && GetComponent<Player>().isPlayer && timeFiredAt != 0)
		{
			Transform mainCam = GetComponent<Player>().lookTransform.Find("Main Camera");
			Vector3 euler = mainCam.localEulerAngles;
			float degrees;
			float HALF_TIME = 0.45f;
			if (Time.time - timeFiredAt <= HALF_TIME)
			{
				degrees = degreesRand * (-0.1f + 1.1f * (HALF_TIME - (Time.time - timeFiredAt)) / HALF_TIME);
			}
			else if (Time.time - timeFiredAt <= HALF_TIME * 2)
			{
				degrees = degreesRand * -0.1f * (HALF_TIME - (Time.time - timeFiredAt - HALF_TIME)) / HALF_TIME;
			}
			else
			{
				degrees = 0;
			}
			mainCam.localEulerAngles = new Vector3(-degrees, euler.y, euler.z);
		}
	}

	public override void Fire()
	{
		base.Fire();
		timeFiredAt = Time.time;
		degreesRand = Random.Range(2, 5); // Yes, this is supposed to be int random (for some reason tf2 makes it either 2, 3, or 4)
		Player p = GetComponent<Player>();
		Quaternion dir = p.lookDir;
		DamageInfo dmgInfo = new DamageInfo();
		FireShot(dir * Vector3.forward, dmgInfo);
		FireShot(dir * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(0, -1.95f, 0) * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(0, 1.95f, 0) * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(-1.95f, 0, 0) * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(1.95f, 0, 0) * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(1.65f, 1.65f, 0) * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(1.65f, -1.65f, 0) * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(-1.65f, 1.65f, 0) * Vector3.forward, dmgInfo);
		FireShot(dir * Quaternion.Euler(-1.65f, -1.65f, 0) * Vector3.forward, dmgInfo);
		if (p.isPlayer)
		{
			if (dmgInfo.total > 0)
			{
				p.health.PlayerDealtDamage(dmgInfo.total, dmgInfo.hitPositions);
			}
		}
		GetComponent<Player>().game.stats.scatterGunShotsFired++;
		GetComponent<Player>().game.stats.scatterGunDamage += dmgInfo.total;
		Academy.Instance.StatsRecorder.Add("Scout/AverageScattergunDamage", dmgInfo.total);
	}
}
