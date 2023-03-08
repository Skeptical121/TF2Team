using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillTrigger : MonoBehaviour
{
	private void OnTriggerStay(Collider other)
	{
		Player player = other.GetComponent<Player>();
		player.health.DealDamage(new Damage(player, player, player.CameraPos(), 500.0f, DamageRampUpType.None));
		player.velocity = Vector3.zero;
	}
}
