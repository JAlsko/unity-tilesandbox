using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using UnityEngine.Tilemaps;

public class TerrainGenerator : MonoBehaviour {

	static string TERRAIN_SAVE_NAME = "terrainSave.dat";

	public Tilemap tmap;
	public List<TileBase> tbases = new List<TileBase>();

	public int width = 10;
	public int height = 10;

	public int interval = 1;
	public float reduction = 0.5f;
	public bool smooth = true;

	public int[,] currentTerrain;

	public int plainHeight;

	void Start () {
		LoadTerrain();
		FillTerrain();
	}
	
	void Update () {
	}

	[ContextMenu("Save Terrain")]
	public void SaveTerrain() {
		Debug.Log("Saving terrain...");
		BinaryFormatter bf = new BinaryFormatter ();
		FileStream file1 = File.Open (Application.persistentDataPath + "/" + TERRAIN_SAVE_NAME, FileMode.OpenOrCreate);

		bf.Serialize (file1, currentTerrain);
		file1.Close ();
	}

	[ContextMenu("Load Terrain")]
	public void LoadTerrain() {
		if (File.Exists (Application.persistentDataPath + "/" + TERRAIN_SAVE_NAME)) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file1 = File.Open (Application.persistentDataPath + "/" + TERRAIN_SAVE_NAME, FileMode.Open);
			int[,] loaded_terrain = (int[,])bf.Deserialize (file1);
			file1.Close ();

			Debug.Log("Loading stats...");
			currentTerrain = loaded_terrain;
			//LoadSavedTerrain(loaded_terrain);
		} else {
			Debug.Log("No stats file found.");
		}
	}

	void LoadSavedTerrain(int[,] world) {
		RenderNewTerrain(world);
	}

	[ContextMenu("Delete Terrain")]
	void DeleteTerrain() {
		if (File.Exists(Application.persistentDataPath + "/" + TERRAIN_SAVE_NAME)) {
			File.Delete(Application.persistentDataPath + "/" + TERRAIN_SAVE_NAME);
		}
		RenderNewTerrain();
	}


	[ContextMenu("GenerateNewTerrain")]
	public void RenderNewTerrain() {
		RenderNewTerrain(null);
	}

	[ContextMenu("GeneratePlainTerrain")]
	public void RenderPlainTerrain() {
		int[,] newWorld = GenerateArray(width, height, true);
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				if (y < plainHeight) {
					newWorld[x,y] = 3;
				} else {
					newWorld[x,y] = 0;
				}
			}
		}
		RenderMap(newWorld, tmap, tbases);
		currentTerrain = newWorld;
	}

	void FillTerrain() {
		TileBase tb = tbases[1];
		Vector3Int[] positions = new Vector3Int[currentTerrain.Length];
        TileBase[] tileArray = new TileBase[positions.Length];
		int index = 0;
		for (int x = 0; x < currentTerrain.GetUpperBound(0); x++) {
			for (int y = 0; y < currentTerrain.GetUpperBound(1); y++) {
				positions[index] = Vector3Int.right*x + Vector3Int.up*y;
				tileArray[index] = tb;
				index++;
				//tilemap.SetTile(new Vector3Int(x, y, 0), tilebases[map[x,y]]);
			}
		}

        tmap.SetTiles(positions, tileArray);
	}

	public void RenderNewTerrain(int[,] world) {
		int[,] world_to_load;
		if (world == null) {
			int[,] newWorld = GenerateArray(width, height, true);

			if (smooth) {
				newWorld = PerlinNoiseSmooth(newWorld, DateTime.Now.TimeOfDay.Seconds, reduction, interval);
			} else {
				newWorld = PerlinNoise(newWorld, DateTime.Now.TimeOfDay.Seconds, reduction);
			}

			world_to_load = newWorld;

		} else {
			world_to_load = world;
		}

		RenderMap(world_to_load, tmap, tbases);
		currentTerrain = world_to_load;
	}

	public static int[,] GenerateArray(int width, int height, bool empty) {
		int[,] map = new int[width,height];
		for (int x = 0; x < map.GetUpperBound(0); x++) {
			for (int y = 0; y < map.GetUpperBound(1); y++) {
				if (empty)
					map[x,y] = 0;
				else
					map[x,y] = 1;
			}
		}
		return map;
	}

	public static void RenderMap(int[,] map, Tilemap tilemap, List<TileBase> tilebases) {
		float startTime = Time.realtimeSinceStartup;
		
		tilemap.ClearAllTiles();
		
		Vector3Int[] positions = new Vector3Int[map.Length];
        TileBase[] tileArray = new TileBase[positions.Length];
		int index = 0;
		for (int x = 0; x < map.GetUpperBound(0); x++) {
			for (int y = 0; y < map.GetUpperBound(1); y++) {
				positions[index] = new Vector3Int(x, y, 0);
				tileArray[index] = tilebases[map[x,y]];
				index++;
				//tilemap.SetTile(new Vector3Int(x, y, 0), tilebases[map[x,y]]);
			}
		}

        tilemap.SetTiles(positions, tileArray);
		
		float finishTime = Time.realtimeSinceStartup;
		Debug.Log("Rendering tiles took " + (finishTime - startTime) + " seconds and finished at " + Time.realtimeSinceStartup + " seconds.");
	}

	public void RenderMap(int[,] map) {
		RenderMap(map, tmap, tbases);
	}
	
	public static void UpdateMap(int[,] map, Tilemap tilemap) {
		for (int x = 0; x < map.GetUpperBound(0); x++) {
			for (int y = 0; y < map.GetUpperBound(1); y++) {
				if (map[x, y] == 0) {
					tilemap.SetTile(new Vector3Int(x, y, 0), null);
				}
			}
		}
	}

	public static int[,] PerlinNoise(int[,] map, float seed, float reduction) {
		int newPoint;
		for (int x = 0; x < map.GetUpperBound(0); x++) {
			newPoint = Mathf.FloorToInt((Mathf.PerlinNoise(x/Mathf.PI, seed) - reduction) * map.GetUpperBound(1));
			Debug.Log(newPoint);
			newPoint += (map.GetUpperBound(1)/2);

			map[x, newPoint] = 1;

			for (int y = newPoint-1; y >= 0; y--) {
				map[x, y] = 2;
			}
		}
		return map;
	}

	public static int[,] PerlinNoiseSmooth(int[,] map, float seed, float reduction, int interval) {
		if (interval > 1) {
			int newPoint, points;
			Vector2Int currentPos, lastPos;
			List<int> noiseX = new List<int>();
			List<int> noiseY = new List<int>();

			for (int x = 0; x < map.GetUpperBound(0); x += interval) {
				newPoint = Mathf.FloorToInt((Mathf.PerlinNoise(x, (seed*reduction))) * map.GetUpperBound(1));
				noiseY.Add(newPoint);
				noiseX.Add(x);
			}

			points = noiseY.Count;
			for (int i = 1; i < points; i++) {
				currentPos = new Vector2Int(noiseX[i], noiseY[i]);
				lastPos = new Vector2Int(noiseX[i-1], noiseY[i-1]);
				Vector2 diff = currentPos - lastPos;

				float heightChange = diff.y/interval;
				float currHeight = lastPos.y;

				for (int x = lastPos.x; x < currentPos.x; x++) {
					bool topTile = true;
					for (int y = Mathf.FloorToInt(currHeight); y > 0; y--) {
						if (topTile) {
							map[x, y] = 1;
							topTile = false;
						} else if (y > Mathf.FloorToInt(currHeight/2)){
							map[x, y] = 1;
						} else {
							map[x, y] = 3;
						}
					}
					currHeight += heightChange;
				}
			}
		} else {
			map = PerlinNoise(map, seed, reduction);
		}
		return map;
	}
}
