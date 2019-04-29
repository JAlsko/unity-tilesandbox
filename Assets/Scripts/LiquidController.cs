using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class LiquidTileState {
    public TileBase stateTile;
    public float liquidLevel = 0;
}

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(ChunkObjectsHolder))]
public class LiquidController : MonoBehaviour
{
    public float maxMass = 1f;
    public float maxCompression = 0.02f;
    public float minMass = 0.01f;
    public float minFlow = 0.2f;
    public float maxSpeed = 1f;

    public float calculationsPerSecond = 1;
    public int calculationIterations = 1;
    float calculationRate = 1;

    public float rendersPerSecond = 1;
    float renderRate = 1;

    float elapsedTime = 0;
    float lastCalculationTime = 0;
    float lastTextureTime = 0;

    public Color32 waterBaseColor;

    public Material liquidMat;
    Texture2D liquidTex;

    public Vector2Int spawnPoint = Vector2Int.zero;

    float[,] liquidMass;
    float[,] updatedLiquidMass;

    //float[,] sourceMass, destinationMass;

    WorldController wCon;
    ChunkObjectsHolder cObjs;

    string[,] world_fg;

    bool initialized = false;
    bool simulating = false;

    public int totalCalculations = 0;
    public float speedSum = 0;
    public float avgCalcTime = 0;

    public int totalIterations = 0;
    public int clampCalls = 0;
    public int minCalls = 0;
    public int methodCalls = 0;

    bool[,] dirtyTiles;
    List<int> chunksToRender = new List<int>();

    public List<LiquidTileState> liquidStates;

    void Start()
    {
        wCon = GetComponent<WorldController>();
        cObjs = GetComponent<ChunkObjectsHolder>();
        calculationRate = 1f/calculationsPerSecond;
        renderRate = 1f/rendersPerSecond;
    }

    private static int CompareLiquidStates(LiquidTileState x, LiquidTileState y) {
        if (x == null) {
            if (y == null) {
                return 0;
            }
            return -1;
        } else if (y == null) {
            return 1;
        }

        if (x.liquidLevel == y.liquidLevel) {
            return 0;
        } else if (x.liquidLevel > y.liquidLevel) {
            return 1;
        } else {
            return -1;
        }
    }

    public void SetChunksToRender(int[] newChunks) {
        chunksToRender.Clear();
        if (newChunks == null) {
            return;
        }
        foreach (int chunk in newChunks) {
            if (chunk >= WorldController.GetChunkCount() || chunk < 0) {
                continue;
            }
            chunksToRender.Add(chunk);
        }
    }

    void SortLiquidTileStates() {
        liquidStates.Sort(CompareLiquidStates);
    }

    public void InitializeLiquids() {
        world_fg = wCon.GetWorld(0);

        SortLiquidTileStates();

        liquidMass = new float[WorldController.GetWorldWidth()+2, WorldController.GetWorldHeight()+2];
        updatedLiquidMass = new float[WorldController.GetWorldWidth()+2, WorldController.GetWorldHeight()+2];
        dirtyTiles = new bool[WorldController.GetWorldWidth()+2, WorldController.GetWorldHeight()+2];
        //sourceMass = new float[WorldController.GetWorldWidth()+2, WorldController.GetWorldHeight()+2];
        //destinationMass = new float[WorldController.GetWorldWidth()+2, WorldController.GetWorldHeight()+2];
        

        /*liquidTex = new Texture2D(WorldController.GetWorldWidth(), WorldController.GetWorldHeight(), TextureFormat.RGBA32, false);
        //liquidTex.alphaIsTransparency = true;
        liquidTex.filterMode = FilterMode.Point;

        liquidMat.mainTexture = liquidTex;

        Color32[] newPixels = generatePixelArray(liquidMass);
        liquidTex.SetPixels32(newPixels);
        liquidTex.Apply();*/

        initialized = true;
    }

    void Update() {
        if (Input.GetKey(KeyCode.Space)) {
            SpawnWaterBlock();
        }
    }

