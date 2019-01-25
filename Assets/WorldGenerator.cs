using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour {

	//public int width = 10;
	//public int height = 10;

	public int interval = 1;
	public float reduction = 0.5f;
	public bool smooth = true;

	void Start () {
		
	}
	
	void Update () {
		
	}

	public int[,] GetNewPerlinWorld(int width, int height) {
		int[,] newWorld = GenerateArray(width, height, true);
		PerlinNoiseSmooth(newWorld, Time.time, reduction, interval);
		return newWorld;
	}

	//public int[,] GetNewPerlinWorld() {
	//	return GetNewPerlinWorld(width, height);
	//}

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

	public static int[,] PerlinNoise(int[,] map, float seed, float reduction) {
		int newPoint;
		for (int x = 0; x < map.GetUpperBound(0); x++) {
			newPoint = Mathf.FloorToInt((Mathf.PerlinNoise(x/Mathf.PI, seed) - reduction) * map.GetUpperBound(1));
			//Debug.Log(newPoint);
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
							map[x, y] = 2;
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
