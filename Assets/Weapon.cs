using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct WeaponState
{
	public int ammo;
	public int totalAmmo;
	public float timeSinceFire;
	public bool firstReload;
	public float timeSinceReload;

	public WeaponType type;
}

public enum WeaponType
{
	None,
	Scattergun,
	Pistol,
	RocketLauncher,
	GrenadeLauncher,
	StickyBombLauncher,
	CrusadersCrossbow,
	Medigun,
	Melee
}

public static class WeaponTypeExtensions
{
	public static int Ammo(this WeaponType type)
	{
		switch (type)
		{
			case WeaponType.Scattergun: return 6;
			case WeaponType.Pistol: return 12;
			case WeaponType.RocketLauncher: return 4;
			case WeaponType.GrenadeLauncher: return 4;
			case WeaponType.StickyBombLauncher: return 8;
			case WeaponType.CrusadersCrossbow: return 1;
			default: return 1;
		}
	}

	public static int TotalAmmo(this WeaponType type)
	{
		switch (type)
		{
			case WeaponType.Scattergun: return 32;
			case WeaponType.Pistol: return 36;
			case WeaponType.RocketLauncher: return 20;
			case WeaponType.GrenadeLauncher: return 16;
			case WeaponType.StickyBombLauncher: return 24;
			case WeaponType.CrusadersCrossbow: return 38;
			default: return 1;
		}
	}

	public static float FireTime(this WeaponType type)
	{
		switch (type)
		{
			case WeaponType.Scattergun: return 0.625f;
			case WeaponType.Pistol: return 0.15f;
			case WeaponType.RocketLauncher: return 0.8f;
			case WeaponType.GrenadeLauncher: return 0.6f;
			case WeaponType.StickyBombLauncher: return 0.6f;
			case WeaponType.CrusadersCrossbow: return 0;
			default: return 1;
		}
	}

	public static float ReloadTime(this WeaponType type, bool firstReload)
	{
		switch (type)
		{
			case WeaponType.Scattergun: return firstReload ? 0.7f : 0.5f;
			case WeaponType.Pistol: return 1.1f;
			case WeaponType.RocketLauncher: return firstReload ? 0.92f : 0.8f;
			case WeaponType.GrenadeLauncher: return firstReload ? 1.24f : 0.6f;
			case WeaponType.StickyBombLauncher: return firstReload ? 1.09f : 0.67f;
			case WeaponType.CrusadersCrossbow: return 1.6f; // TODO: Can passive reload...
			default: return 1;
		}
	}

	public static bool ReloadsAll(this WeaponType type)
	{
		return type == WeaponType.Pistol;
	}
}

// We could emulate having more ping with this as well...
public abstract class Weapon : MonoBehaviour
{
	public AudioClip fireSound;
	public AudioClip reloadSound;

	public static Text ammoPanel;
	public static Text totalAmmoPanel;

	public WeaponState state;

	public virtual void OnSpawn()
	{
		state.timeSinceFire = 1000;
		state.timeSinceReload = 1000;
		state.firstReload = true;
		state.ammo = state.type.Ammo();
		state.totalAmmo = state.type.TotalAmmo();
	}

	public void OnSwitchTo()
	{
		state.firstReload = true;
		state.timeSinceReload = 0;
		state.timeSinceFire = 1000;
	}

	public bool CanFire()
	{
		return state.ammo >= 1 && state.timeSinceFire >= state.type.FireTime();
	}

	// 0.5 seconds switch time since technically we're doing 0.25 for switch to, and 0.25 for switch from, and both happen when you switch
	public virtual float SwitchTime()
	{
		return 0.25f;
	}

	public virtual void PassiveTick()
	{

	}

	public virtual void Tick(ref InputInfo input)
	{
		state.timeSinceFire += Time.fixedDeltaTime;
		if (state.timeSinceFire >= state.type.FireTime() && state.ammo < state.type.Ammo() && state.totalAmmo > 0)
		{
			if (state.timeSinceReload == 0)
			{
				SoundHandler.SpawnSound(transform, transform.position, reloadSound, 1.0f);
				GetComponent<Player>().anim.SetTrigger("Reload");
			}
			state.timeSinceReload += Time.fixedDeltaTime;
		}
		if (CanFire() && input.Primary_Fire)
		{
			state.timeSinceFire = 0;
			state.timeSinceReload = 0;
			state.firstReload = true;
			state.ammo--;
			Fire();
		}
		if (state.ammo < state.type.Ammo() && state.totalAmmo > 0 && state.timeSinceReload >= state.type.ReloadTime(state.firstReload))
		{
			// The actual ammo increase should happen before the reload is over...
			if (state.type.ReloadsAll())
			{
				state.ammo = Mathf.Min(state.totalAmmo, state.type.Ammo());
				state.totalAmmo = Mathf.Max(0, state.totalAmmo - state.type.Ammo());
			}
			else
			{
				state.ammo++;
				state.totalAmmo--;
			}
			state.timeSinceReload = 0;
			state.firstReload = false;
		}
	}

	public virtual void Fire()
	{
		SoundHandler.SpawnSound(transform, transform.position, fireSound, 1.0f);
		GetComponent<Player>().anim.SetTrigger("Attack");
	}



	public static float DamageRamp(DamageRampUpType damageRampUpType, float distance)
	{
		if (damageRampUpType == DamageRampUpType.None)
			return 1;

		distance /= Player.HAMMER_SCALER;
		if (damageRampUpType == DamageRampUpType.SyringeArrow)
		{
			distance = 10.24f - Mathf.Clamp(distance, 0, 10.24f); // I think it's just standard reverse damage falloff
		}
		else if (distance >= 10.24f)
			distance = 10.24f - 1.024f;
		else
			distance -= 1.024f; // Apparantly disabling random spread means that the distance is treated as less, and maxes out at 1024 - 102.4 (921.6)
								//if (distance >= 10.24f)
								//	return 0.5f;

		float sin = Mathf.Cos(distance * Mathf.PI / 10.24f);
		if (distance < 5.12f)
		{
			if (damageRampUpType == DamageRampUpType.Scattergun)
				return 1 + sin * 0.75f;
			else if (damageRampUpType == DamageRampUpType.Rocket)
				return 1 + sin * 0.25f;
			else if (damageRampUpType == DamageRampUpType.Sticky)
				return 1 + sin * 0.2f;
			else
				return 1 + sin * 0.5f;
		}
		else
		{
			return 0.5f + (sin + 1) * 0.5f;
		}
	}
}

public enum DamageRampUpType
{
	Default,
	None, // Grenades, sniper bullets
	Scattergun,
	Rocket,
	Sticky,
	SyringeArrow
}