    void FixedUpdate() {
        if (!initialized)
            return;

        elapsedTime += Time.fixedDeltaTime;

        if (elapsedTime - lastCalculationTime > calculationRate) {
            if (!simulating) {
                lastCalculationTime = elapsedTime;
                float startTime = Time.realtimeSinceStartup;
                LiquidSimulation(calculationIterations);
                //LiquidSimulation(calculationIterations);
                //LiquidSimulation(calculationIterations);
                //LiquidSimulation(calculationIterations);
                //LiquidSimulation(calculationIterations);
                float calcTime = Time.realtimeSinceStartup-startTime;
                totalCalculations += calculationIterations;
                speedSum+=calcTime;
                avgCalcTime = speedSum/totalCalculations;
            }
        } 

        if (elapsedTime - lastTextureTime > renderRate) {
            lastTextureTime = elapsedTime;
            /*Color32[] newPixels = generatePixelArray(liquidMass);
            liquidTex.SetPixels32(newPixels);
            liquidTex.Apply();*/
            RenderShownChunkLiquids();
        }
        
    }

    public void SpawnLiquidBlock(int x, int y) {
        liquidMass[x, y] = maxMass;
        dirtyTiles[x, y] = true;
    }

    public void EmptyLiquidBlock(int x, int y) {
        liquidMass[x, y] = 0;
        //updatedLiquidMass[x, y] = 0;
        dirtyTiles[x, y] = true;
    }

    [ContextMenu("SpawnWaterBlock")]
    public void SpawnWaterBlock() {
        dirtyTiles[spawnPoint.x, spawnPoint.y] = true;
        liquidMass[spawnPoint.x, spawnPoint.y] = maxMass;
        updatedLiquidMass[spawnPoint.x, spawnPoint.y] = maxMass;
    }

    Color32[] generatePixelArray(float[,] liquids) {
        Color32[] newArr = new Color32[WorldController.GetWorldWidth() * WorldController.GetWorldHeight()];
        int index = 0;
        for (int y = 1; y <= liquids.GetUpperBound(1)-1; y++) {
            for (int x = 1; x <= liquids.GetUpperBound(0)-1; x++) {
                if (x <= world_fg.GetUpperBound(0) && y <= world_fg.GetUpperBound(1)) {
                    if (world_fg[x, y] != "air") {
                        newArr[index] = new Color32(0, 0, 0, 255);
                    } else {
                        newArr[index] = new Color32(waterBaseColor.r, waterBaseColor.g, waterBaseColor.b, (byte) (Mathf.Clamp(liquids[x, y]/maxMass, 0, 1) * 255) );
                    }
                } else {
                    newArr[index] = new Color32(waterBaseColor.r, waterBaseColor.g, waterBaseColor.b, (byte) (Mathf.Clamp(liquids[x, y]/maxMass, 0, 1) * 255));
                }
                index++;
            }
        }

        return newArr;
    }

    TileBase GetLiquidTile(float liquidLevel, float aboveLiquidLevel) {
        foreach (LiquidTileState lts in liquidStates) {
            if (aboveLiquidLevel > minMass) {
                return liquidStates[liquidStates.Count-1].stateTile;
            }
            if (liquidLevel <= lts.liquidLevel) {
                return lts.stateTile;
            }
        }

        Debug.Log("Couldn't find appropriate liquid tile for liquid level " + liquidLevel + "!");
        return null;
    }

    void RenderShownChunkLiquids() {
        foreach (int chunk in chunksToRender) {
            RenderChunkLiquids(chunk);
        }
    }

    void RenderChunkLiquids(int chunk) {
        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();

        Tilemap liquidTilemap = cObjs.GetChunkLiquidTilemap(chunk);
        Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);

