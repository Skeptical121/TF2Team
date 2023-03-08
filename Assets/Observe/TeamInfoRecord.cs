using Unity.MLAgents.Sensors;


public class TeamInfoRecord : Record
{
	public float ubercharge; // 0 -> 1

	public TeamInfoRecord(int team, Player obs) : base(obs.game.time)
	{
		foreach (Player player in obs.game.AlivePlayers(team))
		{
			if (player.merc == Merc.Medic)
			{
				ubercharge = ((Medigun)player.weapons[1]).ubercharge;
				break;
			}
		}
	}

	public void Observe(VectorSensor sensor, Player obs)
	{
		sensor.Observe(obs, "TeamUbercharge", ubercharge);
	}
}