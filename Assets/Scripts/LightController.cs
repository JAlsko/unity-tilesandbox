using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(TileRenderer))]
[RequireComponent(typeof(TileController))]
[RequireComponent(typeof(ChunkObjectsHolder))]
public class LightController : Singleton<LightController>
{
    public enum LightingChannelMode
    {
        RED,
        GREEN,
        BLUE
    } 

    private WorldController wCon;
    private TileRenderer wRend;
    private TileController tMgr;
    private ChunkObjectsHolder cObjs;

    private int worldWidth, worldHeight;

    //Primary light value array
    private Color[,] activeLightValues;
    private Color[,] ambientLightValues;
    private int[,] skyTiles;
 
    //Arrays to hold light map textures and materials
    private Texture2D[] chunkActiveLightTexes;
    private Texture2D[] chunkAmbientLightTexes;
    private Texture2D[] chunkBGActiveLightTexes;
    private Material[] chunkActiveLightMats;
    private Material[] chunkAmbientLightMats;
    private Material[] chunkBGActiveLightMats;

    private Dictionary<int, bool> chunksToUpdate = new Dictionary<int, bool>();

    //Queues of light nodes to remove/update
    private Queue<LightNode> activeRemovalQueue, activeUpdateQueue;
    private Queue<LightNode> ambientRemovalQueue, ambientUpdateQueue;

    //List of light sources to re-sample after removing a light
    private List<LightSource> postRemovalUpdates;
    private List<Vector3Int> removalPositions;

    private List<DynamicLightSource> dynamicLights;

    //Sky light color
    public Color skyColor;
    [Range(0f, 1f)]
    public float skyStrength = 1f;

    //Number of blocks/non-blocks that light can travel
    public int blockLightPenetration;
    public int bgLightPenetration;

    //Darkness ratio of background to foreground
    [Range(0f, 1f)]
    public float bgShadowMult;

    //Actual light falloff values computed from tile-number based falloff
    float blockFalloff;
    float bgFalloff;

    //A value to help make a smooth transition from light to no light at the end of falloff
    public float passThreshold;

    public Shader activeLightMapShader;
    public Shader ambientLightMapShader;
    public GameObject lightSourcePrefab;
    public Transform lightSourceParent;

    public Material skyOverlayMat;

    private Dictionary<int, bool> updatedSkyChunks = new Dictionary<int, bool>();

    public void Start() {
        wCon = GetComponent<WorldController>();
        wRend = GetComponent<TileRenderer>();
        tMgr = GetComponent<TileController>();
        cObjs = GetComponent<ChunkObjectsHolder>();

        blockFalloff = 1f/blockLightPenetration;
        bgFalloff = 1f/bgLightPenetration;
    }
    
    /// <summary>
    /// Called when a dynamic light changes position.
    /// Removes old light source and creates a new light source.
    /// </summary>
    /// <param name="light"></param>
    /// <returns></returns>
    public void UpdateDynamicLight(DynamicLightSource light) {
        LightSource oldLightSource = light.GetLightSource();
        RemoveLight(oldLightSource);
        light.RemoveLightSource();
        LightSource newLightSource = light.GetNewLightSource();
        UpdateLight(newLightSource);

        ApplyChunkTextureLightUpdates();
    }

    /// <summary>
    /// Called when a dynamic light disappears.
    /// </summary>
    /// <param name="light"></param>
    /// <returns></returns>
    public void RemoveDynamicLight(DynamicLightSource light) {
        LightSource oldLightSource = light.GetLightSource();
        RemoveLight(oldLightSource);

        ApplyChunkTextureLightUpdates();
    }

