using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class DefaultValDict {
    public static TValue GetValueOrDefault<TKey, TValue>
    (this IDictionary<TKey, TValue> dictionary, 
     TKey key,
     TValue defaultValue)
    {
        TValue value;
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }
}

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(TileManager))]
public class LightMapper : MonoBehaviour
{
    private WorldController wCon;
    private WorldRenderer wRend;
    private TileManager rtm;

    public float minLightValue = 0.05f;
    public float tileAttenuation = 0.4f;
    public float airAttenuation = 0.6f;
    public float attenuationCurveMult = 2f;

    public int skyHeight = 32;

    public int maxLightRadius = 10;

    public int maxLightSteps = 10;
    public int maxLightChecks = 1000;
    [SerializeField] int curLightChecks = 0;

    int[,] world;
    float[,] light_vals;

    List<int> lightSources = new List<int>();
    Dictionary<int, bool> lightSourceDict = new Dictionary<int, bool>();
    List<int> sampledTiles = new List<int>();
    Dictionary<int,int> subtractedTiles = new Dictionary<int, int>();
    Dictionary<int, bool> chunksToUpdate = new Dictionary<int, bool>();

    Dictionary<int, int> tileCheckCount = new Dictionary<int, int>();

    int numDebugMessages;
    int maxDebugMessages = 25;

    public Shader lightmap_shader;

    private static readonly int[,] RecursiveDirections =
    {
        {-1, 0}, // 0 (Left)
        {0, 1}, // 1 (Up)
        {1, 0}, // 2 (Right)
        {0, -1}, // 3 (Down)
    };

    void Start() {
        wCon = GetComponent<WorldController>();
        wRend = GetComponent<WorldRenderer>();
        rtm = GetComponent<TileManager>();

        /*if (airAttenuation >= 1) {
            airAttenuation = .95f;
        }
        if (1/(1-airAttenuation) > maxLightSteps) {
            airAttenuation = (1-(1/maxLightSteps));
            Debug.Log("Air Attenuation too high! Light will carry past max given steps! Resetting it to " + airAttenuation + "!");
        }*/
    }

    /*
    1. Fill light value array with only lights and their corresponding values
    2. Get locations of starting lights (to skip in sampling)
    3. Go to each light and sample all possible neighbors

    OR
    Repeat 1
    2. Store existing lights
    3. Go to each light and sample all possible neighbors (don't sample existing lights)
    4. Each light, reset sampled tiles
    
     */

    public void InitializeWorld() {
        world = wCon.GetWorld();
        light_vals = WorldGenerator.GenerateArray(world.GetUpperBound(0)+1, world.GetUpperBound(1)+1);
    }

    void RerenderLight() {
        light_vals = WorldGenerator.GenerateArray(world.GetUpperBound(0)+1, world.GetUpperBound(1)+1);
    }

    float GetAttenuatedLightVal(float val, float attenuation) {
        float adjustedAttenuation = attenuationCurveMult*attenuation/val;
        float newVal = Mathf.Max(minLightValue, val-adjustedAttenuation);
        return newVal;
    }

    List<Vector2Int> GetTilesInRadius(int x, int y) {
        List<Vector2Int> nearbyTiles = new List<Vector2Int>();

        for (int i = -maxLightRadius; i <= maxLightRadius; i++) {
            if (x+i < world.GetLowerBound(0) || x+i > world.GetUpperBound(0)) {
                continue;
            }
            for (int j = -(maxLightRadius) + Mathf.Abs(i); j <= (maxLightRadius) - Mathf.Abs(i); j++) {
                if (y+j < world.GetLowerBound(1) || y+j > world.GetUpperBound(1)) {
                    continue;
                }
                Vector2Int newTile = new Vector2Int(x+i, y+j);
                nearbyTiles.Add(newTile);
            }
        }

        return nearbyTiles;
    }

