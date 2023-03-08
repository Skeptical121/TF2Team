using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlPoint : MonoBehaviour
{
	public const int MAX_CP_CONTROL_STATE = 4;
	public const int MAX_CAPPERS = 8; // 6s has 6 people, but both scouts can cap 2x

	private const float DECAY_TIME = 90.0f;
	private static readonly float[] capRates = { 1, 1.5f, 1.83f, 2.08f, 2.28f, 2.45f, 2.59f, 2.72f, 2.83f, 2.93f, 3.02f, 3.10f, 3.18f, 3.25f, 3.32f, 3.38f };

	private TeamFight game;

	private int teamOwner; // -1 if not owned
	private bool locked;
	public float totalCapTime;

	public float capTime = 0; // Cap time is done negatively for blue so a neutral point works properly

	private int capPoint; // 0 = blue last, 1 = blue 2nd, 2 = mid, 3 = red 2nd, 4 = red last

	public int numBlueCappers = 0;
	public int numRedCappers = 0;


	public void Init(TeamFight game, int capPoint)
	{
		this.game = game;
		this.capPoint = capPoint;
		if (capPoint == 0 || capPoint == 1)
		{
			teamOwner = 0;
			Lock();
		}
		else if (capPoint == 3 || capPoint == 4)
		{
			teamOwner = 1;
			Lock();
		}
		else
		{
			teamOwner = -1;
		}
	}

	public int TeamOwner()
	{
		return teamOwner;
	}

	public float CapPercentage()
	{
		return Mathf.Abs(capTime / totalCapTime);
	}

	public int NumCappers(int team)
	{
		return team == 0 ? numBlueCappers : numRedCappers;
	}

	public int CapPoint()
	{
		return capPoint;
	}

	private void FixedUpdate()
	{
		if (!locked)
		{
			float oldCapTime = capTime;

			// Can't revert your own point any faster it seems like
			if (teamOwner != 0 && numBlueCappers > 0 && numRedCappers == 0)
			{
				capTime -= capRates[numBlueCappers] * Time.fixedDeltaTime;
			}
			else if (teamOwner != 1 && numRedCappers > 0 && numBlueCappers == 0)
			{
				capTime += capRates[numRedCappers] * Time.fixedDeltaTime;
			}
			else if ((capTime > 0 && numRedCappers == 0) || (capTime < 0 && numBlueCappers == 0))
			{
				if (capTime > 0)
					capTime = Mathf.Max(0, capTime - totalCapTime * Time.fixedDeltaTime / DECAY_TIME);
				else if (capTime < 0)
					capTime = Mathf.Min(0, capTime + totalCapTime * Time.fixedDeltaTime / DECAY_TIME);
			}
			// else the cap is frozen in time

			if (oldCapTime != capTime)
			{
				float reward = ((capTime - oldCapTime) / totalCapTime) * TeamFight.CAP_PERCENTAGE_REWARD;
				game.teams[0].agentGroup.AddGroupReward(-reward); // Blue wants the cap time to be more negative
				game.teams[1].agentGroup.AddGroupReward(reward); // Red wants the cap time to be more positive
			}

			if (capTime >= totalCapTime)
			{
				game.roundTimer = 0;
				capTime = 0;
				teamOwner = 1;
				if (capPoint > 0)
				{
					game.controlPoints[capPoint - 1].Unlock();
					game.defendingCP[0] = game.controlPoints[capPoint - 1];
					game.defendingCP[1] = game.controlPoints[capPoint];
				}
				if (capPoint < 4)
					game.controlPoints[capPoint + 1].Lock();
			}
			else if (capTime <= -totalCapTime)
			{
				game.roundTimer = 0;
				capTime = 0;
				teamOwner = 0;
				if (capPoint < 4)
				{
					game.controlPoints[capPoint + 1].Unlock();
					game.defendingCP[1] = game.controlPoints[capPoint + 1];
					game.defendingCP[0] = game.controlPoints[capPoint];
				}
				if (capPoint > 0)
					game.controlPoints[capPoint - 1].Lock();
			}
		}
		numBlueCappers = 0;
		numRedCappers = 0;
	}

	// Note the point might already be locked, but that's fine
	public void Lock()
	{
		capTime = 0;
		locked = true;
	}

	public void Unlock()
	{
		capTime = 0;
		locked = false;
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.GetComponent<Player>().team == 0)
			numBlueCappers += other.GetComponent<Player>().merc == Merc.Scout ? 2 : 1;
		else
			numRedCappers += other.GetComponent<Player>().merc == Merc.Scout ? 2 : 1;

		if (numBlueCappers > 10 || numRedCappers > 10)
		{
			Debug.LogError("Cappers went too high (" + numBlueCappers + ", " + numRedCappers + ")");
		}
	}


	/*private void OnTriggerExit(Collider other)
	{
		if (other.GetComponent<Player>().team == 0)
			numBlueCappers--;
		else
			numRedCappers--;

		if (numBlueCappers < 0 || numRedCappers < 0)
		{
			Debug.LogError("Cappers went negative (" + numBlueCappers + ", " + numRedCappers + ")");
		}
	}*/
}