    /// <summary>
    /// Initializes lightmap textures/materials, attaches them to chunk objects.
    /// Initializes light value array to black (minus the sky).
    /// </summary>
    /// <returns></returns>
    public void InitializeWorld() {
        worldWidth = WorldController.GetWorldWidth();
        worldHeight = WorldController.GetWorldHeight();

        int chunkSize = WorldController.chunkSize;

        if (activeLightValues == null) {
            activeLightValues = new Color[worldWidth,worldHeight];
        }

        if (ambientLightValues == null) {
            ambientLightValues = new Color[worldWidth,worldHeight];
        }

        skyTiles = new int[worldWidth,worldHeight];

        int chunkCount = WorldController.GetChunkCount();

        chunkActiveLightTexes = new Texture2D[chunkCount];
        chunkAmbientLightTexes = new Texture2D[chunkCount];
        chunkBGActiveLightTexes = new Texture2D[chunkCount];
        chunkActiveLightMats = new Material[chunkCount];
        chunkAmbientLightMats = new Material[chunkCount];
        chunkBGActiveLightMats = new Material[chunkCount];

        for (int chunk = 0; chunk < chunkCount; chunk++) {
            Color[,] baseActiveLightValues = GetColorArray(chunkSize, chunkSize, Color.black, Color.black, true, chunk);  
            Color[,] baseAmbientLightValues = GetColorArray(chunkSize, chunkSize, Color.black, Color.white, true, chunk);  
            Color[] flattenedActiveColors = FlattenColorArray(baseActiveLightValues);
            Color[] flattenedAmbientColors = FlattenColorArray(baseAmbientLightValues);

            Texture2D newActiveLightTex = new Texture2D(chunkSize, chunkSize, TextureFormat.RGBA32, false);
            Texture2D newAmbientLightTex = new Texture2D(chunkSize, chunkSize, TextureFormat.RGBA32, false);
            Texture2D newBGActiveLightTex = new Texture2D(chunkSize, chunkSize, TextureFormat.RGBA32, false);
            Material newActiveLightMat = new Material(activeLightMapShader);
            Material newAmbientLightMat = new Material(ambientLightMapShader);
            Material newBGActiveLightMat = new Material(activeLightMapShader);
            
            newActiveLightTex.filterMode = FilterMode.Point;
            newAmbientLightTex.filterMode = FilterMode.Point;
            newBGActiveLightTex.filterMode = FilterMode.Point;

            newActiveLightTex.SetPixels(flattenedActiveColors);
            newAmbientLightTex.SetPixels(flattenedAmbientColors);
            newBGActiveLightTex.SetPixels(flattenedActiveColors);
            newActiveLightTex.Apply();
            newAmbientLightTex.Apply();
            newBGActiveLightTex.Apply();

            chunkActiveLightTexes[chunk] = newActiveLightTex;
            chunkAmbientLightTexes[chunk] = newAmbientLightTex;
            chunkBGActiveLightTexes[chunk] = newBGActiveLightTex;

            newActiveLightMat.SetTexture("_MainTex", chunkActiveLightTexes[chunk]);
            newAmbientLightMat.SetTexture("_MainTex", chunkAmbientLightTexes[chunk]);
            newAmbientLightMat.SetColor("_Color", skyColor);
            newBGActiveLightMat.SetTexture("_MainTex", chunkBGActiveLightTexes[chunk]);

            GameObject chunkObj = cObjs.GetChunkObject(chunk);
            MeshRenderer fgRenderer = chunkObj.transform.Find("LightMap").GetComponent<MeshRenderer>();
            MeshRenderer skyRenderer = chunkObj.transform.Find("SkylightMap").GetComponent<MeshRenderer>();
            MeshRenderer bgRenderer = chunkObj.transform.Find("BGLightMap").GetComponent<MeshRenderer>();
            MeshRenderer bgSkyRenderer = chunkObj.transform.Find("BGSkylightMap").GetComponent<MeshRenderer>();

            fgRenderer.material = newActiveLightMat;
            skyRenderer.material = newAmbientLightMat;
            bgRenderer.material = newBGActiveLightMat;
            bgSkyRenderer.material = newAmbientLightMat;


            chunkActiveLightMats[chunk] = newActiveLightMat;
            chunkAmbientLightMats[chunk] = newAmbientLightMat;
            chunkBGActiveLightMats[chunk] = newBGActiveLightMat;
        }

        activeUpdateQueue = new Queue<LightNode>();
        activeRemovalQueue = new Queue<LightNode>();
        ambientUpdateQueue = new Queue<LightNode>();
        ambientRemovalQueue = new Queue<LightNode>();
        postRemovalUpdates = new List<LightSource>();
        removalPositions = new List<Vector3Int>();
        dynamicLights = new List<DynamicLightSource>();

        IntializeSkyChunks();
        UpdateSkylight(skyColor);

        HandleNewWorld();
    }

    Color InvertColor(Color startColor) {
        return new Color(1-startColor.r, 1-startColor.g, 1-startColor.b, startColor.a);
    }

    /// <summary>
    /// Updates the sky color and then updates every chunk's sky light map color
    /// </summary>
    /// <param name="newColor"></param>
    /// <returns></returns>
    public void UpdateSkylight(Color newColor) {
        skyColor = newColor;
        int numChunks = WorldController.GetChunkCount();
        for (int chunk = 0; chunk < numChunks; chunk++) {
            chunkAmbientLightMats[chunk].SetColor("_Color", newColor);
        }
        skyOverlayMat.SetColor("_Color", newColor);
    }

    void IntializeSkyChunks() {
        for (int x = 0; x < worldWidth; x++) {
            for (int y = 0; y < worldHeight; y++) {
                skyTiles[x, y] = wCon.isSky(x, y) ? 1 : 0;
            }
        }
    }

