using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class LiquidTileState {
    public TileBase stateTile;
    public float minLiquidLevel = 0;
    public float maxLiquidLevel = 0;
}

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(ChunkObjectsHolder))]
public class LiquidController : Singleton<LiquidController>
{
    public const float liquidGravity = 0.25f;

    public float maxMass = 1f;
    public float maxCompression = 0.02f;
    public float minMass = 0.01f;
    public float minFlow = 0.2f;
    public float maxSpeed = 1f;

    public float minRenderMass = 0.1f;

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

    Dictionary<int, float[,]> chunkLiquidMasses = new Dictionary<int, float[,]>();
    Dictionary<int, float[,]> updatedChunkLiquidMasses = new Dictionary<int, float[,]>();

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
    Dictionary<int, bool[,]> chunkDirtyTiles = new Dictionary<int, bool[,]>();
    public List<int> chunksToRender = new List<int>();
    public List<int> chunksToUpdate = new List<int>();
    public List<int> chunksStartingUpdate = new List<int>();

    public List<LiquidTileState> liquidStates;

    void Start()
    {
        wCon = GetComponent<WorldController>();
        cObjs = GetComponent<ChunkObjectsHolder>();
        calculationRate = 1f/calculationsPerSecond;
        renderRate = 1f/rendersPerSecond;
    }

    public static int SetChunkVal<T> (Dictionary<int, T[,]> chunkDict, int x, int y, T newVal) {
        int chunk = WorldController.GetChunk(x, y);

        if (chunk < 0 || chunk > WorldController.GetChunkCount()) {
            return 0;
        }

        Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);
        chunkDict[chunk][x - chunkPos.x, y - chunkPos.y] = newVal;