    List<int> FindAllNearbyLightSources(int x, int y) {
        List<int> nearbyLightSources = new List<int>();
        
        for (int i = -maxLightRadius*3; i <= maxLightRadius*3; i++) {
            for (int j = -(maxLightRadius*3) + Mathf.Abs(i); j <= (maxLightRadius*3) - Mathf.Abs(i); j++) {
                int tileHash = WorldCollider.HashableInt(x+i, y+j);
                if (lightSources.Contains(tileHash)) {
                    nearbyLightSources.Add(tileHash);
                }
            }
        }

        return nearbyLightSources;
    }

    float[,] GetChunkLightVals(int chunk) {
        Vector2Int chunkPos = wCon.GetChunkPosition(chunk);
        float[,] chunkLightVals = new float[WorldController.chunkSize, WorldController.chunkSize];

        for (int x = 0; x < WorldController.chunkSize; x++) {
            for (int y = 0; y < WorldController.chunkSize; y++) {
                chunkLightVals[x, y] = light_vals[chunkPos.x+x, chunkPos.y+y];
            }
        }

        return chunkLightVals;
    }

    public void UpdateChunkLightVals(int chunk, int[,] world) {
        
    }

    public void UpdateChunkLightmap(int chunk) {
        Vector2Int chunkPos = wCon.GetChunkPosition(chunk);

        Color[] pixels = new Color[WorldController.chunkSize * WorldController.chunkSize];
        int currentPixel = 0;
        for (int y = 0; y < WorldController.chunkSize; y++) {
            for (int x = 0; x < WorldController.chunkSize; x++) {
                float inverseLightVal = 1-light_vals[chunkPos.x+x, chunkPos.y+y];
                //pixels[currentPixel] = new Color(inverseLightVal, inverseLightVal, inverseLightVal, 1);
                pixels[currentPixel] = new Color(0, 0, 0, (1-light_vals[chunkPos.x+x, chunkPos.y+y]));
                currentPixel++;
            }
        }

        Material lightmap_mat = new Material(lightmap_shader);//"Unlit/Transparent"));
        Texture2D lightmap_tex = new Texture2D(WorldController.chunkSize, WorldController.chunkSize, TextureFormat.RGBAFloat, false);
        lightmap_tex.alphaIsTransparency = true;
        lightmap_tex.filterMode = FilterMode.Point;
        lightmap_tex.SetPixels(pixels);
        lightmap_tex.Apply();
        lightmap_mat.SetTexture("_MainTex", lightmap_tex);
        wRend.RenderChunkLightmap(chunk, lightmap_mat);
    }

    [ContextMenu("Generate All Chunk Lightmaps")]
    public void GenerateAllLightmapTextures() {
        int chunkCount = wCon.GetChunkCount();
        for (int chunk = 0; chunk < chunkCount; chunk++) {
            UpdateChunkLightmap(chunk);
        }
    }

    [ContextMenu("Generate Lightmap Texture")]
    public void GenerateLightmapTexture() {
        Color[] pixels = new Color[light_vals.Length];
        int currentPixel = 0;
        for (int y = 0; y < light_vals.GetUpperBound(1); y++) {
            for (int x = 0; x < light_vals.GetUpperBound(0); x++) {
                pixels[currentPixel] = Color.white * (1-light_vals[x, y]);
                //pixels[currentPixel] = new Color(0, 0, 0, (1-light_vals[x,y])*1f);
                currentPixel++;
            }
        }

        Material lightmap_mat = new Material(lightmap_shader);
        Texture2D lightmap_tex = new Texture2D(light_vals.GetUpperBound(0), light_vals.GetUpperBound(1), TextureFormat.RGBAFloat, false);
        lightmap_tex.alphaIsTransparency = true;
        lightmap_tex.filterMode = FilterMode.Point;
        lightmap_tex.SetPixels(pixels);
        lightmap_tex.Apply();
        lightmap_mat.SetTexture("_MainTex", lightmap_tex);
    }

