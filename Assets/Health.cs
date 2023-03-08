using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
	public float health;
	public float ubered;
	public bool isAlive;
	public float timeSinceTakenDamage; // Needed for crit heals

	public static Text healthPanel;
	public static Image healthFill;

	private List<AccumulatedDamage> accumulatedDamageThisTick = new List<AccumulatedDamage>();
	private Player player;

	public void OnSpawn()
	{
		isAlive = true;
		player = GetComponent<Player>();
		health = player.MaxHealth();
		timeSinceTakenDamage = 15.0f; // Enough for full crit heals on spawn
	}

	// Returns true if "building" (Health is < 142.5% of full health)
	public bool Heal(Player healer)
	{
		HealthChange(healer, Mathf.Min(player.MaxHealth() * 1.5f, health + Mathf.Lerp(24.0f, 72.0f, Mathf.InverseLerp(10, 15, timeSinceTakenDamage)) * Time.fixedDeltaTime));
		return health < player.MaxHealth() * 1.425f;
	}

	public void ArrowHeal(Player healer, float amount)
	{
		if (health < player.MaxHealth())
		{
			float healthHealed = health;
			HealthChange(healer, Mathf.Min(player.MaxHealth(), health + amount));
			healthHealed = health - healthHealed;
			Medigun medigun = (Medigun)healer.weapons[1];
			medigun.SetUbercharge(Mathf.Min(100, medigun.ubercharge + healthHealed / Mathf.Lerp(48.0f, 16.0f, Mathf.InverseLerp(10, 15, timeSinceTakenDamage))));
		}
	}

	public void Tick()
	{
		timeSinceTakenDamage += Time.fixedDeltaTime;
		if (Base.self.renderStart)
		{
			if (player.isPlayer)
			{
				for (int i = 0; i < accumulatedDamageThisTick.Count; i++)
				{
					GameObject damageIndicator = Instantiate((GameObject)Resources.Load("DamageIndicator"), GameObject.Find("Canvas").transform);
					damageIndicator.GetComponent<DamageIndicator>().player = player;
					damageIndicator.GetComponent<DamageIndicator>().worldDir = Quaternion.LookRotation(accumulatedDamageThisTick[i].dir);
					damageIndicator.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Lerp(10, 120, Mathf.InverseLerp(10, 60, accumulatedDamageThisTick[i].damage)), 100);
				}
			}
			accumulatedDamageThisTick.Clear();
		}


		if (health > player.MaxHealth()) // Overheal decay
			HealthChange(null, Mathf.Max(player.MaxHealth(), health - Time.fixedDeltaTime * player.MaxHealth() * 0.5f / 15.0f)); // It's somewhat tempting to make it so the medic bears the penatly of losing overheal
		else if (player.merc == Merc.Medic) // Medic self heal
			HealthChange(null, Mathf.Min(player.MaxHealth(), health + Mathf.Lerp(3, 6, Mathf.InverseLerp(0, 10, timeSinceTakenDamage)) * Time.fixedDeltaTime));

		// Uber decay:
		ubered = Mathf.Max(0, ubered - Time.fixedDeltaTime);

		/*if (player.game is MGE)
		{
			float distFromCenter = Mathf.Max(Mathf.Abs(player.CenterBottom().x), Mathf.Abs(player.CenterBottom().z));
			if (distFromCenter > 10.5f)
				DealDamage(new Damage(player, player, player.CameraPos(), distFromCenter > 20.5f ? 4.5f : distFromCenter > 15.5f ? 1.5f : 0.5f, DamageRampUpType.None));
		}*/
	}

	// Health is less valuable the more you have of it, this is based on the dota 2 health formula, but we use ^2 instead ^4 since it makes better numbers for overhealing
	private static float HealthFunction(float health)
	{
		return (health + 1 - (1 - health) * (1 - health)) / 2.0f;
	}

	public void HealthChange(Player dealer, float newHealth)
	{
		newHealth = Mathf.Max(0, Mathf.Round(newHealth)); // Damage in tf2 is rounded
		float actualDelta = newHealth - health;
		float delta = HealthFunction(newHealth / player.MaxHealth()) - HealthFunction(health / player.MaxHealth());
		health = newHealth;
		float reward = TeamFight.DAMAGE_REWARD * delta;
		if (dealer == null || dealer == player || actualDelta <= 0)
		{
			// Self damage, self healing
			player.agent.AddReward(reward);
			if (player.game is MGE mge)
				mge.GetPlayer(1 - player.team).agent.AddReward(-reward); // 0 sum rewards for the players in MGE
			// player.game.teams[1 - player.team].agentGroup.AddGroupReward(-reward);
		}
		else if (dealer.team == player.team)
		{
			player.game.stats.medicHealing += actualDelta;
			// Healing from another player
			player.agent.AddReward(0.25f * reward);
			dealer.agent.AddReward(0.75f * reward); // Most of the reward goes to the healer
			// player.game.teams[1 - player.team].agentGroup.AddGroupReward(-reward);
		}
		else
		{
			player.agent.AddReward(reward);
			dealer.agent.AddReward(-reward); // Damage dealer gets full reward (delta is negative here, remember)
		}
	}

	public float DealDamage(Damage damage)
	{
		if (ubered == 0)
		{
			damage.ApplyModifiers();
			damage.SetKnockback();
			timeSinceTakenDamage = 0;
			HealthChange(damage.source, health - damage.actualDamage);
			if (health <= 0)
				Kill(damage.source);
		}
		else
		{
			damage.SetKnockback();
			damage.actualDamage = 0;
		}

		Vector3 knockback = damage.GetKnockback();

		// Rigidbody rb = GetComponent<Rigidbody>();
		float max = 35 * Player.HAMMER_SCALER; // This also happens to be the terminal velocity
		player.velocity = new Vector3(Mathf.Clamp(player.velocity.x + knockback.x, -max, max), Mathf.Clamp(player.velocity.y + knockback.y, -max, max), Mathf.Clamp(player.velocity.z + knockback.z, -max, max));

		if (Base.self.renderStart)
		{
			if (accumulatedDamageThisTick.Count == 0)
				SoundHandler.SpawnSound(transform, transform.position, player.merc.PainSound());

			bool foundDupe = false;
			for (int i = 0; i < accumulatedDamageThisTick.Count; i++)
			{
				if (Vector3.Angle(accumulatedDamageThisTick[i].dir, -knockback) < 3.0f)
				{
					accumulatedDamageThisTick[i].damage += damage.actualDamage;
					foundDupe = true;
				}
			}
			if (!foundDupe)
				accumulatedDamageThisTick.Add(new AccumulatedDamage { damage = damage.actualDamage, dir = -knockback });
		}

		return damage.actualDamage;
	}

	public void Kill(Player dealer)
	{
		if (isAlive)
		{
			/*float reward = Game.MERC_DEATH_REWARD[(int)player.merc];
			if (player.merc == Merc.Medic)
			{
				reward += ((Medigun)player.weapons[1]).ubercharge / 100.0f * Game.UBER_REWARD;
			}
			player.agent.AddReward(-reward);
			if (dealer != null && dealer.team != player.team)
			{
				dealer.agent.AddReward(reward);
			}
			else
			{
				player.game.teams[1 - player.team].agentGroup.AddGroupReward(reward);
			}*/
		}
		isAlive = false;
	}

	public void PlayerDealtDamage(float damage, IEnumerable<Vector3> hitPositions)
	{
		// Play hitsound:
		AudioSource ac = SoundHandler.SpawnSound(player.transform, player.transform.position, SoundType.Hitsound);
		ac.pitch = Mathf.Lerp(140 / 100.0f, 30 / 100.0f, Mathf.InverseLerp(10, 150, damage));

		foreach (Vector3 pos in hitPositions)
		{
			GameObject dn = (GameObject)Instantiate(Resources.Load("DamageNumber"), GameObject.Find("Canvas").transform);
			dn.GetComponent<Text>().text = Mathf.RoundToInt(damage).ToString();
			dn.GetComponent<DamageNumber>().worldPosition = pos;
			dn.GetComponent<DamageNumber>().SetPos();
			Destroy(dn, 3.0f);
		}
	}
}

public class AccumulatedDamage
{
	public float damage;
	public Vector3 dir;
}
