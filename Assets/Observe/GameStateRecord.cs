using Unity.Mathematics;
using Unity.MLAgents.Sensors;

public class Record
{
	public float gameTime;
	public Record(float gameTime)
	{
		this.gameTime = gameTime;
	}
}

public class GameStateRecord : Record
{
	
	public float roundTimer;
	public int cpControlState;
	public float2 capPercentage;
	public int2 numCappers;

	public GameStateRecord(TeamFight game, Player obs) : base(obs.game.time)
	{
		roundTimer = game.roundTimer;
		cpControlState = game.GetCPControlState(obs.team);
		capPercentage.x = game.GetCapPercentage(obs.team, true);
		capPercentage.y = game.GetCapPercentage(obs.team, false);
		numCappers.x = game.GetNumCappers(obs.team, true);
		numCappers.y = game.GetNumCappers(obs.team, false);
	}

	public void Observe(VectorSensor sensor, Player obs)
	{
		sensor.Observe(obs, "RoundTimer", roundTimer / TeamFight.MAX_ROUND_TIMER);
		sensor.Observe(obs, "CPControlState", cpControlState / (float)ControlPoint.MAX_CP_CONTROL_STATE);
		sensor.Observe(obs, "TeamCapPercentage", capPercentage.x);
		sensor.Observe(obs, "OppCapPercentage", capPercentage.y);
		sensor.Observe(obs, "TeamNumCappers", numCappers.x / (float)ControlPoint.MAX_CAPPERS);
		sensor.Observe(obs, "OppNumCappers", numCappers.y / (float)ControlPoint.MAX_CAPPERS);
	}
}