    [ContextMenu("Sample All Lights")]
    public void SampleAllLights() {
        light_vals = ReinitializeLightValues(world);
        
        foreach (int lightSource in lightSources) {
            Vector2Int light_coords = WorldCollider.UnhashInt(lightSource);
            SampleLight(light_coords.x, light_coords.y);
        }
        GenerateAllLightmapTextures();
    }

    public void SampleAllLights(int[,] world) {
        InitializeWorld();
        light_vals = InitializeLightValues(world);

        numDebugMessages = 0;
        
        foreach (int lightSource in lightSources) {
            Vector2Int light_coords = WorldCollider.UnhashInt(lightSource);
            SampleLight(light_coords.x, light_coords.y);
        }
        GenerateAllLightmapTextures();
    }

    public void SampleChunks(List<int> chunksToSample) {
        foreach (int chunk in chunksToSample) {
            if (chunk < 0 || chunk >= wCon.GetChunkCount()) {
                continue;
            }
            else {
                Debug.Log("Resampling chunk " + chunk + "!");
                SampleChunk(chunk);
            }
        }
    }

    void SampleChunk(int chunk) {
        if (curLightChecks > maxLightChecks) {
            return;
        }
        curLightChecks++;
        List<int> lightSourcesInChunk = new List<int>();
        
        Vector2Int chunkPos = wCon.GetChunkPosition(chunk);
        for (int x = 0; x < WorldController.chunkSize; x++) {
            for (int y = 0; y < WorldController.chunkSize; y++) {
                int cx = x + chunkPos.x;
                int cy = y + chunkPos.y;

                if (cx < world.GetLowerBound(0) || cx > world.GetUpperBound(0) || cy < world.GetLowerBound(1) || cy > world.GetUpperBound(1)) {
                    Debug.Log("Tile " + cx + ", " + cy + " out of array!");
                    continue;
                }

                int tileHash = WorldCollider.HashableInt(cx, cy);
                if (lightSources.Contains(tileHash)) {
                    lightSourcesInChunk.Add(tileHash);
                    light_vals[cx, cy] = 1;
                } else {
                    light_vals[cx, cy] = 0;
                }
            }
        }

        foreach (int lightSourceHash in lightSourcesInChunk) {
            Vector2Int lightSourcePos = WorldCollider.UnhashInt(lightSourceHash);
            SampleLight(lightSourcePos.x, lightSourcePos.y);
        }

        UpdateChunkLightmap(chunk);
    }

    public void HandleNewBlock(int x, int y, TileData newTile) {
        world = wCon.GetWorld();
        curLightChecks = 0;

        if (newTile.lightVal > minLightValue) {
            if (!lightSources.Contains(WorldCollider.HashableInt(x, y))) {
                light_vals[x, y] = 1;//newTile.lightVal;
                lightSources.Add(WorldCollider.HashableInt(x, y));
            }
        } else {
            if (lightSources.Contains(WorldCollider.HashableInt(x, y))) {
                int tileHash = WorldCollider.HashableInt(x, y);
                if (!lightSources.Contains(tileHash)) {
                    return;
                }

                lightSources.Remove(tileHash);

                light_vals[x, y] = 0;
            }
        }
    }

    public void SampleUpdatedTile(int[,] newWorld, int x, int y, TileData newTile) {
        world = newWorld;
        curLightChecks = 0;

        if (newTile.lightVal > minLightValue) {
            if (!lightSources.Contains(WorldCollider.HashableInt(x, y))) {
                light_vals[x, y] = newTile.lightVal;
                lightSources.Add(WorldCollider.HashableInt(x, y));
                lightSourceDict[WorldCollider.HashableInt(x, y)] = true;
                SampleLight(x, y);
            }
        } else {
            if (lightSources.Contains(WorldCollider.HashableInt(x, y))) {
                RemoveLightSource(x, y);
            } else {
                SampleTile(x, y);//SampleLight(x, y);
            }
        }

        foreach (int chunk in chunksToUpdate.Keys) {
            UpdateChunkLightmap(chunk);
        }
        chunksToUpdate.Clear();
    }