        return 1;
    }

    public static int SetChunkVal<T> (Dictionary<int, T[,]> chunkDict, int chunk, int x, int y, T newVal) {
        if (chunk < 0 || chunk > WorldController.GetChunkCount()) {
            return 0;
        }

        chunkDict[chunk][x, y] = newVal;

        return 1;
    }

    public static T GetChunkVal<T> (Dictionary<int, T[,]> chunkDict, int x, int y) {
        int chunk = WorldController.GetChunk(x, y);

        if (chunk < 0 || chunk > WorldController.GetChunkCount()) {
            Debug.LogError("Trying to get chunk value at x: " + x + ", y:" + y + ". Chunk does not exist!");
            return default(T);
        }

        Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);

        try
        {
            return chunkDict[chunk][x - chunkPos.x, y - chunkPos.y];
        }
        catch (System.IndexOutOfRangeException e)  // CS0168
        {
            Debug.Log("Chunk error at chunk " + chunk);
            Debug.Log("Indexing at x: " + (x) + ", y:" + (y));
            Debug.Log("Chunk position at " + chunkPos);
            // Set IndexOutOfRangeException to the new exception's InnerException.
            throw new System.ArgumentOutOfRangeException("index parameter is out of range.", e);
        }
    }

    public static T GetChunkVal<T> (Dictionary<int, T[,]> chunkDict, int chunk, int x, int y) {
        if (chunk < 0 || chunk > WorldController.GetChunkCount()) {
            Debug.LogError("Trying to get chunk value at x: " + x + ", y:" + y + ". Chunk does not exist!");
            return default(T);
        }

        return chunkDict[chunk][x, y];
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

        if (x.minLiquidLevel == y.minLiquidLevel) {
            return 0;
        } else if (x.minLiquidLevel > y.minLiquidLevel) {
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

        int totalChunks = WorldController.GetChunkCount();
        int chunkSize = WorldController.chunkSize;
        for (int chunk = 0; chunk < totalChunks; chunk++) {
            chunkLiquidMasses[chunk] = new float[chunkSize, chunkSize];
            updatedChunkLiquidMasses[chunk] = new float[chunkSize, chunkSize];
            chunkDirtyTiles[chunk] = new bool[chunkSize, chunkSize];
        }

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
        //if (Input.GetKey(KeyCode.Space)) {
        //    SpawnLiquidBlock(spawnPoint.x, spawnPoint.y);
        //}
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
            //Color32[] newPixels = generatePixelArray(liquidMass);
            //liquidTex.SetPixels32(newPixels);
            //liquidTex.Apply();
            RenderShownChunkLiquids();
        }
        
    }

    public void SpawnLiquidBlock(int x, int y, float amount = 1) {
        //liquidMass[x, y] = maxMass;
        SetLiquidMass(x, y, amount);
        //dirtyTiles[x, y] = true;
        SetChunkVal(chunkDirtyTiles, x, y, true);

        int chunk = WorldController.GetChunk(x, y);
        //StartChunkLiquidSimulation(chunk);
    }

    public void SpawnLiquidBlock(float amount = 1) {
        Vector3 mousePos = CursorController.Instance.GetMousePos();
        SpawnLiquidBlock((int)mousePos.x, (int)mousePos.y, amount);
    }

    public void EmptyLiquidBlock(int x, int y) {
        //liquidMass[x, y] = 0;
        SetLiquidMass(x, y, 0f);
        //dirtyTiles[x, y] = true;
        SetChunkVal(chunkDirtyTiles, x, y, true);

        int chunk = WorldController.GetChunk(x, y);
        //StartChunkLiquidSimulation(chunk);
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
            if (aboveLiquidLevel > minRenderMass) {
                return liquidStates[liquidStates.Count-1].stateTile;
            }
            if (liquidLevel <= lts.maxLiquidLevel && liquidLevel >= lts.minLiquidLevel) {
                return lts.stateTile;
            }
        }

        Debug.Log("Couldn't find appropriate liquid tile for liquid level " + liquidLevel + "!");
        return null;
    }

    TileBase GetLiquidTile(float liquidLevel, float aboveLiquidLevel, TileBase prevTile) {
        if (aboveLiquidLevel > minRenderMass) {
            return GetLiquidTile(liquidLevel, aboveLiquidLevel);
        }

        for (int i = 0; i < liquidStates.Count; i++) {
            LiquidTileState lqs = liquidStates[i];
            if (liquidLevel >= lqs.minLiquidLevel && liquidLevel <= lqs.maxLiquidLevel && lqs.stateTile == prevTile) {
                return prevTile;
            }
        }

        return GetLiquidTile(liquidLevel, aboveLiquidLevel);
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
                if (GetChunkVal(chunkDirtyTiles, x, y) == false)
                    continue;
                
                float liquidLevel = GetChunkVal(chunkLiquidMasses, x, y);
                float aboveLiquidLevel = GetChunkVal(chunkLiquidMasses, x, y+1);

                Vector3Int tilePos = new Vector3Int(x-chunkPos.x, y-chunkPos.y, 0);

                if (y == worldHeight) {
                    liquidTilemap.SetTile(tilePos, GetLiquidTile(liquidLevel, 0, liquidTilemap.GetTile(tilePos)));
                } else {
                    liquidTilemap.SetTile(tilePos, GetLiquidTile(liquidLevel, aboveLiquidLevel, liquidTilemap.GetTile(tilePos)));
                }
                if (aboveLiquidLevel <= 0) {
                    SetChunkVal(chunkDirtyTiles, x, y, false);
                }
            }
        }
    }

    void LiquidSimulation(int numIterations = 1) {
        simulating = true;
        /* int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight(); */

        for (int i = 0; i < chunksToUpdate.Count; i++) {
            int chunk = chunksToUpdate[i];

            bool newlySimulatedChunk = chunksStartingUpdate.Contains(chunk);

            ChunkLiquidSimulationStep(chunkLiquidMasses, updatedChunkLiquidMasses, chunk, newlySimulatedChunk);
            if (newlySimulatedChunk) {
                chunksStartingUpdate.Remove(chunk);
            }
        }

        for (int i = 0; i < chunksToUpdate.Count; i++) {
            int chunk = chunksToUpdate[i];
            for (int x = 0; x < WorldController.chunkSize; x++) {
                for (int y = 0; y < WorldController.chunkSize; y++) {
                    SetChunkVal(chunkLiquidMasses, chunk, x, y, GetChunkVal(updatedChunkLiquidMasses, chunk, x, y));
                }
            }
        }

        /* if (numIterations % 2 != 0) {
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    liquidMass[x, y] = updatedLiquidMass[x, y];
                }
            }
        } */

        /* for (int x = 0; x < worldWidth+2; x++) {
            liquidMass[x, 0] = 0;
            liquidMass[x, worldHeight + 1] = 0;
        }
        for (int y = 0; y < worldHeight+1; y++) {
            liquidMass[0, y] = 0;
            liquidMass[worldWidth+1, y] = 0;
        } */
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
                        flow = GetStableStateB(remainingMass + sourceMass[x-1, y]) - sourceMass[x-1, y];
                        if (flow > minFlow) {
                            flow *= 0.25f;
                        }
                        flow = Mathf.Clamp(flow, 0, Mathf.Min(maxSpeed, remainingMass));
                        
                        destinationMass[x, y] -= flow;
                        destinationMass[x-1, y] += flow;
                        remainingMass -= flow;

                        clampCalls++;
                        minCalls++;
                        methodCalls++;

                        dirtyTiles[x, y] = true;
                        dirtyTiles[x-1, y] = true;
                    }

                    if (remainingMass <= 0) {
                        continue;
                    }

                    if (world_fg[x+1, y] == "air") {
                        flow = GetStableStateB(remainingMass + sourceMass[x+1, y]) - sourceMass[x+1, y];
                        if (flow > minFlow) {
                            flow *= 0.25f;
                        }
                        flow = Mathf.Clamp(flow, 0, Mathf.Min(maxSpeed, remainingMass));
                        
                        destinationMass[x, y] -= flow;
                        destinationMass[x+1, y] += flow;
                        remainingMass -= flow;

                        clampCalls++;
                        minCalls++;
                        methodCalls++;

                        dirtyTiles[x, y] = true;
                        dirtyTiles[x+1, y] = true;
                    }

                    if (remainingMass <= 0) {
                        continue;
                    }

                    if (world_fg[x, y+1] == "air") {
                        flow = remainingMass - GetStableStateB(remainingMass + sourceMass[x, y+1]);
                        if (flow > minFlow) {
                            flow *= 0.25f;
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

    float GetLiquidMass(Dictionary<int, float[,]> sourceMass, int x, int y) {
        return GetChunkVal(sourceMass, x, y);
    }

    public int SetLiquidMass(int x, int y, float newVal) {
        //SetLiquidMass(updatedChunkLiquidMasses, newVal, x, y);
        return SetLiquidMass(chunkLiquidMasses, newVal, x, y);
    }

    int SetLiquidMass(Dictionary<int, float[,]> destinationMass, float newVal, int x, int y) {
        int chunk = WorldController.GetChunk(x, y);
        StartChunkLiquidSimulation(chunk);
        SetDirtyTiles(x, y, true);
        return SetChunkVal(destinationMass, x, y, newVal);
    }

    int IncrementLiquidMass(Dictionary<int, float[,]> destinationMass, float incVal, int x, int y) {
        float curVal = GetChunkVal(destinationMass, x, y);
        int chunk = WorldController.GetChunk(x, y);
        StartChunkLiquidSimulation(chunk);
        SetDirtyTiles(x, y, true);
        return SetChunkVal(destinationMass, x, y, curVal + incVal);
    }

    int SetDirtyTiles(int x, int y, bool newVal) {
        return SetChunkVal(chunkDirtyTiles, x, y, newVal);
    }

    public void EndChunkLiquidSimulation(int chunk) {
        if (chunksToUpdate.Contains(chunk)) {
            //Debug.Log("Ending chunk sim on chunk " + chunk);
            chunksToUpdate.Remove(chunk);
        }
    }

    public void StartChunkLiquidSimulation(int chunk) {
        if (!chunksToUpdate.Contains(chunk)) {
            //Debug.Log("Starting chunk sim on chunk " + chunk);
            chunksToUpdate.Add(chunk);
            chunksStartingUpdate.Add(chunk);
        }
    }

    void ChunkLiquidSimulationStep(Dictionary<int, float[,]> srcChunkLiquidMasses, Dictionary<int, float[,]> dstChunkLiquidMasses, int chunk, bool startStep = false) {
        float flow = 0;
        float remainingMass;

        //int worldWidth = WorldController.GetWorldWidth();
        //int worldHeight = WorldController.GetWorldHeight();

        //for (int curIteration = 0; curIteration < numIterations; curIteration++) {
            //sourceMass = liquidMass;//curIteration % 2 == 0 ? liquidMass : updatedLiquidMass;
            //destinationMass = updatedLiquidMass;//curIteration % 2 == 0 ? updatedLiquidMass : liquidMass;

        Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);

        float totalFlow = 0;

        for (int x = chunkPos.x; x < chunkPos.x + WorldController.chunkSize; x++) {
            for (int y = chunkPos.y; y < chunkPos.y + WorldController.chunkSize; y++) {
                totalIterations++;

                if (WorldController.Instance.GetTile(x, y) != "air" || x <= 0 || x >= WorldController.GetWorldWidth() || y <= 0 || y >= WorldController.GetWorldHeight()) {
                    continue;
                }

                flow = 0;
                remainingMass = GetChunkVal(srcChunkLiquidMasses, x, y);//sourceMass[x, y];
                if (remainingMass <= 0) {
                    continue;
                }

                if (WorldController.Instance.GetTile(x, y-1)/*world_fg[x, y-1]*/ == "air") {
                    flow = GetStableStateB(remainingMass + GetChunkVal(srcChunkLiquidMasses, x, y-1)) - GetChunkVal(srcChunkLiquidMasses, x, y-1);
                    if (flow > minFlow) {
                        flow *= 0.5f;
                    }
                    flow = Mathf.Clamp(flow, 0, Mathf.Min(maxSpeed, remainingMass));
                    totalFlow += flow;
                    
                    IncrementLiquidMass(dstChunkLiquidMasses, -flow, x, y);//destinationMass[x, y] -= flow;
                    IncrementLiquidMass(dstChunkLiquidMasses, flow, x, y-1);//destinationMass[x, y-1] += flow;
                    remainingMass -= flow;

                    clampCalls++;
                    minCalls++;
                    methodCalls++;

                    SetDirtyTiles(x, y, true);//dirtyTiles[x, y] = true;
                    SetDirtyTiles(x, y-1, true);//dirtyTiles[x, y-1] = true;
                }

                if (remainingMass <= 0) {
                    continue;
                }

                if (WorldController.Instance.GetTile(x-1, y)/*world_fg[x-1, y]*/ == "air") {
                    //flow = GetStableStateB(remainingMass + GetChunkVal(srcChunkLiquidMasses, x-1, y))/2 - GetChunkVal(srcChunkLiquidMasses, x-1, y);
                    flow = (GetChunkVal(srcChunkLiquidMasses, x, y) - GetChunkVal(srcChunkLiquidMasses, x-1, y))/4;
                    if (flow > minFlow) {
                        flow *= 0.5f;
                    }
                    flow = Mathf.Clamp(flow, 0, remainingMass);//Mathf.Min(maxSpeed, remainingMass));
                    totalFlow += flow;
                    
                    IncrementLiquidMass(dstChunkLiquidMasses, -flow, x, y);//destinationMass[x, y] -= flow;
                    IncrementLiquidMass(dstChunkLiquidMasses, flow, x-1, y);//destinationMass[x-1, y] += flow;
                    remainingMass -= flow;

                    clampCalls++;
                    minCalls++;
                    methodCalls++;

                    SetDirtyTiles(x, y, true);//dirtyTiles[x, y] = true;
                    SetDirtyTiles(x-1, y, true);//dirtyTiles[x-1, y] = true;
                }

                if (remainingMass <= 0) {
                    continue;
                }

                if (WorldController.Instance.GetTile(x+1, y)/* world_fg[x+1, y] */ == "air") {
                    //flow = GetStableStateB(remainingMass + GetChunkVal(srcChunkLiquidMasses, x+1, y))/2 - GetChunkVal(srcChunkLiquidMasses, x+1, y);
                    flow = (GetChunkVal(srcChunkLiquidMasses, x, y) - GetChunkVal(srcChunkLiquidMasses, x+1, y))/4;
                    if (flow > minFlow) {
                        flow *= 0.5f;
                    }
                    flow = Mathf.Clamp(flow, 0, remainingMass);//Mathf.Min(maxSpeed, remainingMass));
                    totalFlow += flow;
                    
                    IncrementLiquidMass(dstChunkLiquidMasses, -flow, x, y);//destinationMass[x, y] -= flow;
                    IncrementLiquidMass(dstChunkLiquidMasses, flow, x+1, y);//destinationMass[x+1, y] += flow;
                    remainingMass -= flow;

                    clampCalls++;
                    minCalls++;
                    methodCalls++;

                    SetDirtyTiles(x, y, true);//dirtyTiles[x, y] = true;
                    SetDirtyTiles(x+1, y, true);//dirtyTiles[x+1, y] = true;
                }

                if (remainingMass <= 0) {
                    continue;
                }

                if (WorldController.Instance.GetTile(x, y+1)/* world_fg[x, y+1] */ == "air") {
                    flow = remainingMass - GetStableStateB(remainingMass + GetChunkVal(srcChunkLiquidMasses, x, y+1));
                    if (flow > minFlow) {
                        flow *= 0.25f;
                    }
                    flow = Mathf.Clamp(flow, 0, Mathf.Min(maxSpeed, remainingMass));
                    totalFlow += flow;
                    
                    IncrementLiquidMass(dstChunkLiquidMasses, -flow, x, y);//destinationMass[x, y] -= flow;
                    IncrementLiquidMass(dstChunkLiquidMasses, flow, x, y+1);//destinationMass[x, y+1] += flow;
                    remainingMass -= flow;

                    clampCalls++;
                    minCalls++;
                    methodCalls++;

                    SetDirtyTiles(x, y, true);//dirtyTiles[x, y] = true;
                    SetDirtyTiles(x, y+1, true);//dirtyTiles[x, y+1] = true;
                }

            }
        }

        //Debug.Log("Chunk " + chunk + " flow: " + totalFlow);

        if (totalFlow < .001f && !startStep) {
            EndChunkLiquidSimulation(chunk);
        }

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
