using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;


// https://cdn.openai.com/dota-2.pdf
public class Team
{
	public SimpleMultiAgentGroup agentGroup;

	public Team()
	{
		agentGroup = new SimpleMultiAgentGroup();
	}
}
