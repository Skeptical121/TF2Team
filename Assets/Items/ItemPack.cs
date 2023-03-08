using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPack : MonoBehaviour
{
	public enum Size
	{
		Small, Medium, Large
	}

	public bool healthKit;
	public Size size; // 0, 1 or 2 for small, medium and large respectively

	public float timeToSpawn = 0; // Items take 10 seconds to respawn, 0 seconds if it exists

	private void FixedUpdate()
	{
		if (timeToSpawn > 0)
		{
			timeToSpawn = Mathf.Max(0, timeToSpawn - Time.fixedDeltaTime);
			if (timeToSpawn == 0)
				GetComponent<MeshRenderer>().enabled = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (timeToSpawn == 0)
		{
			Player player = other.GetComponent<Player>();
			if (healthKit)
			{
				if (player.health.health < player.MaxHealth())
				{
					player.health.HealthChange(null, Mathf.Min(player.MaxHealth(), player.health.health + (size == Size.Small ? 0.2f : size == Size.Medium ? 0.5f : 1.0f) * player.MaxHealth()));
					if (player.game is MGE)
						player.game.stats.healthKits[player.team]++;
					timeToSpawn = 10;
					GetComponent<MeshRenderer>().enabled = false;
				}
			}
			else
			{
				bool gotAmmo = false;
				foreach (Weapon w in player.weapons)
				{
					if (w != null && w.state.totalAmmo < w.state.type.TotalAmmo())
					{
						gotAmmo = true;
						w.state.totalAmmo = Mathf.Min(w.state.type.TotalAmmo(), w.state.totalAmmo + (int)((size == Size.Small ? 0.2f : size == Size.Medium ? 0.5f : 1.0f) * w.state.type.TotalAmmo()));
					}
				}
				if (gotAmmo)
					timeToSpawn = 10;
			}
		}
	}
}