    /// <summary>
    /// Returns an empty color array (with the sky at full light) 
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    Color[,] GetColorArray(int width, int height, Color tileColor, Color skyColor, bool isChunk = false, int chunk = 0) {
        Color[,] blackArr = new Color[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (!isChunk) {
                    if (wCon.isSky(x, y)) {
                        blackArr[x, y] = skyColor;
                    } else {
                        blackArr[x, y] = tileColor;
                    }
                } else {
                    Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);
                    if (wCon.isSky(chunkPos.x + x, chunkPos.y + y)) {
                        blackArr[x, y] = skyColor;
                    } else {
                        blackArr[x, y] = tileColor;
                    }
                }
            }
        }

        return blackArr;
    }

    /// <summary>
    /// Flattens a color array for feeding into Texture2D.SetPixels().
    /// </summary>
    /// <param name="colors"></param>
    /// <returns></returns>
    Color[] FlattenColorArray(Color[,] colors) {
        Color[] newArr = new Color[colors.Length];
        int index = 0;
        for (int y = 0; y <= colors.GetUpperBound(1); y++) {
            for (int x = 0; x <= colors.GetUpperBound(0); x++) {
                newArr[index] = colors[x, y];
                index++;
            }
        }

        return newArr;
    }

    /// <summary>
    /// Updates a chunk's lightmap texture without applying it.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="newColor"></param>
    /// <returns></returns>
    void QueueChunkActiveLightTextureUpdate(int x, int y, Color newColor) {
        int chunkToUpdate = WorldController.GetChunk(x, y);
        Vector2Int chunkPos = WorldController.GetChunkPosition(chunkToUpdate);
        chunkActiveLightTexes[chunkToUpdate].SetPixel(x-chunkPos.x, y-chunkPos.y, newColor);
        chunksToUpdate[chunkToUpdate] = true;
    }

    /// <summary>
    /// Updates a chunk's sky lightmap texture without applying it. (Possible optimization: check if texture update is redundant and skip it)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="newColor"></param>
    /// <returns></returns>
    void QueueChunkAmbientLightTextureUpdate(int x, int y, Color newColor) {
        if (wCon.isSky(x, y)) {
            newColor = Color.white;
        }
        
        int chunkToUpdate = WorldController.GetChunk(x, y);
        Vector2Int chunkPos = WorldController.GetChunkPosition(chunkToUpdate);
        chunkAmbientLightTexes[chunkToUpdate].SetPixel(x-chunkPos.x, y-chunkPos.y, newColor);
        chunksToUpdate[chunkToUpdate] = true;
    }

    /// <summary>
    /// Updates a chunk's background lightmap texture without applying it.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="newColor"></param>
    /// <returns></returns>
    void QueueChunkBGActiveLightTextureUpdate(int x, int y, Color newColor) {
        int chunkToUpdate = WorldController.GetChunk(x, y);
        Vector2Int chunkPos = WorldController.GetChunkPosition(chunkToUpdate);
        chunkBGActiveLightTexes[chunkToUpdate].SetPixel(x-chunkPos.x, y-chunkPos.y, newColor);
        chunksToUpdate[chunkToUpdate] = true;
    }

    /// <summary>
    /// Applies all made changes to chunk lightmap textures.
    /// </summary>
    /// <returns></returns>
    void ApplyChunkTextureLightUpdates() {
        foreach (int chunk in chunksToUpdate.Keys) {
            chunkActiveLightTexes[chunk].Apply();
            chunkAmbientLightTexes[chunk].Apply();
            chunkBGActiveLightTexes[chunk].Apply();
        }

        chunksToUpdate.Clear();
    }

    /// <summary>
    /// Iterates through new world and calculates light values for every block.
    /// Generates light sources for relevant blocks and block-sky borders.
    /// </summary>
    /// <returns></returns>
    public void HandleNewWorld() {
        string[,] world_fg = wCon.GetWorld(0);
        string[,] world_bg = wCon.GetWorld(1);
        for (int x = 0; x < worldWidth; x++) {
            for (int y = 0; y < worldHeight; y++) {
                if (world_fg[x, y] == "air") {
                    continue;
                } 
                
                float tileLightVal = tMgr.GetTile(world_fg[x, y]).lightStrength;
                if (tileLightVal > 0) {
                    CreateLightSource(new Vector3Int(x, y, 0), tMgr.GetTile(world_fg[x, y]).lightColor, tileLightVal);
                }

                // Mark the possible air blocks around it as light sources
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                if (wCon.isSky(x + 1, y)) {
                    if (!GetLightSource(tilePosition + Vector3Int.right)) {
                        int chunk = WorldController.GetChunk(x+1, y);
                        LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.right, skyColor, skyStrength, true, true);
                    }
                }
                if (wCon.isSky(x - 1, y)) {
                    if (!GetLightSource(tilePosition + Vector3Int.left)) {
                        int chunk = WorldController.GetChunk(x-1, y);
                        LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.left, skyColor, skyStrength, true, true);
                    }
                }
                if (wCon.isSky(x, y + 1)) {
                    if (!GetLightSource(tilePosition + Vector3Int.up)) {
                        int chunk = WorldController.GetChunk(x, y+1);
                        LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.up, skyColor, skyStrength, true, true);
                    }
                }
                if (wCon.isSky(x, y - 1)) {
                    if (!GetLightSource(tilePosition + Vector3Int.down)) {
                        int chunk = WorldController.GetChunk(x, y-1);
                        LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.down, skyColor, skyStrength, true, true);
                    }
                }
            }
        }

        ApplyChunkTextureLightUpdates();
    }

    /// <summary>
    /// Called on tile modification. Handles addition/removal of light sources,
    /// as well as lightmap updates for new/removed blocks.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="newTile"></param>
    /// <param name="bgTile"></param>
    /// <returns></returns>
    public void HandleNewTile(int x, int y, string newTile, string bgTile) {
        SingleTileObject newTileData = tMgr.GetTile(newTile);


        //Tile is removed...
        //-------------------------------------------------------------------------------------------------------
        if (newTile == "air") {
            //Tile location has background
            if (bgTile != "air") {
                //Previous tile was non-sky light source
                if (GetLightSource(new Vector3Int(x, y, 0))) {
                    LightSource lightSource = GetLightSource(new Vector3Int(x, y, 0));
                    if (lightSource != null && lightSource.lightColor != skyColor)
                    {
                        RemoveLight(lightSource);
                        lightSource.gameObject.SetActive(false);
                        Destroy(lightSource.gameObject);
                    }
                }

                //Previous tile was not light source
                else {
                    Color currentColor = activeLightValues[x, y];
                    if (currentColor != Color.black && currentColor != Color.clear)
                    {
                        //Since new block is background only, update its light to reflect the new falloff
                        float colorIncrement = blockFalloff - bgFalloff;
                        currentColor = new Color(
                            Mathf.Clamp(currentColor.r + colorIncrement, 0f, 1f),
                            Mathf.Clamp(currentColor.g + colorIncrement, 0f, 1f),
                            Mathf.Clamp(currentColor.b + colorIncrement, 0f, 1f));

                        QueueChunkActiveLightTextureUpdate(x, y, currentColor);
                        QueueChunkBGActiveLightTextureUpdate(x, y, currentColor);

                        //Refresh light values by creating a light source here and removing it
                        LightSource source = CreateLightSource(new Vector3Int(x, y, 0), currentColor, 1f);
                        source.gameObject.SetActive(false);
                        Destroy(source.gameObject);
                    }
                }
            }

            //Tile location is now sky
            else {
                //If light source was there previously, remove it
                if (GetLightSource(new Vector3Int(x, y, 0))) {
                    LightSource lightSource = GetLightSource(new Vector3Int(x, y, 0));
                    if (lightSource != null && lightSource.lightColor != skyColor)
                    {
                        RemoveLight(lightSource);
                        lightSource.gameObject.SetActive(false);
                        Destroy(lightSource.gameObject);
                    }
                }

                QueueChunkAmbientLightTextureUpdate(x, y, Color.black);

                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                CreateLightSource(tilePosition, skyColor, 1f, true, true);

                skyTiles[x, y] = 1;
            }
        }
        //-------------------------------------------------------------------------------------------------------


        //Adding new tile with light value
        else if (newTileData.lightStrength > 0) {
            if (!GetLightSource(new Vector3Int(x, y, 0))) {
                CreateLightSource(new Vector3Int(x, y, 0), newTileData.lightColor, newTileData.lightStrength);
            } else {
                LightSource existingLight = GetLightSource(new Vector3Int(x, y, 0));
                if (existingLight.LightStrength <= newTileData.lightStrength) {
                    CreateLightSource(new Vector3Int(x, y, 0), newTileData.lightColor, newTileData.lightStrength);

                    bool wasSkyTile = skyTiles[x, y] == 1;

                    if (wasSkyTile) {
                        int chunk = WorldController.GetChunk(x, y);
                        RemoveLight(existingLight, true);
                    }
                }
            }

            skyTiles[x, y] = 0;
        } 
        
        //Adding new tile with no light value
        else {
            //Black out light values for the new tile
            QueueChunkActiveLightTextureUpdate(x, y, Color.black);
            QueueChunkBGActiveLightTextureUpdate(x, y, Color.black);
            //QueueChunkAmbientLightTextureUpdate(x, y, Color.clear);

            //If new neighbors are sky blocks, add sky light sources there
            Vector3Int tilePosition = new Vector3Int(x, y, 0);
            if (wCon.isSky(x + 1, y)) {
                if (!GetLightSource(tilePosition + Vector3Int.right)) {
                    int chunk = WorldController.GetChunk(x+1, y);
                    LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.right, skyColor, skyStrength, true, true);
                }
            }
            if (wCon.isSky(x - 1, y)) {
                if (!GetLightSource(tilePosition + Vector3Int.left)) {
                    int chunk = WorldController.GetChunk(x-1, y);
                    LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.left, skyColor, skyStrength, true, true);
                }
            }
            if (wCon.isSky(x, y + 1)) {
                if (!GetLightSource(tilePosition + Vector3Int.up)) {
                    int chunk = WorldController.GetChunk(x, y+1);
                    LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.up, skyColor, skyStrength, true, true);
                }
            }
            if (wCon.isSky(x, y - 1)) {
                if (!GetLightSource(tilePosition + Vector3Int.down)) {
                    int chunk = WorldController.GetChunk(x, y-1);
                    LightSource newSkylight = CreateLightSource(tilePosition + Vector3Int.down, skyColor, skyStrength, true, true);
                }
            }

            //Remove any light sources previously at this location
            LightSource existingLight = GetLightSource(new Vector3Int(x, y, 0));
            if (existingLight == null)
                existingLight = CreateLightSource(new Vector3Int(x, y, 0), activeLightValues[x, y], 1f, false);

            bool wasSkyTile = skyTiles[x, y] == 1;

            if (wasSkyTile) {
                int chunk = WorldController.GetChunk(x, y);
                RemoveLight(existingLight, true);
            } else {
                RemoveLight(existingLight);
            }

            existingLight.gameObject.SetActive(false);
            Destroy(existingLight.gameObject);

            skyTiles[x, y] = 0;
        }

        ApplyChunkTextureLightUpdates();
    }

    /// <summary>
    /// Creates a light source object at specified coordinates.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="strength"></param>
    /// <param name="doUpdate"></param>
    /// <returns></returns>
    public LightSource CreateLightSource(Vector3Int position, Color color, float strength, bool doUpdate = true, bool isAmbient = false) {
        LightSource newLightSource = Instantiate(lightSourcePrefab, position, Quaternion.identity, lightSourceParent).GetComponent<LightSource>();
        newLightSource.InitializeLight(color, strength, isAmbient);

        if (doUpdate)
            UpdateLight(newLightSource, isAmbient);

        int positionHash = Helpers.HashableInt(position.x, position.y);

        return newLightSource;
    }

    /// <summary>
    /// Gets light source component at specified coordinates (if there exists one).
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public LightSource GetLightSource(Vector3Int position, bool isAmbient = false)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(position.x + 0.5f, position.y + 0.5f), 
            Vector2.zero, 0f, 1 << 8);

        LightSource foundLight = hit ? hit.collider.GetComponent<LightSource>() : null;
        if (foundLight == null)
            return null;
        
        if (foundLight.Ambient == isAmbient)
            return foundLight;
        else
            return null;
    }

    /// <summary>
    /// Initial light map update call. Creates a matching light node at coordinates,
    /// updates the textures, and then starts update passes.
    /// </summary>
    /// <param name="light"></param>
    /// <returns></returns>
    void UpdateLight(LightSource light, bool isAmbient = false) {
        LightNode lightNode;
        lightNode.position = light.Position;
        lightNode.color = light.lightColor * light.LightStrength;

        Color currentColor = Color.black;
        
        if (!isAmbient) {
            currentColor = activeLightValues[light.Position.x, light.Position.y];
            activeLightValues[light.Position.x, light.Position.y] = new Color(
                Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
                Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
                Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength));
            //currentColor = activeLightValues[light.Position.x, light.Position.y];
        } else {
            currentColor = ambientLightValues[light.Position.x, light.Position.y];
            ambientLightValues[light.Position.x, light.Position.y] = new Color(
                Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
                Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
                Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength));
            //currentColor = ambientLightValues[light.Position.x, light.Position.y];
        }
        
        if (!isAmbient) {
            QueueChunkActiveLightTextureUpdate(light.Position.x, light.Position.y, currentColor);
            QueueChunkBGActiveLightTextureUpdate(light.Position.x, light.Position.y, currentColor);
        } else {
            QueueChunkAmbientLightTextureUpdate(light.Position.x, light.Position.y, currentColor);
        }

        removalPositions.Clear();
        
        if (!isAmbient) {
            activeUpdateQueue.Clear();
            activeUpdateQueue.Enqueue(lightNode); 
            PerformUpdatePasses(activeUpdateQueue, true, true, true, isAmbient);
        } else {
            ambientUpdateQueue.Clear();
            ambientUpdateQueue.Enqueue(lightNode); 
            PerformUpdatePasses(ambientUpdateQueue, true, true, true, isAmbient);
        }
    }

    /// <summary>
    /// Initial light map removal call. Clears light data for tile, starts removal passes,
    /// and then updates affected light sources.
    /// </summary>
    /// <param name="light"></param>
    /// <returns></returns>
    void RemoveLight(LightSource light, bool isAmbient = false) {
        LightNode lightNode;
        lightNode.position = light.Position;

        Color currentColor = Color.black;
        if (!isAmbient) {
            currentColor = activeLightValues[light.Position.x, light.Position.y];
            lightNode.color = new Color(
                Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
                Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
                Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength)
            );
            activeLightValues[light.Position.x, light.Position.y] = Color.black;
        } else {
            currentColor = ambientLightValues[light.Position.x, light.Position.y];
            lightNode.color = new Color(
                Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
                Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
                Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength)
            );
            ambientLightValues[light.Position.x, light.Position.y] = Color.black;
        }
        
        if (!isAmbient) {
            QueueChunkActiveLightTextureUpdate(light.Position.x, light.Position.y, Color.black);
            QueueChunkBGActiveLightTextureUpdate(light.Position.x, light.Position.y, Color.black);
        } else {
            QueueChunkAmbientLightTextureUpdate(light.Position.x, light.Position.y, Color.black);
        }

        if (!isAmbient) {
            activeRemovalQueue.Clear();
            activeRemovalQueue.Enqueue(lightNode);
            PerformRemovalPasses(activeRemovalQueue, isAmbient);
        } else {
            ambientRemovalQueue.Clear();
            ambientRemovalQueue.Enqueue(lightNode);
            PerformRemovalPasses(ambientRemovalQueue, isAmbient);
        }
        foreach (LightSource lightSrc in postRemovalUpdates) {
            if (lightSrc != light) {
                UpdateLight(lightSrc, isAmbient);
            }
        }
    }

    /// <summary>
    /// Update pass handler method. Executes update passes for each light node in the queue
    /// for each specified color channel.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="redChannel"></param>
    /// <param name="greenChannel"></param>
    /// <param name="blueChannel"></param>
    /// <returns></returns>
    void PerformUpdatePasses(Queue<LightNode> queue, bool redChannel = true, bool greenChannel = true, bool blueChannel = true, bool isAmbient = false) {
        if (!redChannel && !greenChannel && !blueChannel) {
            return;
        }

        //Builds a backup queue for re-running through queue for other color channels
        Queue<LightNode> backupQueue = new Queue<LightNode>();
        foreach (LightNode lNode in queue) {
            if (!removalPositions.Contains(lNode.position))
                backupQueue.Enqueue(lNode);
        }
        queue.Clear();

        if (redChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.RED, isAmbient);
        }

        if (greenChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.GREEN, isAmbient);
        }

        if (blueChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.BLUE, isAmbient);
        }
    }

    /// <summary>
    /// Analyzes neighbor light values and determines if they need to be queued for update.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="colorMode"></param>
    /// <returns></returns>
    void ExecuteUpdatePass(Queue<LightNode> queue, LightingChannelMode colorMode, bool isAmbient = false) {
        LightNode light = queue.Dequeue();

        float lightVal, lightValLeft, lightValUp, lightValRight, lightValDown;

        switch (colorMode) {
            case LightingChannelMode.RED:
                lightVal = light.color.r;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x-1, light.position.y].r : ambientLightValues[light.position.x-1, light.position.y].r) : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? (!isAmbient ? activeLightValues[light.position.x+1, light.position.y].r : ambientLightValues[light.position.x+1, light.position.y].r) : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y-1].r : ambientLightValues[light.position.x, light.position.y-1].r) : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y+1].r : ambientLightValues[light.position.x, light.position.y+1].r) : -1f;

            break;
            case LightingChannelMode.GREEN:
                lightVal = light.color.g;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x-1, light.position.y].g : ambientLightValues[light.position.x-1, light.position.y].g) : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? (!isAmbient ? activeLightValues[light.position.x+1, light.position.y].g : ambientLightValues[light.position.x+1, light.position.y].g) : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y-1].g : ambientLightValues[light.position.x, light.position.y-1].g) : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y+1].g : ambientLightValues[light.position.x, light.position.y+1].g) : -1f;
            break;
            case LightingChannelMode.BLUE:
                lightVal = light.color.b;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x-1, light.position.y].b : ambientLightValues[light.position.x-1, light.position.y].b) : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? (!isAmbient ? activeLightValues[light.position.x+1, light.position.y].b : ambientLightValues[light.position.x+1, light.position.y].b) : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y-1].b : ambientLightValues[light.position.x, light.position.y-1].b) : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y+1].b : ambientLightValues[light.position.x, light.position.y+1].b) : -1f;
            break;
            default:
                return;
        }

        ExtendUpdatePass(queue, light, lightVal, lightValLeft, Vector3Int.left, colorMode, isAmbient);
        ExtendUpdatePass(queue, light, lightVal, lightValUp, Vector3Int.up, colorMode, isAmbient);
        ExtendUpdatePass(queue, light, lightVal, lightValRight, Vector3Int.right, colorMode, isAmbient);
        ExtendUpdatePass(queue, light, lightVal, lightValDown, Vector3Int.down, colorMode, isAmbient);
    }

    /// <summary>
    /// Determines if this neighbor tile would increase in light value from nearby light node
    /// accounting for light falloff. If so, this neighbor becomes a light node and is added to the queue.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="light"></param>
    /// <param name="startLightVal"></param>
    /// <param name="neighborLightVal"></param>
    /// <param name="direction"></param>
    /// <param name="colorMode"></param>
    /// <returns></returns>
    private void ExtendUpdatePass(Queue<LightNode> queue, LightNode light, float startLightVal, float neighborLightVal, Vector3Int direction, LightingChannelMode colorMode, bool isAmbient = false) {
        if (neighborLightVal == -1f)
            return;

        int neighborX = light.position.x+direction.x;
        int neighborY = light.position.y+direction.y;

        bool hasBGTile = true;//wCon.isTile(neighborX, neighborY, 1);

        if (hasBGTile) {
            float thisLightFalloff = bgFalloff;

            bool hasFGTile = wCon.isTile(neighborX, neighborY, 0);
            if (hasFGTile)
                thisLightFalloff = blockFalloff;

            if (neighborLightVal + thisLightFalloff + passThreshold < startLightVal) {
                startLightVal = Mathf.Clamp(startLightVal-thisLightFalloff, 0f, 1f);
                Color currentColor = !isAmbient ? activeLightValues[neighborX, neighborY] : ambientLightValues[neighborX, neighborY];
                Color newColor;
                switch(colorMode) {
                    case LightingChannelMode.RED:
                        newColor = new Color(startLightVal, currentColor.g, currentColor.b);
                    break;
                    case LightingChannelMode.GREEN:
                        newColor = new Color(currentColor.r, startLightVal, currentColor.b);
                    break;
                    case LightingChannelMode.BLUE:
                        newColor = new Color(currentColor.r, currentColor.g, startLightVal);
                    break;
                    default:
                        return;
                }

                LightNode newLightNode;
                newLightNode.position = light.position+direction;
                newLightNode.color = newColor;
                if (!isAmbient)
                    activeLightValues[neighborX, neighborY] = newColor;
                else
                    ambientLightValues[neighborX, neighborY] = newColor;

                if (!isAmbient) {
                    QueueChunkActiveLightTextureUpdate(neighborX, neighborY, newColor);
                    QueueChunkBGActiveLightTextureUpdate(neighborX, neighborY, newColor);
                } else {
                    QueueChunkAmbientLightTextureUpdate(neighborX, neighborY, newColor);
                }

                queue.Enqueue(newLightNode);
            }
        }
    }

    /// <summary>
    /// Remove pass handler method. Executes remove passes for each light node in the queue
    /// for each specified color channel.
    /// </summary>
    /// <param name="queue"></param>
    /// <returns></returns>
    void PerformRemovalPasses(Queue<LightNode> queue, bool isAmbient = false) {
        postRemovalUpdates.Clear();

        //Builds backup queue to re-run through for each color channel
        Queue<LightNode> backupQueue = new Queue<LightNode>();
        foreach (LightNode lNode in queue) {
            backupQueue.Enqueue(lNode);
        }

        removalPositions.Clear();
        activeUpdateQueue.Clear();
        //Red Channel
        while (queue.Count > 0)
            ExecuteRemovalPass(queue, LightingChannelMode.RED, isAmbient);
        PerformUpdatePasses(activeUpdateQueue, true, false, false, isAmbient);

        removalPositions.Clear();
        activeUpdateQueue.Clear();
        //Green Channel
        foreach (LightNode lightNode in backupQueue)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteRemovalPass(queue, LightingChannelMode.GREEN, isAmbient);
        PerformUpdatePasses(activeUpdateQueue, false, true, false, isAmbient);

        removalPositions.Clear();
        activeUpdateQueue.Clear();
        //Blue Channel
        foreach (LightNode lightNode in backupQueue)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteRemovalPass(queue, LightingChannelMode.BLUE, isAmbient);
        PerformUpdatePasses(activeUpdateQueue, false, false, true, isAmbient);
    }

    /// <summary>
    /// Analyzes neighbor light values and determines if they need to be queued for update.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="colorMode"></param>
    /// <returns></returns>
    void ExecuteRemovalPass(Queue<LightNode> queue, LightingChannelMode colorMode, bool isAmbient = false) {
        LightNode light = queue.Dequeue();

        float lightVal, lightValLeft, lightValUp, lightValRight, lightValDown;

        int positionHash = Helpers.HashableInt(light.position.x, light.position.y);
        LightSource lightSource = GetLightSource(light.position);
        if (lightSource != null && !postRemovalUpdates.Contains(lightSource)) {
            postRemovalUpdates.Add(lightSource);
        }

        removalPositions.Add(light.position);

        switch (colorMode) {
            
            case LightingChannelMode.RED:
                lightVal = light.color.r;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x-1, light.position.y].r : ambientLightValues[light.position.x-1, light.position.y].r) : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? (!isAmbient ? activeLightValues[light.position.x+1, light.position.y].r : ambientLightValues[light.position.x+1, light.position.y].r) : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y-1].r : ambientLightValues[light.position.x, light.position.y-1].r) : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y+1].r : ambientLightValues[light.position.x, light.position.y+1].r) : -1f;

            break;
            case LightingChannelMode.GREEN:
                lightVal = light.color.g;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x-1, light.position.y].g : ambientLightValues[light.position.x-1, light.position.y].g) : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? (!isAmbient ? activeLightValues[light.position.x+1, light.position.y].g : ambientLightValues[light.position.x+1, light.position.y].g) : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y-1].g : ambientLightValues[light.position.x, light.position.y-1].g) : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y+1].g : ambientLightValues[light.position.x, light.position.y+1].g) : -1f;
            break;
            case LightingChannelMode.BLUE:
                lightVal = light.color.b;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x-1, light.position.y].b : ambientLightValues[light.position.x-1, light.position.y].b) : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? (!isAmbient ? activeLightValues[light.position.x+1, light.position.y].b : ambientLightValues[light.position.x+1, light.position.y].b) : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y-1].b : ambientLightValues[light.position.x, light.position.y-1].b) : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? (!isAmbient ? activeLightValues[light.position.x, light.position.y+1].b : ambientLightValues[light.position.x, light.position.y+1].b) : -1f;
            break;
            default:
                return;
        }

        ExtendRemovalPass(queue, light, lightVal, lightValLeft, Vector3Int.left, colorMode, isAmbient);
        ExtendRemovalPass(queue, light, lightVal, lightValUp, Vector3Int.up, colorMode, isAmbient);
        ExtendRemovalPass(queue, light, lightVal, lightValRight, Vector3Int.right, colorMode, isAmbient);
        ExtendRemovalPass(queue, light, lightVal, lightValDown, Vector3Int.down, colorMode, isAmbient);
    }

    /// <summary>
    /// Determines if this neighbor tile would decrease in light value from nearby light node.
    /// If so, this neighbor becomes a light node and is added to the queue.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="light"></param>
    /// <param name="startLightVal"></param>
    /// <param name="neighborLightVal"></param>
    /// <param name="direction"></param>
    /// <param name="colorMode"></param>
    /// <returns></returns>
    void ExtendRemovalPass(Queue<LightNode> queue, LightNode light, float startLightVal, float neighborLightVal, Vector3Int direction, LightingChannelMode colorMode, bool isAmbient = false) {
        int neighborX = light.position.x+direction.x;
        int neighborY = light.position.y+direction.y;

        if (neighborLightVal > 0f) {
            if (neighborLightVal < startLightVal) {
                Color currentColor = !isAmbient ? activeLightValues[neighborX, neighborY] : ambientLightValues[neighborX, neighborY];
                Color newColor;
                switch(colorMode) {
                    case LightingChannelMode.RED:
                        newColor = new Color(0f, currentColor.g, currentColor.b);
                    break;
                    case LightingChannelMode.GREEN:
                        newColor = new Color(currentColor.r, 0f, currentColor.b);
                    break;
                    case LightingChannelMode.BLUE:
                        newColor = new Color(currentColor.r, currentColor.g, 0f);
                    break;
                    default:
                        return;
                }

                LightNode lightRemovalNode;
                lightRemovalNode.position = light.position + direction;
                lightRemovalNode.color = currentColor;
                if (!isAmbient)
                    activeLightValues[neighborX, neighborY] = newColor;
                else
                    ambientLightValues[neighborX, neighborY] = newColor;

                if (!isAmbient) {
                    QueueChunkActiveLightTextureUpdate(neighborX, neighborY, newColor);
                    QueueChunkBGActiveLightTextureUpdate(neighborX, neighborY, newColor);
                } else {
                    QueueChunkAmbientLightTextureUpdate(neighborX, neighborY, newColor);
                }
                queue.Enqueue(lightRemovalNode);
            } else {
                LightNode lightNode;
                lightNode.position = light.position + direction;
                lightNode.color = !isAmbient ? activeLightValues[neighborX, neighborY] : ambientLightValues[neighborX, neighborY];
                activeUpdateQueue.Enqueue(lightNode);
            }
        }
    }
}