    void SampleTile(int x, int y) {
        light_vals[x, y] = 0;

        List<int> lightsToResample = FindAllNearbyLightSources(x, y);
        foreach (int lightSource in lightsToResample) {
            Vector2Int light_coords = WorldCollider.UnhashInt(lightSource);
            light_vals[light_coords.x, light_coords.y] = 1;
            for (int dir = 0; dir <= RecursiveDirections.GetUpperBound(0); dir++) {
                int i = RecursiveDirections[dir,0];
                int j = RecursiveDirections[dir,1];
                SubtractLight(light_coords.x+i, light_coords.y+j, 1, dir, 1, 0);
            }
        }

        foreach (int lightSource in lightsToResample) {
            Vector2Int light_coords = WorldCollider.UnhashInt(lightSource);
            light_vals[light_coords.x, light_coords.y] = 1;
            SampleLight(light_coords.x, light_coords.y);
            //Debug.Log("Resampling light at " + light_coords.x + ", " + light_coords.y);            
        }
    }

    void RemoveLightSource(int x, int y) {
        int tileHash = WorldCollider.HashableInt(x, y);
        if (!lightSources.Contains(tileHash)) {
            return;
        }

        lightSources.Remove(tileHash);

        light_vals[x, y] = 0;

        Debug.Log("Removing light source at " + x + ", " + y);

        tileCheckCount.Clear();

        for (int dir = 0; dir <= RecursiveDirections.GetUpperBound(0); dir++) {
            int i = RecursiveDirections[dir,0];
            int j = RecursiveDirections[dir,1];
            SubtractLight(x+i, y+j, 1, dir, 1, 0);
        }

        //List<Vector2Int> nearbyTiles = GetTilesInRadius(x, y);
        /*foreach (Vector2Int tile in nearbyTiles) {
            tileHash = WorldCollider.HashableInt(tile.x, tile.y);
            if (lightSources.Contains(tileHash)) {
                continue;
            }
            light_vals[tile.x, tile.y] = 0;
        }*/

        Debug.Log(curLightChecks);

        curLightChecks = 0;

        //light_vals = ReinitializeLightValues(world);

        List<int> lightsToResample = FindAllNearbyLightSources(x, y);
        foreach (int lightSource in lightsToResample) {
            Vector2Int light_coords = WorldCollider.UnhashInt(lightSource);
            light_vals[light_coords.x, light_coords.y] = 1;
            for (int dir = 0; dir <= RecursiveDirections.GetUpperBound(0); dir++) {
                int i = RecursiveDirections[dir,0];
                int j = RecursiveDirections[dir,1];
                SubtractLight(light_coords.x+i, light_coords.y+j, 1, dir, 1, 0);
            }
        }

        foreach (int lightSource in lightsToResample) {
            Vector2Int light_coords = WorldCollider.UnhashInt(lightSource);
            light_vals[light_coords.x, light_coords.y] = 1;
            SampleLight(light_coords.x, light_coords.y);
            //Debug.Log("Resampling light at " + light_coords.x + ", " + light_coords.y);            
        }

        SampleLight(x, y);

        //EvaluateTileLight(x, y);
    }

    void SubtractLight(int x, int y, float amount, int neighborDir, int neighborLevel, int auxLevel) {
        if (curLightChecks > maxLightChecks) {
            return;
        }
        curLightChecks++;

        int tileHash = WorldCollider.HashableInt(x, y);

        bool isFilledTile = wCon.isTile(x, y);
        float newAmount = GetAttenuatedLightVal(amount, (isFilledTile ? tileAttenuation : airAttenuation));

        if (newAmount < minLightValue || neighborLevel + auxLevel > maxLightRadius) {
            return;
        }

        if (!lightSources.Contains(tileHash)) {
            int chunk = wCon.GetChunk(x, y);
            chunksToUpdate[chunk] = true;

            float oldVal = light_vals[x, y];
            //if (oldVal-newAmount < 0) {
                light_vals[x, y] = 0;
            //} else {
            //    light_vals[x, y] -= newAmount;
            //}

            //Debug.Log("Subtracting " + newAmount + " from tile " + x + ", " + y + " (light val: " + oldVal + ") with new light val: " + light_vals[x, y]);
        }

        if (auxLevel >= neighborLevel) {
            return;
        }
        
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i == 0 & j == 0)
                    continue;

