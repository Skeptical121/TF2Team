using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PlayerSuperAgent : Agent
{
    Vector3 goalLoc;

    public override void OnActionReceived(ActionBuffers actions)
    {
    }

    public override void CollectObservations(VectorSensor sensor)
    {
    }
}
