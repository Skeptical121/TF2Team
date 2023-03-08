using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medigun : Weapon
{
	public float ubercharge; // 0 -> 100
	private Player player;
	public Player healTarget;
	public bool ubered;
	public LineRenderer lr;

	public override void OnSpawn()
	{
		base.OnSpawn();
		ubercharge = 0;
		healTarget = null;
		player = GetComponent<Player>();
		lr = GetComponent<LineRenderer>();
	}

	public void SetUbercharge(float newUbercharge)
	{
		player.agent.AddReward((newUbercharge - ubercharge) / 100.0f * TeamFight.UBER_REWARD);
		// player.game.teams[1 - player.team].agentGroup.AddGroupReward((ubercharge - newUbercharge) / 100.0f * Game.UBER_REWARD);
		ubercharge = newUbercharge;
	}

	public override void Tick(ref InputInfo input)
	{
		if (ubercharge == 100 && !ubered && input.ClassAbility)
		{
			ubered = true;
			player.game.stats.numUbercharges[player.team]++;
		}

		if (input.Primary_Fire)
		{
			input.Primary_Fire = false;
			Fire();
		}

		if (healTarget != null)
		{
			float disconnectDist = 5.4f * Player.HAMMER_SCALER;
			if (Vector3.SqrMagnitude(healTarget.Center() - player.Center()) > disconnectDist * disconnectDist)
			{
				healTarget = null;
			}
			else
			{
				bool building = healTarget.health.Heal(player);
				if (!ubered)
				{
					SetUbercharge(Mathf.Min(100, ubercharge + (building ? 2.5f : 1.25f) * Time.fixedDeltaTime));
				}
				else
				{
					healTarget.health.ubered = 1.0f;
				}

				if (Base.self.renderStart)
				{
					lr.enabled = true;
					Vector3[] curve = new Vector3[] { transform.position + Vector3.up, transform.position + Vector3.up + player.lookDir * Vector3.forward * 3f, healTarget.transform.position + Vector3.up };
					lr.SetPosition(0, GetCurvePos(curve, 0));
					lr.SetPosition(1, GetCurvePos(curve, 0.25f));
					lr.SetPosition(2, GetCurvePos(curve, 0.5f));
					lr.SetPosition(3, GetCurvePos(curve, 0.75f));
					lr.SetPosition(4, GetCurvePos(curve, 1f));
				}
			}
		}
		if (ubered)
			player.health.ubered = 1.0f;
	}

	private Vector3 GetCurvePos(Vector3[] curve, float t)
	{
		return (1 - t) * (1 - t) * curve[0] + 2 * t * (1 - t) * curve[1] + t * t * curve[2];
	}

	public override void PassiveTick()
	{
		base.PassiveTick();
		if (ubered)
		{
			int numPlayersUbered = 1;
			foreach (Player p in player.game.AlivePlayers(player.team))
			{
				if (p.playerID != player.playerID && p.health.ubered > 0) // Assumes one medic
				{
					numPlayersUbered++;
				}
			}
			SetUbercharge(Mathf.Max(0, ubercharge - 6.25f * Mathf.Max(2, numPlayersUbered) * Time.fixedDeltaTime));
			if (ubercharge == 0)
			{
				ubered = false;
			}
		}
		if (Base.self.renderStart && healTarget == null)
			lr.enabled = false;
	}

	public override void Fire()
	{
		float closestAngle = 30;
		float connectDist = 4.5f * Player.HAMMER_SCALER;
		foreach (Player p in player.game.AlivePlayers(player.team))
		{
			if (p.playerID != player.playerID && Vector3.SqrMagnitude(p.Center() - player.CameraPos()) < connectDist * connectDist &&
				!Physics.Raycast(player.CameraPos(), Vector3.Normalize(p.Center() - player.CameraPos()), Vector3.Distance(player.CameraPos(), p.Center()), LayerHandler.Visual()))
			{
				float angle = Vector3.Angle(player.lookDir * Vector3.forward, Vector3.Normalize(p.Center() - player.CameraPos()));
				// Debug.Log(angle + ", " + player.lookDir * Vector3.forward + ", " + Vector3.Normalize(p.Center(false) - player.CameraPos()));
				if (angle < closestAngle)
				{
					closestAngle = angle;
					healTarget = p;
				}
			}
		}
	}
}
