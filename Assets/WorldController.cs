using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;


[RequireComponent(typeof(WorldGenerator))]
[RequireComponent(typeof(WorldRenderer))]
[RequireComponent(typeof(ColliderManager))]
public class WorldController : MonoBehaviour {

    private static WorldController instance;

	private ColliderManager cMgr;

	static string WORLD_SAVE_NAME = "worldSave.dat";
	static int NULL_TILE = 0;

	public int[,] world;

	WorldGenerator wGen;
	WorldRenderer wRend;

	void Start () {
		wGen = GetComponent<WorldGenerator>();
		wRend = GetComponent<WorldRenderer>();
		cMgr = GetComponent<ColliderManager>();

		if (instance == null) {
			instance = this;
		} else if (instance != this) {
			Destroy(this.gameObject);
		}

		DontDestroyOnLoad(this.gameObject);
	}
	
	void Update () {
		
	}

    public static WorldController Instance {
        get {
            return instance;
        }
    }

    //World Save/Load functions
    //-------------------------------------------------------------------------------

    [ContextMenu("Save World")]
	public void SaveWorld() {
		Debug.Log("Saving World...");
		BinaryFormatter bf = new BinaryFormatter ();
		FileStream file1 = File.Open (Application.persistentDataPath + "/" + WORLD_SAVE_NAME, FileMode.OpenOrCreate);

		bf.Serialize (file1, world);
		file1.Close ();
	}

	[ContextMenu("Load World")]
	public void LoadWorld() {
		if (File.Exists (Application.persistentDataPath + "/" + WORLD_SAVE_NAME)) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file1 = File.Open (Application.persistentDataPath + "/" + WORLD_SAVE_NAME, FileMode.Open);
			int[,] loadedWorld = (int[,])bf.Deserialize (file1);
			file1.Close ();

			Debug.Log("Loading stats...");
			LoadSavedWorld(loadedWorld);
		} else {
			Debug.Log("No stats file found.");
		}
	}

	void LoadSavedWorld(int[,] loadedWorld) {
		world = loadedWorld;
		RenderWorld();
	}

	[ContextMenu("Delete World")]
	void DeleteWorld() {
		if (File.Exists(Application.persistentDataPath + "/" + WORLD_SAVE_NAME)) {
			File.Delete(Application.persistentDataPath + "/" + WORLD_SAVE_NAME);
		}
		RenderWorld();
	}

	private void RenderWorld() {
		//wRend.RenderWorld(world);
	}

	//-------------------------------------------------------------------------------

	public void AddTile(int x, int y, int newTile) {
		UpdateTile(x, y, newTile);
	}

	public void RemoveTile(int x, int y) {
		UpdateTile(x, y, NULL_TILE);
	}

	private void UpdateTile(int x, int y, int val) {

	}
}
