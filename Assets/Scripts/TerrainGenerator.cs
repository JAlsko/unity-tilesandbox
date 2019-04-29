using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    private TileController wMod;

    public Material terrainMat;
    private Texture2D terrainTex;

    public Vector2Int worldSize;

    public List<Color> tileColors = new List<Color>();

    public int seaLevel = 0;
    public int peakHeight = 1;

    public int interval = 1;
	public float roughness = .5f;
    public float plainsHeight = -1;
    private float realPlainsHeight;
    private int plainsPoints = 0;
    private int nonplainsPoints = 0;
    public float plainsRoughness = .5f;

    public int caveDepth = 16;

    public float caveStartAliveChance = .45f;
    public int caveBirthLimit = 0;
    public int caveDeathLimit = 0;
    public int caveSimulationSteps = 1;

    public int stoneDepth = 4;

    public float stoneStartAliveChance = .45f;
    public int stoneBirthLimit = 0;
    public int stoneDeathLimit = 0;
    public int stoneSimulationSteps = 1;

	void Start () {
        wMod = GetComponent<TileController>();
        realPlainsHeight = (plainsHeight - (worldSize.y/2)) / (worldSize.y/2);
	}

    void Update() {
        
    }
	
    /*[ContextMenu("Draw new world")]
	public void DrawNewWorld() {
        terrainTex = new Texture2D(worldSize.x, worldSize.y, TextureFormat.RGBAFloat, false);
        terrainTex.filterMode = FilterMode.Point;
        Color[] newPixels = generatePixelArray(world);
        terrainTex.SetPixels(newPixels);
        terrainTex.Apply();
        terrainTex.filterMode = FilterMode.Point;
        terrainMat.SetTexture("_MainTex", terrainTex);
    }*/

    public string[,] GenerateNewWorld() {
        string[,] newWorld;

        plainsPoints = 0;
        nonplainsPoints = 0;

        realPlainsHeight = (plainsHeight - (worldSize.y/2)) / (worldSize.y/2);
        newWorld = GetNewFractalWorld(worldSize.x, worldSize.y);
        if (stoneStartAliveChance > 0)
            DistributeStone(newWorld);

        //PlantGrass();

        /*if (caveStartAliveChance > 0)
            DigCaves(world);
        */
        return newWorld;
    }

    Color[] generatePixelArray(int[,] tiles) {
        Color[] newArr = new Color[tiles.Length];
        int index = 0;
        for (int y = 0; y <= tiles.GetUpperBound(1); y++) {
            for (int x = 0; x <= tiles.GetUpperBound(0); x++) {
                newArr[index] = tileColors[tiles[x, y]];
                if (y == plainsHeight) {
                    newArr[index] = Color.red;
                }
                index++;
            }
        }

        return newArr;
    }

	public static int[,] Get2DArrayCopy(int[,] arrayToCopy) {
		int[,] newArr = new int[arrayToCopy.GetUpperBound(0)+1, arrayToCopy.GetUpperBound(1)+1];
		for (int x = 0; x <= newArr.GetUpperBound(0); x++) {
			for (int y = 0; y <= newArr.GetUpperBound(1); y++) {
				newArr[x, y] = arrayToCopy[x, y];
			}
		}

		return newArr;
	}

    public static string[,] Get2DArrayCopy(string[,] arrayToCopy) {
		string[,] newArr = new string[arrayToCopy.GetUpperBound(0)+1, arrayToCopy.GetUpperBound(1)+1];
		for (int x = 0; x <= newArr.GetUpperBound(0); x++) {
			for (int y = 0; y <= newArr.GetUpperBound(1); y++) {
				newArr[x, y] = arrayToCopy[x, y];
			}
		}

		return newArr;
	}

	public string[,] GetNewFractalWorld(int width, int height) {
		string[,] newWorld = GenerateArray(width, height);
		newWorld = FractalTerrain(newWorld, Time.time);
		return newWorld;
	}

	public static string[,] GenerateArray(int width, int height, string defaultVal = "air") {
		string[,] map = new string[width,height];
		for (int x = 0; x <= map.GetUpperBound(0); x++) {
			for (int y = 0; y <= map.GetUpperBound(1); y++) {
                map[x,y] = defaultVal;
			}
		}
		return map;
	}

	public string[,] FractalTerrain(string[,] map, float seed) {
		float[] heightMap = Enumerable.Repeat(0f, map.GetUpperBound(0)+1).ToArray();
		heightMap = InterpolateHeightMap(FractalRecurs(heightMap, 0, heightMap.Length-1, 1));
		for (int x = 0; x < map.GetUpperBound(0)+1; x++) {
            int yMidpoint = (peakHeight)/2;
			int heightVal = (int)(yMidpoint + heightMap[x]*(yMidpoint + seaLevel));
			for (int y = 0; y < map.GetUpperBound(1)+1; y++) {
				if (y > heightVal) {
					//if (UnityEngine.Random.Range(0, 540) < 1) {
					//	map[x, y] = 3;
					//} else {
                        wMod.InitializeNewTile(x, y, "air");
                        map[x, y] = "air";
					//}
				} else if (y == heightVal) {
					//map[x, y] = 1;
                    wMod.InitializeNewTile(x, y, "dirt");
                    map[x, y] = "dirt";
				} else {
					if (UnityEngine.Random.Range(0, 400) < 1) {
						wMod.InitializeNewTile(x, y, "torch");
                        map[x, y] = "torch";
					} else {
						wMod.InitializeNewTile(x, y, "dirt");
                        map[x, y] = "dirt";
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

	public float[] FractalRecurs(float[] heightMap, int leftIndex, int rightIndex, float displacement) {
		if (Mathf.Abs(rightIndex - leftIndex) <= interval) {
			return heightMap;
		}

        float avg = (heightMap[leftIndex] + heightMap[rightIndex])/2;
		int midIndex = (leftIndex + rightIndex)/2;
        float change = UnityEngine.Random.Range(-1f,1f) * displacement;

        /*if ((heightMap[leftIndex] > plainsHeight && heightMap[rightIndex] > plainsHeight) || (heightMap[leftIndex] == 0 && heightMap[rightIndex] == 0)) {
            displacement *= roughness;
        } else {
            displacement *= plainsRoughness;
        }*/

        if (avg > realPlainsHeight){// || heightMap[leftIndex] == 0 || heightMap[rightIndex] == 0) {
            displacement *= roughness;
            nonplainsPoints++;
        } else {
            //Debug.Log("Plains height: " + realPlainsHeight + ".. this height: " + avg);
            displacement *= plainsRoughness;
            change *= plainsRoughness;
            plainsPoints++;
        }

		heightMap[midIndex] = avg + change;
		//Debug.Log("Changing index " + midIndex + " val to " + heightMap[midIndex]);
		heightMap = FractalRecurs(heightMap, leftIndex, midIndex, displacement);
		return FractalRecurs(heightMap, midIndex, rightIndex, displacement);
	}

    public int[,] GenerateNoiseArray(int width, int height, float startAliveChance) {
        int[,] noiseArr = new int[width,height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (UnityEngine.Random.Range(0f, 1f) < startAliveChance) {
                    noiseArr[x, y] = 1;
                } else {
                    noiseArr[x, y] = 0;
                }
            }
        }

        return noiseArr;
    }

    public int[,] CullNoiseWithDepth(string[,] world, int[,] noiseArr, int depth) {
        for (int x = 0; x < worldSize.x; x++) {
            int currentDepth = 0;
            for (int y = worldSize.y-1; y >= 0; y--) {
                if (world[x, y] == "air") {
                    continue;
                } else {
                    currentDepth++;
                    if (currentDepth >= depth) {
                        continue;
                    } else {
                        noiseArr[x, y] = 1;
                    }
                }
            }
        }

        return noiseArr;
    }

    int CountNeighbors(int[,] arr, int x, int y) {
        int neighborCount = 0;
        for (int i = -1; i < 2; i++) {
            for (int j = -1; j < 2; j++) {
                int neighborX = x+i;
                int neighborY = y+j;

                if (i == 0 && j == 0) {
                    continue;
                }
                
                if (neighborX < 0 || neighborY < 0 || neighborX > arr.GetUpperBound(0) || neighborY >= arr.GetUpperBound(1)) {
                    continue;
                }

                if (arr[neighborX, neighborY] == 1) {
                    neighborCount++;
                }
            }
        }

        return neighborCount;
    }

    int[,] CaveSimulationStep(int[,] oldCaves) {
        int[,] newCaves = new int[worldSize.x, worldSize.y];

        for (int x = 0; x < worldSize.x; x++) {
            for (int y = 0; y < worldSize.y; y++) {
                int neighborCount = CountNeighbors(oldCaves, x, y);

                if (oldCaves[x, y] == 0) {
                    if (neighborCount > caveBirthLimit) {
                        newCaves[x, y] = 1;
                    } else {
                        newCaves[x, y] = 0;
                    }
                }

                else if (oldCaves[x, y] == 1) {
                    if (neighborCount < caveDeathLimit) {
                        newCaves[x, y] = 0;
                    } else {
                        newCaves[x, y] = 1;
                    }
                }
            }
        }

        return newCaves;
    }

    int[,] StoneSimulationStep(int[,] oldStone) {
        int[,] newStone = new int[worldSize.x, worldSize.y];

        for (int x = 0; x < worldSize.x; x++) {
            for (int y = 0; y < worldSize.y; y++) {
                int neighborCount = CountNeighbors(oldStone, x, y);

                if (oldStone[x, y] == 0) {
                    if (neighborCount > stoneBirthLimit) {
                        newStone[x, y] = 1;
                    } else {
                        newStone[x, y] = 0;
                    }
                }

                else if (oldStone[x, y] == 1) {
                    if (neighborCount < stoneDeathLimit) {
                        newStone[x, y] = 0;
                    } else {
                        newStone[x, y] = 1;
                    }
                }
            }
        }

        return newStone;
    }

    public int[,] GenerateCaves(string[,] world) {
        int[,] caveArr = GenerateNoiseArray(worldSize.x, worldSize.y, caveStartAliveChance);
        caveArr = CullNoiseWithDepth(world, caveArr, caveDepth);
        for (int i = 0; i < caveSimulationSteps; i++) {
            caveArr = CaveSimulationStep(caveArr);
        }
        return caveArr;
    }

    public int[,] GenerateStone(string[,] world) {
        int[,] stoneArr = GenerateNoiseArray(worldSize.x, worldSize.y, stoneStartAliveChance);
        stoneArr = CullNoiseWithDepth(world, stoneArr, stoneDepth);
        for (int i = 0; i < stoneSimulationSteps; i++) {
            stoneArr = StoneSimulationStep(stoneArr);
        }
        return stoneArr;
    }

    public string[,] DigCaves(string[,] oldWorld) {
        int[,] caves = GenerateCaves(oldWorld);
        for (int x = 0; x < worldSize.x; x++) {
            for (int y = worldSize.y - 1; y >= 0; y--) {
                if (oldWorld[x, y] == "air") {
                    continue;
                }
                oldWorld[x, y] = caves[x, y] == 0 ? "air" : oldWorld[x, y];
            }
        }

        return oldWorld;
    }

    public void PlantGrass(int[,] world) {
        for (int x = 0; x < worldSize.x; x++) {
            for (int y = worldSize.y - 1; y >= 0; y--) {
                if (world[x, y] == 0) {
                    continue;
                }
                world[x, y] = 4;
                break;
            }
        }
    }

    public void DistributeStone(string[,] world) {
        int[,] stone = GenerateStone(world);
        for (int x = 0; x < worldSize.x; x++) {
            for (int y = 0; y < worldSize.y; y++) {
                if (world[x, y] == "air") {
                    continue;
                }
                world[x, y] = stone[x, y] == 0 ? "stone" : world[x, y];
            }
        }
    }

}