                if (((i == -1 || i == 1) && (j != 0)) || ((j == -1 || j == 1) && (i != 0))) {
                    continue;
                }
                
                if (x+i < world.GetLowerBound(0) || x+i > world.GetUpperBound(0) || y+j < world.GetLowerBound(1) || y+j > world.GetUpperBound(1)) {
                    continue;
                }

                if (RecursiveDirections[neighborDir, 0] == i && RecursiveDirections[neighborDir, 1] == j && auxLevel == 0) {
                    SubtractLight(x+i, y+j, newAmount, neighborDir, neighborLevel+1, auxLevel);
                }

                else if ((-RecursiveDirections[neighborDir, 0] == i && -RecursiveDirections[neighborDir, 1] == j)) {
                    continue;
                }

                else if ((RecursiveDirections[neighborDir, 0] != i && RecursiveDirections[neighborDir, 1] != j)){
                    SubtractLight(x+i, y+j, newAmount, neighborDir, neighborLevel, auxLevel+1);
                }
            }
        }
    }

    void SampleLight(int x, int y) {
        for (int dir = 0; dir <= RecursiveDirections.GetUpperBound(0); dir++) {
            int i = RecursiveDirections[dir,0];
            int j = RecursiveDirections[dir,1];
            if (x+i < world.GetLowerBound(0) || x+i > world.GetUpperBound(0) || y+j < world.GetLowerBound(1) || y+j > world.GetUpperBound(1))
                continue; 
            SampleTile(x+i, y+j, dir, 1, 0);
        }
    }

    void SampleTile(int x, int y, int neighborDir, int neighborLevel, int auxLevel) {
        if (curLightChecks > maxLightChecks) {
            //Debug.Log("Hit max light checks");
            return;
        }
        curLightChecks++;

        if (neighborLevel + auxLevel > maxLightRadius) {
            //return;
        }

        int tileHash = WorldCollider.HashableInt(x, y);
        if (!lightSources.Contains(tileHash)) {
            EvaluateTileLight(x, y);
            int chunk = wCon.GetChunk(x, y);
            chunksToUpdate[chunk] = true;
            if (light_vals[x, y] < minLightValue) {
                return;
            }
        }

        int skippedNeighbors = 0;
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i == 0 & j == 0)
                    continue;

                if (((i == -1 || i == 1) && (j != 0)) || ((j == -1 || j == 1) && (i != 0))) {
                    continue;
                }
                
                if (x+i < world.GetLowerBound(0) || x+i > world.GetUpperBound(0) || y+j < world.GetLowerBound(1) || y+j > world.GetUpperBound(1)) {
                    skippedNeighbors++;
                    if (skippedNeighbors >= 4) {
                        //Debug.Log("Reached corner");
                        return;
                    }
                    continue;
                }

                if (RecursiveDirections[neighborDir, 0] == i && RecursiveDirections[neighborDir, 1] == j && auxLevel == 0) {
                    bool isFilledTile = wCon.isTile(x+i, y+j);
                    float potentialNewLightVal = GetAttenuatedLightVal(light_vals[x, y], (isFilledTile ? tileAttenuation : airAttenuation));

                    if (potentialNewLightVal >= light_vals[x+i, y+j])
                        SampleTile(x+i, y+j, neighborDir, neighborLevel+1, auxLevel);
                }

                else if ((-RecursiveDirections[neighborDir, 0] == i && -RecursiveDirections[neighborDir, 1] == j)) {
                    skippedNeighbors++;
                    if (skippedNeighbors >= 4) {
                        //Debug.Log("Reached corner");
                        return;
                    }
                    continue;
                }

                else if ((RecursiveDirections[neighborDir, 0] != i && RecursiveDirections[neighborDir, 1] != j)) {
                    if (auxLevel > neighborLevel) {
                        return;
                    }

                    bool isFilledTile = wCon.isTile(x+i, y+j);
                    float potentialNewLightVal = GetAttenuatedLightVal(light_vals[x, y], (isFilledTile ? tileAttenuation : airAttenuation));

                    if (potentialNewLightVal > light_vals[x+i, y+j])
                        SampleTile(x+i, y+j, neighborDir, neighborLevel, auxLevel+1);
                }
            }
        }
    }

    //When removing a single block, resample it, check neighbors to see if they're dimmer (after attenuation) than current tile and then sample them
    void SampleNewTile(int x, int y) {
        if (curLightChecks > maxLightChecks) {
            //Debug.Log("Hit max light checks");
            return;
        }

        curLightChecks++;

        int tileHash = WorldCollider.HashableInt(x, y);
        if (!lightSources.Contains(tileHash)) {
            EvaluateTileLight(x, y);
            int chunk = wCon.GetChunk(x, y);
            chunksToUpdate[chunk] = true;
            if (light_vals[x, y] < minLightValue) {
                return;
            }
        }

        int curIndex = 0;
        int skippedNeighbors = 0;

        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i == 0 & j == 0)
                    continue;

                if (((i == -1 || i == 1) && (j != 0)) || ((j == -1 || j == 1) && (i != 0))) {
                    continue;
                }
                
                /*if (sampledTiles.Contains(WorldCollider.HashableInt(x+i, y+j))) {
                    skippedNeighbors++;
                    if (skippedNeighbors >= 4) {
                        //Debug.Log("Reached corner");
                        return;
                    }
                    continue;
                }*/
                
                if (x+i < world.GetLowerBound(0) || x+i > world.GetUpperBound(0) || y+j < world.GetLowerBound(1) || y+j > world.GetUpperBound(1)) {
                    skippedNeighbors++;
                    if (skippedNeighbors >= 4) {
                        //Debug.Log("Reached corner");
                        return;
                    }
                    continue;
                }

                bool isFilledTile = wCon.isTile(x+i, y+j);
                float potentialNewLightVal = GetAttenuatedLightVal(light_vals[x, y], (isFilledTile ? tileAttenuation : airAttenuation));

                if (potentialNewLightVal > light_vals[x+i, y+j])
                    SampleLight(x+i, y+j);
                
                curIndex++;
            }
        }
    }

    void SampleTile(int x, int y, bool isLight, int currentStep) {
        if (currentStep > maxLightSteps) {
            //Debug.Log("Hit max recurs steps!");
            return;
        }

        if (curLightChecks > maxLightChecks) {
            //Debug.Log("Hit max light checks");
            return;
        }

        curLightChecks++;
        
        int tileHash = WorldCollider.HashableInt(x, y);
        //if (!DefaultValDict.GetValueOrDefault(lightSourceDict, tileHash, false)) {
        if (!lightSources.Contains(tileHash)) {
        //if (!isLight) {
            EvaluateTileLight(x, y);
            if (light_vals[x, y] < minLightValue) {
                return;
            }
        }

        int curIndex = 0;
        int skippedNeighbors = 0;

        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i == 0 & j == 0)
                    continue;

                if (((i == -1 || i == 1) && (j != 0)) || ((j == -1 || j == 1) && (i != 0))) {
                    continue;
                }
                
                if (sampledTiles.Contains(WorldCollider.HashableInt(x+i, y+j))) {
                    skippedNeighbors++;
                    if (skippedNeighbors >= 4) {
                        //Debug.Log("Reached corner");
                        return;
                    }
                    continue;
                }
                
                if (x+i < world.GetLowerBound(0) || x+i > world.GetUpperBound(0) || y+j < world.GetLowerBound(1) || y+j > world.GetUpperBound(1)) {
                    skippedNeighbors++;
                    if (skippedNeighbors >= 4) {
                        //Debug.Log("Reached corner");
                        return;
                    }
                    continue;
                }

                if (curIndex == 0)
                    SampleTile(x+i, y+j, false, currentStep+1);
                else
                    SampleTile(x+i, y+j, false, currentStep+1);
                
                curIndex++;
            }
        }
    }

    float[,] InitializeLightValues(int[,] world) {
        Debug.Log("Initializing light values");
        lightSources.Clear();

        for (int x = 0; x <= world.GetUpperBound(0); x++) {
            for (int y = 0; y <= world.GetUpperBound(1); y++) {
                int tileIndex = world[x, y];
                TileData tile = rtm.GetTile(tileIndex);
                float lightVal = tile.lightVal;
                if (lightVal > minLightValue) {
                    lightSources.Add(WorldCollider.HashableInt(x, y));
                }
                light_vals[x, y] = lightVal;
            }
        }

        return light_vals;
    }

    float[,] ReinitializeLightValues(int[,] world) {
        Debug.Log("Initializing light values");

        for (int x = 0; x <= world.GetUpperBound(0); x++) {
            for (int y = 0; y <= world.GetUpperBound(1); y++) {
                int tileIndex = world[x, y];
                TileData tile = rtm.GetTile(tileIndex);
                float lightVal = tile.lightVal;
                
                light_vals[x, y] = lightVal;
            }
        }

        return light_vals;
    }

    /* 
    float[,] ReinitializeChunkValues(int chunk) {
        Debug.Log("Reinitializing light values");
        Vector2Int chunkPos = wCon.GetChunkPosition(chunk);

        for (int x = 0; x < WorldController.chunkSize; x++) {
            for (int y = 0; y < WorldController.chunkSize; y++) {
                int newX = chunkPos.x + x;
                int newY = chunkPos.y + y;
                int tileIndex = world[newX, newY];
                Tile tile = rtm.GetTile(tileIndex);
                float lightVal = tile.lightVal;
                
                int tileHash = WorldCollider.HashableInt(newX, newY);
                if (lightSources.Contains(tileHash))
                    light_vals[newX, newY] = lightVal;
            }
        }

        return light_vals;
    }*/

    void EvaluateTileLight(int x, int y) {
        bool isFilledTile = wCon.isTile(x, y);
        float attenuation = isFilledTile ? tileAttenuation : airAttenuation;
        float newLightVal = GetAttenuatedLightVal(GetMaxNeighborLight(x, y), (isFilledTile ? tileAttenuation : airAttenuation));
        if (newLightVal > minLightValue)
            light_vals[x, y] = newLightVal;
        else
            light_vals[x, y] = 0;

        if (newLightVal >= .9f) {
            //Debug.Log("Tile " + x + ", " + y + " has light val " + newLightVal);
        }
        int tileHash = WorldCollider.HashableInt(x, y);
        sampledTiles.Add(tileHash);
        if (numDebugMessages <= maxDebugMessages) {
            numDebugMessages++;
            Debug.Log("Tile (" + x + ", " + y + ") light val: " + light_vals[x, y]);
        }
    }

    float GetMaxNeighborLight(int x, int y) {
        float maxLightVal = -Mathf.Infinity;
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i == 0 && j == 0)
                    continue;

                if (((i == -1 || i == 1) && (j != 0)) || ((j == -1 || j == 1) && (i != 0))) {
                    continue;
                }

                if (x+i < world.GetLowerBound(0) || x+i > world.GetUpperBound(0) || y+j < world.GetLowerBound(1) || y+j > world.GetUpperBound(1))
                    continue;

                float curLightVal = light_vals[x+i, y+j];

                /*if (world[x+i, y+j] == 0) {
                    curLightVal = 1;
                }*/

                if (curLightVal <= minLightValue) {
                    continue;
                }

                if (curLightVal > maxLightVal) {
                    maxLightVal = curLightVal;
                }
            }
        }

        return maxLightVal;
    }
}
