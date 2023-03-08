using Unity.MLAgents.Sensors;

public class ObservePreviousActions
{
	// I think we could add to this...
	public static void Observe(VectorSensor sensor, Player obs)
	{
		sensor.Observe(obs, "JumpBuffer", obs.input.JumpBuffer);

		int right = (obs.input.LeftBuffer ? -1 : 0) + (obs.input.RightBuffer ? 1 : 0);
		int forward = (obs.input.BackBuffer ? -1 : 0) + (obs.input.ForwardBuffer ? 1 : 0);
		sensor.Observe(obs, "RightBuffer", right);
		sensor.Observe(obs, "ForwardBuffer", forward);
	}
}