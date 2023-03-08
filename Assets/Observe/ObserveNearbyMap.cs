using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;
// When people aren't fighting anyone, we should slow down their tickrate... thus in a stalemate we can save a lot of env steps


// Nearby map... 3 dimensional
// We can do nearby map in a grid and go through walls because the map is already known by the player
// Generally trying to avoid doing too many raycasts, but we can at the least do a few
public static class ObserveNearbyMap
{
	public const int WIDTH = 21; // Odd #
	public const int LENGTH = 21; // Odd #

	// Trying to capture nearby walls,
	// Elevation in the form of ledges to fall off / jump onto
	// Nearby healthpacks, ammo, capture points, resupply rooms
	// How high is the ceiling...


	// Elevation of 8x8 grid near you- includes walls... this is to avoid walking off ledges, walking into walls, knowing when to jump, should help with rocket jumping, sticky placement & combat as well
	// We avoid raycasts here by caching the entire map on a precise grid before starting

	/*public static void Observe(Player obs)
	{
		// Observe very close to what's behind you / beside you, and mostly infront


		// Nearby walls that we can move into, ledges we have to jump off / can fall off of
		// And then what's near our crosshair

		// RenderTexture.active = obs.nearbyMap;

		// obs.nearbyMapEdit.ReadPixels(new Rect(0, 0, obs.nearbyMap.width, obs.nearbyMap.height), 0, 0);
		float diameter = 0.64f * Player.HAMMER_SCALER;

		for (int rX = 0; rX < WIDTH; rX++)
		{
			for (int rY = 0; rY < LENGTH; rY++)
			{
				// int rX = x + 10;
				// int rY = z + 10;

				// Make it so we observe more of the terrain infront than behind, as that is more likely to be important
				float elevation = obs.game.map.GetLocalElevation(obs.Center(true) + obs.playerDir * new Vector3(rX - WIDTH / 2, 0, rY - LENGTH / 4) * diameter, diameter * 0.55f); // We have the circles overlap a little bit to try to miss less

				float scale = 0.5f + 0.5f * (elevation - obs.transform.localPosition.y) / obs.game.map.size.y;
				obs.nearbyMapEdit.SetPixel(rX, rY, new Color(scale, scale, scale));
			}
		}
		obs.nearbyMapEdit.Apply();
		if (obs.isPlayer)
			GameObject.Find("NearbyMap").GetComponent<RawImage>().texture = obs.nearbyMap;

		RenderTexture.active = obs.nearbyMap;
		Graphics.Blit(obs.nearbyMapEdit, obs.nearbyMap);
		RenderTexture.active = null;
	}*/

	// 6 observations
	public static void ObserveRaycasts(VectorSensor sensor, Player obs)
	{
		Vector3[] eulerOffset = { new Vector3(0, 0, 0), new Vector3(0, 7.5f, 0), new Vector3(0, -7.5f, 0), new Vector3(0, 5.0f, 0), new Vector3(0, -5.0f, 0) };

		float MAX_DIST = 20.48f * Player.HAMMER_SCALER;
		for (int i = 0; i < eulerOffset.Length; i++)
		{
			Vector3 normal = Vector3.zero;
			if (Physics.Raycast(obs.CameraPos(), obs.lookDir * Quaternion.Euler(eulerOffset[i]) * Vector3.forward, out RaycastHit hit, MAX_DIST, LayerHandler.Visual()))
			{
				sensor.Observe(obs, "RayDist", hit.distance / MAX_DIST);
				normal = hit.normal;
			}
			else
			{
				sensor.Observe(obs, "RayDist", 1.0f); // "max distance"
			}
			if (i == 0)
			{
				sensor.Observe(obs, "RayNormalY", normal.y);
			}
		}
	}

	// 12 observations
	public static void ObserveNearbyItems(VectorSensor sensor, Player obs)
	{
		// For stuff like healthpacks, shutters...
		// Closest healthpack, size of healthpack, how long since it was taken, direction to it
		GameObject closestHP = obs.game.ClosestPack(obs.Center(), true);
		//if (Game.VERSION >= 2)
			sensor.Observe(obs, "HP", closestHP == null ? new Vector2(0, 0) : Obs.SignedSqrtMaxNoHeight(Obs.RelPos(obs, closestHP.transform.position), 10.24f));
		//else
		//	sensor.Observe(obs, "HP", closestHP == null ? new Vector3(0, 0, 0) : Obs.SignedSqrtMax(Obs.RelPos(obs, closestHP.transform.position), 10.24f, 10.24f));
		sensor.Observe(obs, null, closestHP == null ? -1 : (int)closestHP.GetComponent<ItemPack>().size / 2.0f);
		sensor.Observe(obs, null, closestHP == null ? 1 : closestHP.GetComponent<ItemPack>().timeToSpawn / 10.0f);

		GameObject closestAmmo = obs.game.ClosestPack(obs.Center(), false);
		//if (Game.VERSION >= 2)
			sensor.Observe(obs, "Ammo", closestAmmo == null ? new Vector2(0, 0) : Obs.SignedSqrtMaxNoHeight(Obs.RelPos(obs, closestHP.transform.position), 10.24f));
		//else
		//	sensor.Observe(obs, "Ammo", closestAmmo == null ? new Vector3(0, 0, 0) : Obs.SignedSqrtMax(Obs.RelPos(obs, closestHP.transform.position), 10.24f, 10.24f));
		sensor.Observe(obs, null, closestAmmo == null ? -1 : (int)closestAmmo.GetComponent<ItemPack>().size / 2.0f);
		sensor.Observe(obs, null, closestAmmo == null ? 1 : closestAmmo.GetComponent<ItemPack>().timeToSpawn / 10.0f);

		//if (Game.VERSION >= 2)
		//{

		//ControlPoint defendingCP = obs.game.defendingCP[obs.team];
		//ControlPoint attackingCP = obs.game.defendingCP[1 - obs.team];
		//sensor.Observe(obs, "CP RelPos", Obs.SignedSqrtMaxNoHeight(Obs.RelPos(obs, defendingCP.transform.position), 20.48f));
		//sensor.Observe(obs, null, Obs.SignedSqrtMaxNoHeight(Obs.RelPos(obs, attackingCP.transform.position), 20.48f));

		//}
	}
}
