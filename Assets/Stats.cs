using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats
{
	public float scatterGunDamage = 0;
	public float pistolDamage = 0;
	public float directRocketDamage = 0;
	public float rocketSelfDamage = 0;
	public float oppRocketDamage = 0;
	public int[] deaths;
	public int[] numUbercharges;
	public float medicHealing;
	public float distanceTravelled;


	public int rocketsExploded; // Don't count rockets that don't hit anything by the time the round ends
	public int directRockets;
	public int rocketsHitOpp;
	public float rocketDistToOpp;


	public int scatterGunShotsFired;
	public int pistolShotsFired;

	public int numTicks;
	public float[] lookAtOppHorizontal;
	public float[] lookAtOppVertical;
	public float[] verticalRot;
	public float distToOpp;
	public float verticalDistToOpp;
	public float absVerticalDistToOpp;
	public float[] maxUpVelocity;
	public int[] grounded;
	public int[] healthKits;

	public int[,] jumps;
	public Vector3[] startPos; // Find out how far we go from start position

	public void Reset()
	{
		scatterGunDamage = 0;
		pistolDamage = 0;
		directRocketDamage = 0;
		rocketSelfDamage = 0;
		oppRocketDamage = 0;
		deaths = new int[2];
		numUbercharges = new int[2];
		medicHealing = 0;
		distanceTravelled = 0;

		rocketsExploded = 0;
		directRockets = 0;
		rocketsHitOpp = 0;
		rocketDistToOpp = 0;

		scatterGunShotsFired = 0;
		pistolShotsFired = 0;

		numTicks = 0;
		lookAtOppHorizontal = new float[2];
		lookAtOppVertical = new float[2];
		verticalRot = new float[2];
		distToOpp = 0;
		verticalDistToOpp = 0;
		absVerticalDistToOpp = 0;
		maxUpVelocity = new float[2];
		grounded = new int[2];
		healthKits = new int[2];

		jumps = new int[2,1]; // Set up for MGE
		startPos = new Vector3[2];
	}
}
