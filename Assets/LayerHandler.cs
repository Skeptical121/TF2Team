using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerHandler
{
	public const int SKYBOX = 12;
	private const int PLAYER_CLIP = 13;

    public static int Player(int team)
	{
		return 6 + team;
	}

	public static int Hitbox(int team)
	{
		return 10 + team;
	}

	public static int PlayerMoveClip(int team)
	{
		return (1 << 0) | (1 << PLAYER_CLIP) | (1 << SKYBOX) | (1 << Player(1 - team));
	}

	public static int OppHitscan(int team)
	{
		return (1 << 0) | (1 << SKYBOX) | (1 << Hitbox(1 - team));
	}

	public static int OppProjectile(int team)
	{
		return (1 << 0) | (1 << SKYBOX) | (1 << Player(1 - team));
	}

	public static int BothProjectile()
	{
		return (1 << 0) | (1 << SKYBOX) | (1 << Player(0)) | (1 << Player(1));
	}

	public static int Visual()
	{
		return (1 << 0) | (1 << SKYBOX); // Only default blocks vision
	}
}
