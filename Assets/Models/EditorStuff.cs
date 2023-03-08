using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class EditorStuff : EditorWindow
{
	private AnimationClip refPose;
	private AnimationClip pose1;
	private AnimationClip pose2;

	private string hitboxes;
	private GameObject bluePrefab;
	// private GameObject redPrefab;

	private GameObject map;

	[MenuItem("Window/EditorStuff")]
	static void Init()
	{
		// Get existing open window or if none, make a new one:
		EditorStuff window = (EditorStuff)GetWindow(typeof(EditorStuff));
		window.Show();
	}

	void OnGUI()
	{
		EditorGUILayout.PrefixLabel("Ref");
		refPose = EditorGUILayout.ObjectField(refPose, typeof(AnimationClip), false) as AnimationClip;
		EditorGUILayout.PrefixLabel("Down");
		pose1 = EditorGUILayout.ObjectField(pose1, typeof(AnimationClip), false) as AnimationClip;
		EditorGUILayout.PrefixLabel("Straight_Up");
		pose2 = EditorGUILayout.ObjectField(pose2, typeof(AnimationClip), false) as AnimationClip;

		if (GUILayout.Button("Set Ref Pose"))
		{
			AnimationUtility.SetAdditiveReferencePose(pose1, refPose, 0);
			AnimationUtility.SetAdditiveReferencePose(pose2, refPose, 0);
		}

		EditorGUILayout.Separator();
		EditorGUILayout.PrefixLabel("Hitboxes");
		hitboxes = EditorGUILayout.TextArea(hitboxes);
		bluePrefab = EditorGUILayout.ObjectField(bluePrefab, typeof(GameObject), false) as GameObject;
		// redPrefab = EditorGUILayout.ObjectField(redPrefab, typeof(GameObject), false) as GameObject;

		if (GUILayout.Button("Set Hitboxes"))
		{
			string[] lines = hitboxes.Split('\n');
			foreach (string line in lines)
			{
				if (line.StartsWith("$hbox "))
				{
					// string name = line.Substring(line.IndexOf('"') + 1, line.LastIndexOf('"') - line.IndexOf('"'));
					Debug.Log(line);
					string[] split = line.Split(' ');
					Debug.Log(split[2].Substring(1, split[2].Length - 2));

					Vector3 min = new Vector3(float.Parse(split[3]), float.Parse(split[4]), float.Parse(split[5])) * 0.01f;
					Vector3 max = new Vector3(float.Parse(split[6]), float.Parse(split[7]), float.Parse(split[8])) * 0.01f;
					Vector3 center = (min + max) / 2;
					center.x = -center.x; // It appears that the X coordinate is supposed to be flipped
					Vector3 size = max - min;

					foreach (GameObject prefab in new GameObject[] { bluePrefab }) //, redPrefab })
					{
						RecurseCreateHitbox(prefab.transform, split[2].Substring(1, split[2].Length - 2), center, size);
					}
				}
			}
		}

		EditorGUILayout.Separator();
		map = EditorGUILayout.ObjectField(map, typeof(GameObject), true) as GameObject;
		if (GUILayout.Button("Create map elevations"))
		{
			Base b = GameObject.Find("Base").GetComponent<Base>();
			Base.self = b;
			if (b.maps == null)
				b.maps = new List<Map>();
			if (b.GetMap(map.gameObject.name) == null)
				b.maps.Add(new Map(map.transform));
			else
				Debug.Log("Map " + map.gameObject.name + " already exists!");
		}
	}

	private void RecurseCreateHitbox(Transform t, string name, Vector3 center, Vector3 size)
	{
		if (t.gameObject.name.Equals(name))
		{
			BoxCollider bc = t.gameObject.GetComponent<BoxCollider>();
			if (bc == null)
			{
				Debug.Log("Add box collider to " + name);
				bc = t.gameObject.AddComponent<BoxCollider>();
			}
			bc.isTrigger = true;
			bc.center = center;
			bc.size = size;
		}
		else
		{
			for (int i = 0; i < t.childCount; i++)
				RecurseCreateHitbox(t.GetChild(i), name, center, size);
		}
	}
}
#endif