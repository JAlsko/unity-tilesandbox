using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGenerator : MonoBehaviour {

	//public int width = 10;
	//public int height = 10;

	public int interval = 1;
	static int s_interval;
	public float reduction = 0.5f;
	public bool smooth = true;

	public int minInterval = 1;
	public float roughness = .5f;
	static float s_roughness;

	void Start () {
		s_interval = interval;
		s_roughness = roughness;
	}
	
	void Update () {
		
	}

	public int[,] GetNewPerlinWorld(int width, int height) {
		int[,] newWorld = GenerateArray(width, height, true);
		PerlinNoiseSmooth(newWorld, Time.time, reduction, interval);
		return newWorld;
	}

	public int[,] GetNewFractalWorld(int width, int height) {
		int[,] newWorld = GenerateArray(width, height, true);
		FractalTerrain(newWorld, Time.time);
		return newWorld;
	}

	//public int[,] GetNewPerlinWorld() {
	//	return GetNewPerlinWorld(width, height);
	//}

	public static int[,] GenerateArray(int width, int height, bool empty) {
		int[,] map = new int[width,height];
		for (int x = 0; x <= map.GetUpperBound(0); x++) {
			for (int y = 0; y <= map.GetUpperBound(1); y++) {
				if (empty)
					map[x,y] = 0;
				else
					map[x,y] = 1;
			}
		}
		return map;
	}

	public static float[,] GenerateArray(int width, int height) {
		float[,] map = new float[width,height];
		for (int x = 0; x <= map.GetUpperBound(0); x++) {
			for (int y = 0; y <= map.GetUpperBound(1); y++) {
				map[x,y] = 0;
			}
		}
		return map;
	}

	public static int[] GenerateArray(int length) {
		int[] map = new int[length];
		for (int i = 0; i < map.Length; i++) {
			map[i] = 0;
		}

		return map;
	}

	public static int[,] FractalTerrain(int[,] map, float seed) {
		float[] heightMap = Enumerable.Repeat(0f, map.GetUpperBound(0)+1).ToArray();
		heightMap = InterpolateHeightMap(FractalRecurs(heightMap, 0, heightMap.Length-1, 1));
		for (int x = 0; x < map.GetUpperBound(0)+1; x++) {
			int heightVal = (int)((map.GetUpperBound(1)+1)/2 + heightMap[x]*(map.GetUpperBound(1)+1)/3);
			for (int y = 0; y < map.GetUpperBound(1)+1; y++) {
				if (y > heightVal) {
					if (UnityEngine.Random.Range(0, 360) < 1) {
						map[x, y] = 3;
					} else {
						map[x, y] = 0;
					}
				} else if (y == heightVal) {
					map[x, y] = 1;
				} else {
					if (UnityEngine.Random.Range(0, 75) < 1) {
						map[x, y] = 3;
					} else {
						map[x, y] = 2;
					}
				}
			}
		}
		return map;
	}

	//First index MUST be nonzero
	static float[] InterpolateHeightMap(float[] heightMap) {
		int prevFilledIndex = 0;
		int currentEmptyLength = 0;
		float prevFilledVal = 0;
		float prevVal = heightMap[0];
		for (int i = 1; i < heightMap.Length; i++) {
			//Start of interpolation
			if (heightMap[i] == 0) {
				if (prevVal != 0) {
					//Debug.Log("Starting interpolation at " + prevFilledIndex);
					prevFilledIndex = i-1;
					prevFilledVal = prevVal;
					currentEmptyLength = 2;
				}
				else {
					currentEmptyLength++;
				}
			} else {
				if (prevVal == 0) {
					currentEmptyLength++;
					float valDiff = heightMap[i]-prevFilledVal;
					float avgChange = valDiff/currentEmptyLength;
					int startIndex = prevFilledIndex+1;
					for (int j = 0; j < i-startIndex; j++) {
						heightMap[startIndex + j] = prevFilledVal + ((j+1) * avgChange);
						//Debug.Log("Changing index " + (startIndex + j) + " val to " + heightMap[j]);
					}
				}
			}
			prevVal = heightMap[i];
		}

		return heightMap;
	}

	static float[] FractalRecurs(float[] heightMap, int leftIndex, int rightIndex, float displacement) {
		if (Mathf.Abs(rightIndex - leftIndex) <= s_interval) {
			return heightMap;
		}
		int midIndex = (leftIndex + rightIndex)/2;
		float change = UnityEngine.Random.Range(-1f,1f) * displacement;
		float avg = (heightMap[leftIndex] + heightMap[rightIndex])/2;
		heightMap[midIndex] = avg + change;
		//Debug.Log("Changing index " + midIndex + " val to " + heightMap[midIndex]);
		displacement *= s_roughness;
		heightMap = FractalRecurs(heightMap, leftIndex, midIndex, displacement);
		return FractalRecurs(heightMap, midIndex, rightIndex, displacement);
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
