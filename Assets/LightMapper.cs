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

enum LightValueCheck {
    GreaterThan,
    LessThan,
    Unequal
}

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(TileManager))]
public class LightMapper : MonoBehaviour
{
    private WorldController wCon;
    private WorldRenderer wRend;
    private TileManager rtm;

    public float minLightValue = 0.05f;
    public float tileAttenuation = 0.5f;
    public float airAttenuation = 0.9f;

    public int maxLightSteps = 10;
    public int maxLightChecks = 1000;
    [SerializeField] int curLightChecks = 0;

    int[,] world;
    float[,] light_vals;

    List<int> lightSources = new List<int>();
    Dictionary<int, bool> lightSourceDict = new Dictionary<int, bool>();
    List<int> sampledTiles = new List<int>();
    Dictionary<int, bool> chunksToUpdate = new Dictionary<int, bool>();

    int numDebugMessages;
    int maxDebugMessages = 25;

    public Texture2D lightmap_tex;
    public Material lightmap_mat;

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

    void Update() {
        
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
                pixels[currentPixel] = new Color(0, 0, 0, (1-light_vals[chunkPos.x+x, chunkPos.y+y]));
                currentPixel++;
            }
        }

        lightmap_mat = new Material(Shader.Find("Unlit/Transparent"));
        lightmap_tex = new Texture2D(WorldController.chunkSize, WorldController.chunkSize, TextureFormat.RGBAFloat, false);
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
                pixels[currentPixel] = new Color(0, 0, 0, (1-light_vals[x,y])*1f);
                currentPixel++;
            }
        }

        lightmap_tex = new Texture2D(light_vals.GetUpperBound(0), light_vals.GetUpperBound(1), TextureFormat.RGBAFloat, false);
        lightmap_tex.alphaIsTransparency = true;
        lightmap_tex.filterMode = FilterMode.Point;
        lightmap_tex.SetPixels(pixels);
        lightmap_tex.Apply();
        lightmap_mat.SetTexture("_MainTex", lightmap_tex);
    }

    public void SampleAllLights(int[,] world) {
        InitializeWorld();
        light_vals = InitializeLightValues(world);

        numDebugMessages = 0;
        
        foreach (int lightSource in lightSources) {
            Vector2Int light_coords = WorldCollider.UnhashInt(lightSource);
            SampleNewTile(light_coords.x, light_coords.y, LightValueCheck.GreaterThan);
            sampledTiles.Clear();
        }
    }

    public void SampleUpdatedTile(int[,] world, int x, int y, Tile newTile) {
        this.world = world;

        if (newTile.lightVal > minLightValue) {
            Debug.Log("new light");
            if (!lightSources.Contains(WorldCollider.HashableInt(x, y))) {
                light_vals[x, y] = newTile.lightVal;
                lightSources.Add(WorldCollider.HashableInt(x, y));
                lightSourceDict[WorldCollider.HashableInt(x, y)] = true;
                SampleNewTile(x, y, LightValueCheck.GreaterThan);
            }
        } else {
            if (lightSources.Contains(WorldCollider.HashableInt(x, y))) {
                light_vals[x, y] = 0;
                lightSources.Remove(WorldCollider.HashableInt(x, y));
                lightSourceDict.Remove(WorldCollider.HashableInt(x, y));
                SampleNewTile(x, y, LightValueCheck.LessThan);
            } else {
                SampleNewTile(x, y, LightValueCheck.GreaterThan);
            }
        }

        foreach (int chunk in chunksToUpdate.Keys) {
            UpdateChunkLightmap(chunk);
        }
        chunksToUpdate.Clear();
    }


    //When removing a single block, resample it, check neighbors to see if they're dimmer (after attenuation) than current tile and then sample them
    void SampleNewTile(int x, int y, LightValueCheck compareOp) {
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
                float potentialNewLightVal = light_vals[x, y] * (isFilledTile ? tileAttenuation : airAttenuation);

                switch (compareOp) {
                    case LightValueCheck.GreaterThan:
                    if (potentialNewLightVal > light_vals[x+i, y+j])
                        SampleNewTile(x+i, y+j, compareOp);
                    break;
                    case LightValueCheck.LessThan:
                    if (potentialNewLightVal < light_vals[x+i, y+j] && potentialNewLightVal > minLightValue)
                        SampleNewTile(x+i, y+j, compareOp);
                    break;
                    case LightValueCheck.Unequal:
                    if (potentialNewLightVal != light_vals[x+i, y+j])
                        SampleNewTile(x+i, y+j, compareOp);
                    break;
                    default:
                    return;
                }
                
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
                Tile tile = rtm.GetTile(tileIndex);
                float lightVal = tile.lightVal;
                if (lightVal > minLightValue) {
                    lightSources.Add(WorldCollider.HashableInt(x, y));
                    lightSourceDict[WorldCollider.HashableInt(x, y)] = true;
                }
                light_vals[x, y] = lightVal;
            }
        }

        return light_vals;
    }

    void EvaluateTileLight(int x, int y) {
        bool isFilledTile = wCon.isTile(x, y);
        float attenuation = isFilledTile ? tileAttenuation : airAttenuation;
        float newLightVal = GetMaxNeighborLight(x, y) * attenuation;
        if (newLightVal > minLightValue)
            light_vals[x, y] = newLightVal;
        else
            light_vals[x, y] = 0;
        int tileHash = WorldCollider.HashableInt(x, y);
        sampledTiles.Add(tileHash);
        if (numDebugMessages <= maxDebugMessages) {
            numDebugMessages++;
            Debug.Log("Tile (" + x + ", " + y + ") light val: " + light_vals[x, y]);
        }
    }

    float GetMaxNeighborLight(int x, int y) {
        float maxLightVal = 0;
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
