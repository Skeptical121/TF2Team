using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundHandler : MonoBehaviour
{
	public static SoundHandler self;
	public GameObject soundPrefab;
	public Sound[] sounds;
	private Dictionary<SoundType, AudioClip[]> clips;
	private Dictionary<SoundType, float> volumes;

	// Start is called before the first frame update
	void Start()
	{
		self = Camera.main.GetComponent<SoundHandler>();
		clips = new Dictionary<SoundType, AudioClip[]>();
		volumes = new Dictionary<SoundType, float>();
		foreach (Sound s in sounds)
		{
			//if (!clips.ContainsKey(s.type))
			//	clips.Add(s.type, new List<AudioClip>());
			clips.Add(s.type, s.clips);
			volumes.Add(s.type, s.volume);
		}
	}

	// Update is called once per frame
	void Update()
	{

	}

	public static AudioSource SpawnSound(Transform parent, Vector3 pos, SoundType sound)
	{
		if (!self.clips.ContainsKey(sound))
			Debug.LogError("Sound type " + sound + " doesn't exist yet!");
		return SpawnSound(parent, pos, self.clips[sound][UnityEngine.Random.Range(0, self.clips[sound].Length)], self.volumes[sound]);
	}

	public static AudioSource SpawnSound(Transform parent, Vector3 pos, AudioClip clip, float volume)
	{
		if (Base.self.renderStart && clip != null)
		{
			GameObject soundObject = Instantiate(self.soundPrefab, parent);
			soundObject.transform.position = pos;
			AudioSource ac = soundObject.GetComponent<AudioSource>();
			ac.clip = clip;
			ac.volume = volume;
			ac.Play();
			Destroy(soundObject, ac.clip.length + 1.0f);
			return ac;
		}
		else
		{
			return null;
		}
	}
}

[Serializable]
public class Sound
{
	public SoundType type;
	public AudioClip[] clips;
	public float volume = 1;
}

public enum SoundType
{
	Hitsound,
	PainScout,
	PainSoldier,
	PainDemoman,
	PainMedic
}