        for (int x = chunkPos.x; x < chunkPos.x+WorldController.chunkSize; x++) {
            for (int y = chunkPos.y; y < chunkPos.y+WorldController.chunkSize; y++) {
                if (dirtyTiles[x, y] == false)
                    continue;
                
                if (y == worldHeight) {
                    liquidTilemap.SetTile(new Vector3Int(x-chunkPos.x, y-chunkPos.y, 0), GetLiquidTile(liquidMass[x, y], 0));
                } else {
                    liquidTilemap.SetTile(new Vector3Int(x-chunkPos.x, y-chunkPos.y, 0), GetLiquidTile(liquidMass[x, y], liquidMass[x, y+1]));
                }
                dirtyTiles[x, y] = false;
            }
        }
    }

    void LiquidSimulation(int numIterations = 1) {
        simulating = true;
        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();

        LiquidSimulationStep(liquidMass, updatedLiquidMass, worldWidth, worldHeight);

        if (numIterations % 2 != 0) {
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    liquidMass[x, y] = updatedLiquidMass[x, y];
                }
            }
        }

        for (int x = 0; x < worldWidth+2; x++) {
            liquidMass[x, 0] = 0;
            liquidMass[x, worldHeight + 1] = 0;
        }
        for (int y = 0; y < worldHeight+1; y++) {
            liquidMass[0, y] = 0;
            liquidMass[worldWidth+1, y] = 0;
        }
        simulating = false;
    }

    void LiquidSimulationStep(float[,] sourceMass, float[,] destinationMass, int worldWidth, int worldHeight) {
        float flow = 0;
        float remainingMass;

        //int worldWidth = WorldController.GetWorldWidth();
        //int worldHeight = WorldController.GetWorldHeight();

        //for (int curIteration = 0; curIteration < numIterations; curIteration++) {
            //sourceMass = liquidMass;//curIteration % 2 == 0 ? liquidMass : updatedLiquidMass;
            //destinationMass = updatedLiquidMass;//curIteration % 2 == 0 ? updatedLiquidMass : liquidMass;

            for (int x = 1; x < worldWidth-1; x++) {
                for (int y = 1; y < worldHeight-1; y++) {
                    totalIterations++;

                    if (world_fg[x, y] != "air") {
                        continue;
                    }

                    flow = 0;
                    remainingMass = sourceMass[x, y];
                    if (remainingMass <= 0) {
                        continue;
                    }

                    if (world_fg[x, y-1] == "air") {
                        flow = GetStableStateB(remainingMass + sourceMass[x, y-1]) - sourceMass[x, y-1];
                        if (flow > minFlow) {
                            flow *= 0.5f;
                        }
                        flow = Mathf.Clamp(flow, 0, Mathf.Min(maxSpeed, remainingMass));
                        
                        destinationMass[x, y] -= flow;
                        destinationMass[x, y-1] += flow;
                        remainingMass -= flow;

                        clampCalls++;
                        minCalls++;
                        methodCalls++;

                        dirtyTiles[x, y] = true;
                        dirtyTiles[x, y-1] = true;
                    }

                    if (remainingMass <= 0) {
                        continue;
                    }

                    if (world_fg[x-1, y] == "air") {
                        flow = (sourceMass[x, y] - sourceMass[x-1, y])/4;
                        if (flow > minFlow) {
                            flow *= 0.5f;
                        }
                        flow = Mathf.Clamp(flow, 0, remainingMass);
                        
                        destinationMass[x, y] -= flow;
                        destinationMass[x-1, y] += flow;
                        remainingMass -= flow;

                        clampCalls++;

                        dirtyTiles[x, y] = true;
                        dirtyTiles[x-1, y] = true;
                    }

                    if (remainingMass <= 0) {
                        continue;
                    }

                    if (world_fg[x+1, y] == "air") {
                        flow = (sourceMass[x, y] - sourceMass[x+1, y])/4;
                        if (flow > minFlow) {
                            flow *= 0.5f;
                        }
                        flow = Mathf.Clamp(flow, 0, remainingMass);
                        
                        destinationMass[x, y] -= flow;
                        destinationMass[x+1, y] += flow;
                        remainingMass -= flow;

                        clampCalls++;

                        dirtyTiles[x, y] = true;
                        dirtyTiles[x+1, y] = true;
                    }

                    if (remainingMass <= 0) {
                        continue;
                    }

                    if (world_fg[x, y+1] == "air") {
                        flow = remainingMass - GetStableStateB(remainingMass + sourceMass[x, y+1]);
                        if (flow > minFlow) {
                            flow *= 0.5f;
                        }
                        flow = Mathf.Clamp(flow, 0, Mathf.Min(maxSpeed, remainingMass));
                        
                        destinationMass[x, y] -= flow;
                        destinationMass[x, y+1] += flow;
                        remainingMass -= flow;

                        clampCalls++;
                        minCalls++;
                        methodCalls++;

                        dirtyTiles[x, y] = true;
                        dirtyTiles[x, y+1] = true;
                    }
                }
            }
        //}
    }

    float GetStableStateB(float totalMass) {
        if (totalMass <= 1) {
            return 1;
        } else if (totalMass < 2*maxMass + maxCompression) {
            return (maxMass * maxMass + totalMass*maxCompression) / (maxMass + maxCompression);
        } else {
            return (totalMass + maxCompression)/2;
        }
    }
}
