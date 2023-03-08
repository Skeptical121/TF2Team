using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct InputInfo
{
	public const float SENSITIVITY = 2.5f; // 13.6364 inches / 360 degrees = 0.03788 inches / degree. 800 dpi
	public const float M_YAW = 0.022f; // tf2 constant
	public const float M_PITCH = 0.022f; // tf2 constant

	public enum Type
	{
		Jump,
		Forward,
		Back,
		Left,
		Right,
		Crouch,
		PrimaryFire
	}

	public bool JumpBuffer, ForwardBuffer, BackBuffer, LeftBuffer, RightBuffer; // So we can sync aim with movement, we feed in the inputs as observations

	public bool Jump;
	public bool Forward;
	public bool Back;
	public bool Left;
	public bool Right;
	public bool Crouch;
	public bool Primary_Fire; // A float is a more general way of storing inputs
	public bool ClassAbility;

	public int SwitchToSlot; // 0 to stay on same slot, positive numbers are shifted down 1 to get index

	public float rotChange; // Max velocity = 300 currently
	public float upDownChange; // Max velocity = 100 currently

	public void SetPlayerInputs(Player p)
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			Cursor.lockState = CursorLockMode.None;
		else if (Input.anyKeyDown)
			Cursor.lockState = CursorLockMode.Locked;

		// * 2 because of the OS (Operating System) multipler thing?
		p.playerDir *= Quaternion.Euler(0, Input.GetAxisRaw("Mouse X") * M_YAW * SENSITIVITY * 2f, 0);
		p.upDownRotation = Mathf.Clamp(p.upDownRotation - Input.GetAxisRaw("Mouse Y") * M_PITCH * SENSITIVITY * 2f, -89.0f, 89.0f);
		p.lookDir = p.playerDir * Quaternion.Euler(p.upDownRotation, 0, 0);

		p.lookTransform.rotation = p.lookDir;
		// Vector3 lookEuler = p.lookDir.eulerAngles;
		// p.lookTransform.eulerAngles = new Vector3(lookEuler.x, lookEuler.y, 90);

		Forward = Input.GetKey(KeyCode.W);
		Back = Input.GetKey(KeyCode.S);
		// Null movement for A / D, not W / S because we don't really use it for that anyways
		if (Input.GetKeyDown(KeyCode.D))
		{
			Left = false;
			Right = true;
		}
		else if (Input.GetKeyDown(KeyCode.A))
		{
			Right = false;
			Left = true;
		}
		if (Input.GetKeyUp(KeyCode.A))
		{
			Left = false;
			if (Input.GetKey(KeyCode.D))
				Right = true;
		}
		if (Input.GetKeyUp(KeyCode.D))
		{
			Right = false;
			if (Input.GetKey(KeyCode.A))
				Left = true;
		}
		Jump |= Input.GetKeyDown(KeyCode.Space);
		Crouch = Input.GetKey(KeyCode.LeftShift);
		Primary_Fire |= Input.GetKey(KeyCode.Mouse0);
		ClassAbility |= Input.GetKey(KeyCode.Mouse1);
		if (Input.GetKeyDown(KeyCode.Q))
			SwitchToSlot = 1;
		if (Input.GetKeyDown(KeyCode.F))
			SwitchToSlot = 2;
		if (Input.GetKeyDown(KeyCode.R))
			SwitchToSlot = 3;
	}
}