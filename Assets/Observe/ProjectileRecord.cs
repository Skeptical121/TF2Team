

using Unity.MLAgents.Sensors;
using UnityEngine;

public class ProjectileRecord : Record
{
	public int team;
	public Vector3 globalPos;
	public Vector3 vel;
	public ProjectileType type;
	public float timeAlive;

	public ProjectileRecord(Projectile proj, Player obs) : base(obs.game.time)
	{
		team = proj.owner.team;
		globalPos = proj.transform.position;
		vel = proj.vel;
		type = proj.type;
		timeAlive = proj.timeAlive;
	}

	public void Observe(VectorSensor sensor, Player obs)
	{
		// Relative position (x, y, z, dist?)
		// Relative velocity (x, y, z, magnitude?) (relative to our rotation, not to our velocity)
		// Type [Rocket, Sticky, Pipe]
		// Time alive (for sticky det time)

		Obs.ObserveString(obs, "Projectile [" + (team == 0 ? "Blue" : "Red") + "]");

		sensor.Observe(obs, "RelPos / RelVel / TimeAlive", Obs.SignedSqrtMax(Obs.RelPos(obs, globalPos), 15.36f * Player.HAMMER_SCALER, 10.24f * Player.HAMMER_SCALER));
		sensor.Observe(obs, null, Obs.RelVel(obs, vel) / ProjectileType.SyringeArrow.FireSpeed()); // Make all the projectiles have a standardized velocity calculation, no matter the type, and syringe arrows are the fastest type
		sensor.Observe(obs, null, Mathf.Clamp01(timeAlive / 5.0f));
		sensor.AddOneHotObservation((int)type, 4);

		// TODO: It'd be nice to know the position at which the projectile will hit a wall / the ground
	}

	public static void FakeObserve(VectorSensor sensor, Player obs)
	{
		Obs.ObserveString(obs, "Fake observe [11]");
		for (int i = 0; i < 11; i++)
			sensor.AddObservation(0.0f);
	}
}