using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(TileRenderer))]
[RequireComponent(typeof(TileManager))]
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
    private TileManager tMgr;
    private ChunkObjectsHolder cObjs;

    private int worldWidth, worldHeight;

    //Primary light value array
    private Color[,] lightValues;
 
    //Arrays to hold light map textures and materials
    private Texture2D[] chunkLightTexes;
    private Texture2D[] chunkBGLightTexes;
    private Material[] chunkLightMats;
    private Material[] chunkBGLightMats;

    private Dictionary<int, bool> chunksToUpdate = new Dictionary<int, bool>();

    //Queues of light nodes to remove/update
    private Queue<LightNode> removalQueue, updateQueue;

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

    public Shader lightMapShader;
    public GameObject lightSourcePrefab;
    public Transform lightSourceParent;

    public void Start() {
        wCon = GetComponent<WorldController>();
        wRend = GetComponent<TileRenderer>();
        tMgr = GetComponent<TileManager>();
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

        if (lightValues == null) {
            lightValues = new Color[worldWidth,worldHeight];
        }

        int chunkCount = WorldController.GetChunkCount();

        chunkLightTexes = new Texture2D[chunkCount];
        chunkBGLightTexes = new Texture2D[chunkCount];
        chunkLightMats = new Material[chunkCount];
        chunkBGLightMats = new Material[chunkCount];

        Color[,] baseChunkLightValues = GetBlackArray(chunkSize, chunkSize);  
        Color[] flattenedColors = flattenColorArray(baseChunkLightValues);

        for (int chunk = 0; chunk < chunkCount; chunk++) {
            Texture2D newLightTex = new Texture2D(chunkSize, chunkSize, TextureFormat.RGBA32, false);
            Texture2D newBGLightTex = new Texture2D(chunkSize, chunkSize, TextureFormat.RGBA32, false);
            Material newLightMat = new Material(lightMapShader);
            Material newBGLightMat = new Material(lightMapShader);
            
            //newLightTex.alphaIsTransparency = true;
            //newBGLightTex.alphaIsTransparency = true;
            newLightTex.filterMode = FilterMode.Point;
            newBGLightTex.filterMode = FilterMode.Point;

            newLightTex.SetPixels(flattenedColors);
            newBGLightTex.SetPixels(flattenedColors);
            newLightTex.Apply();
            newBGLightTex.Apply();

            chunkLightTexes[chunk] = newLightTex;
            chunkBGLightTexes[chunk] = newBGLightTex;

            newLightMat.SetTexture("_MainTex", chunkLightTexes[chunk]);
            newBGLightMat.SetTexture("_MainTex", chunkBGLightTexes[chunk]);

            GameObject chunkObj = cObjs.GetChunkObject(chunk);
            MeshRenderer fgRenderer = chunkObj.transform.Find("LightMap").GetComponent<MeshRenderer>();
            MeshRenderer bgRenderer = chunkObj.transform.Find("BGLightMap").GetComponent<MeshRenderer>();
            fgRenderer.material = newLightMat;
            bgRenderer.material = newBGLightMat;

            chunkLightMats[chunk] = newLightMat;
            chunkBGLightMats[chunk] = newBGLightMat;
        }

        lightValues = GetBlackArray(worldWidth, worldHeight);  
        flattenedColors = flattenColorArray(lightValues);

        updateQueue = new Queue<LightNode>();
        removalQueue = new Queue<LightNode>();
        postRemovalUpdates = new List<LightSource>();
        removalPositions = new List<Vector3Int>();
        dynamicLights = new List<DynamicLightSource>();

        HandleNewWorld();
    }

    /// <summary>
    /// Returns an empty color array (with the sky at full light) 
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    Color[,] GetBlackArray(int width, int height) {
        Color[,] blackArr = new Color[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (wCon.isSky(x, y)) {
                    blackArr[x, y] = Color.white;
                }
                blackArr[x, y] = Color.black;
            }
        }

        return blackArr;
    }

    /// <summary>
    /// Flattens a color array for feeding into Texture2D.SetPixels().
    /// </summary>
    /// <param name="colors"></param>
    /// <returns></returns>
    Color[] flattenColorArray(Color[,] colors) {
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
    void QueueChunkLightTextureUpdate(int x, int y, Color newColor) {
        if (wCon.isSky(x, y)) {
            newColor = Color.white;
        }

        int chunkToUpdate = WorldController.GetChunk(x, y);
        Vector2Int chunkPos = WorldController.GetChunkPosition(chunkToUpdate);
        chunkLightTexes[chunkToUpdate].SetPixel(x-chunkPos.x, y-chunkPos.y, newColor);
        chunksToUpdate[chunkToUpdate] = true;
    }

    /// <summary>
    /// Updates a chunk's background lightmap texture without applying it.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="newColor"></param>
    /// <returns></returns>
    void QueueChunkBGLightTextureUpdate(int x, int y, Color newColor) {
        if (wCon.isSky(x, y)) {
            newColor = Color.white;
        }

        int chunkToUpdate = WorldController.GetChunk(x, y);
        Vector2Int chunkPos = WorldController.GetChunkPosition(chunkToUpdate);
        chunkBGLightTexes[chunkToUpdate].SetPixel(x-chunkPos.x, y-chunkPos.y, newColor);
        chunksToUpdate[chunkToUpdate] = true;
    }

    /// <summary>
    /// Applies all made changes to chunk lightmap textures.
    /// </summary>
    /// <returns></returns>
    void ApplyChunkTextureLightUpdates() {
        foreach (int chunk in chunksToUpdate.Keys) {
            chunkLightTexes[chunk].Apply();
            chunkBGLightTexes[chunk].Apply();
        }

        chunksToUpdate.Clear();
    }

    /// <summary>
    /// Iterates through new world and calculates light values for every block.
    /// Generates light sources for relevant blocks and block-sky borders.
    /// </summary>
    /// <returns></returns>
    public void HandleNewWorld() {
        int[,] world_fg = wCon.GetWorld(0);
        int[,] world_bg = wCon.GetWorld(1);
        for (int x = 0; x < worldWidth; x++) {
            for (int y = 0; y < worldHeight; y++) {
                if (world_fg[x, y] == 0) {
                    continue;
                } 
                
                float tileLightVal = tMgr.allTiles[world_fg[x, y]].lightStrength;
                if (tileLightVal > 0) {
                    CreateLightSource(new Vector3Int(x, y, 0), tMgr.allTiles[world_fg[x, y]].lightColor, tileLightVal);
                }

                // Mark the possible air blocks around it as light sources
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                if (wCon.isSky(x + 1, y))
                    if (!GetLightSource(tilePosition + Vector3Int.right))
                        CreateLightSource(tilePosition + Vector3Int.right, skyColor, skyStrength);
                if (wCon.isSky(x - 1, y))
                    if (!GetLightSource(tilePosition + Vector3Int.left))
                        CreateLightSource(tilePosition + Vector3Int.left, skyColor, skyStrength);
                if (wCon.isSky(x, y + 1))
                    if (!GetLightSource(tilePosition + Vector3Int.up))
                        CreateLightSource(tilePosition + Vector3Int.up, skyColor, skyStrength);
                if (wCon.isSky(x, y - 1))
                    if (!GetLightSource(tilePosition + Vector3Int.down))
                        CreateLightSource(tilePosition + Vector3Int.down, skyColor, skyStrength);
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
    public void HandleNewTile(int x, int y, int newTile, int bgTile) {
        TileData newTileData = tMgr.allTiles[newTile];


        //Tile is removed...
        //-------------------------------------------------------------------------------------------------------
        if (newTile == 0) {
            //Tile location has background
            if (bgTile != 0) {
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
                    Color currentColor = lightValues[x, y];
                    if (currentColor != Color.black && currentColor != Color.clear)
                    {
                        //Since new block is background only, update its light to reflect the new falloff
                        float colorIncrement = blockFalloff - bgFalloff;
                        currentColor = new Color(
                            Mathf.Clamp(currentColor.r + colorIncrement, 0f, 1f),
                            Mathf.Clamp(currentColor.g + colorIncrement, 0f, 1f),
                            Mathf.Clamp(currentColor.b + colorIncrement, 0f, 1f));

                        QueueChunkLightTextureUpdate(x, y, currentColor);
                        QueueChunkBGLightTextureUpdate(x, y, currentColor);

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

                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                CreateLightSource(tilePosition, skyColor, 1f);
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
                }
            }
        } 
        
        //Adding new tile with no light value
        else {
            //Black out light values for the new tile
            QueueChunkLightTextureUpdate(x, y, Color.black);
            QueueChunkBGLightTextureUpdate(x, y, Color.black);

            //If new neighbors are sky blocks, add sky light sources there
            Vector3Int tilePosition = new Vector3Int(x, y, 0);
            if (wCon.isSky(x + 1, y))
                if (!GetLightSource(tilePosition + Vector3Int.right))
                    CreateLightSource(tilePosition + Vector3Int.right, skyColor, skyStrength);
            if (wCon.isSky(x - 1, y))
                if (!GetLightSource(tilePosition + Vector3Int.left))
                    CreateLightSource(tilePosition + Vector3Int.left, skyColor, skyStrength);
            if (wCon.isSky(x, y + 1))
                if (!GetLightSource(tilePosition + Vector3Int.up))
                    CreateLightSource(tilePosition + Vector3Int.up, skyColor, skyStrength);
            if (wCon.isSky(x, y - 1))
                if (!GetLightSource(tilePosition + Vector3Int.down))
                    CreateLightSource(tilePosition + Vector3Int.down, skyColor, skyStrength);

            //Remove any light sources previously at this location
            LightSource existingLight = GetLightSource(new Vector3Int(x, y, 0));
            if (existingLight == null)
                existingLight = CreateLightSource(new Vector3Int(x, y, 0), lightValues[x, y], 1f, false);

            RemoveLight(existingLight);
            existingLight.gameObject.SetActive(false);
            Destroy(existingLight.gameObject);
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
    public LightSource CreateLightSource(Vector3Int position, Color color, float strength, bool doUpdate = true) {
        LightSource newLightSource = Instantiate(lightSourcePrefab, position, Quaternion.identity, lightSourceParent).GetComponent<LightSource>();
        newLightSource.InitializeLight(color, strength);

        if (doUpdate)
            UpdateLight(newLightSource);

        int positionHash = Helpers.HashableInt(position.x, position.y);

        return newLightSource;
    }

    /// <summary>
    /// Gets light source component at specified coordinates (if there exists one).
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public LightSource GetLightSource(Vector3Int position)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(position.x + 0.5f, position.y + 0.5f), 
            Vector2.zero, 0f, 1 << 8);
        return hit ? hit.collider.GetComponent<LightSource>() : null;
    }

    /// <summary>
    /// Initial light map update call. Creates a matching light node at coordinates,
    /// updates the textures, and then starts update passes.
    /// </summary>
    /// <param name="light"></param>
    /// <returns></returns>
    void UpdateLight(LightSource light) {
        LightNode lightNode;
        lightNode.position = light.Position;
        lightNode.color = light.lightColor * light.LightStrength;

        Color currentColor = lightValues[light.Position.x, light.Position.y];
        lightValues[light.Position.x, light.Position.y] = new Color(
            Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
            Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
            Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength));
        currentColor = lightValues[light.Position.x, light.Position.y];
        
        QueueChunkLightTextureUpdate(light.Position.x, light.Position.y, currentColor);
        QueueChunkBGLightTextureUpdate(light.Position.x, light.Position.y, currentColor);

        removalPositions.Clear();
        updateQueue.Clear();
        updateQueue.Enqueue(lightNode); 
        
        PerformUpdatePasses(updateQueue);
    }

    /// <summary>
    /// Initial light map removal call. Clears light data for tile, starts removal passes,
    /// and then updates affected light sources.
    /// </summary>
    /// <param name="light"></param>
    /// <returns></returns>
    void RemoveLight(LightSource light) {
        LightNode lightNode;
        lightNode.position = light.Position;

        Color currentColor = lightValues[light.Position.x, light.Position.y];
        lightNode.color = new Color(
            Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
            Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
            Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength)
        );
        lightValues[light.Position.x, light.Position.y] = Color.black;
        QueueChunkLightTextureUpdate(light.Position.x, light.Position.y, Color.black);
        QueueChunkBGLightTextureUpdate(light.Position.x, light.Position.y, Color.black);

        removalQueue.Clear();
        removalQueue.Enqueue(lightNode);
        PerformRemovalPasses(removalQueue);

        foreach (LightSource lightSrc in postRemovalUpdates) {
            if (lightSrc != light) {
                UpdateLight(lightSrc);
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
    void PerformUpdatePasses(Queue<LightNode> queue, bool redChannel = true, bool greenChannel = true, bool blueChannel = true) {
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
                ExecuteUpdatePass(queue, LightingChannelMode.RED);
        }

        if (greenChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.GREEN);
        }

        if (blueChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.BLUE);
        }
    }

    /// <summary>
    /// Analyzes neighbor light values and determines if they need to be queued for update.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="colorMode"></param>
    /// <returns></returns>
    void ExecuteUpdatePass(Queue<LightNode> queue, LightingChannelMode colorMode) {
        LightNode light = queue.Dequeue();

        float lightVal, lightValLeft, lightValUp, lightValRight, lightValDown;

        switch (colorMode) {
            case LightingChannelMode.RED:
                lightVal = light.color.r;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? lightValues[light.position.x-1, light.position.y].r : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? lightValues[light.position.x+1, light.position.y].r : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? lightValues[light.position.x, light.position.y-1].r : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? lightValues[light.position.x, light.position.y+1].r : -1f;

            break;
            case LightingChannelMode.GREEN:
                lightVal = light.color.g;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? lightValues[light.position.x-1, light.position.y].g : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? lightValues[light.position.x+1, light.position.y].g : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? lightValues[light.position.x, light.position.y-1].g : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? lightValues[light.position.x, light.position.y+1].g : -1f;
            break;
            case LightingChannelMode.BLUE:
                lightVal = light.color.b;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? lightValues[light.position.x-1, light.position.y].b : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? lightValues[light.position.x+1, light.position.y].b : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? lightValues[light.position.x, light.position.y-1].b : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? lightValues[light.position.x, light.position.y+1].b : -1f;
            break;
            default:
                return;
        }

        ExtendUpdatePass(queue, light, lightVal, lightValLeft, Vector3Int.left, colorMode);
        ExtendUpdatePass(queue, light, lightVal, lightValUp, Vector3Int.up, colorMode);
        ExtendUpdatePass(queue, light, lightVal, lightValRight, Vector3Int.right, colorMode);
        ExtendUpdatePass(queue, light, lightVal, lightValDown, Vector3Int.down, colorMode);
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
    private void ExtendUpdatePass(Queue<LightNode> queue, LightNode light, float startLightVal, float neighborLightVal, Vector3Int direction, LightingChannelMode colorMode) {
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
                Color currentColor = lightValues[neighborX, neighborY];
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
                lightValues[neighborX, neighborY] = newColor;

                QueueChunkLightTextureUpdate(neighborX, neighborY, newColor);
                QueueChunkBGLightTextureUpdate(neighborX, neighborY, newColor);

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
    void PerformRemovalPasses(Queue<LightNode> queue) {
        postRemovalUpdates.Clear();

        //Builds backup queue to re-run through for each color channel
        Queue<LightNode> backupQueue = new Queue<LightNode>();
        foreach (LightNode lNode in queue) {
            backupQueue.Enqueue(lNode);
        }

        removalPositions.Clear();
        updateQueue.Clear();
        //Red Channel
        while (queue.Count > 0)
            ExecuteRemovalPass(queue, LightingChannelMode.RED);
        PerformUpdatePasses(updateQueue, true, false, false);

        removalPositions.Clear();
        updateQueue.Clear();
        //Green Channel
        foreach (LightNode lightNode in backupQueue)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteRemovalPass(queue, LightingChannelMode.GREEN);
        PerformUpdatePasses(updateQueue, false, true, false);

        removalPositions.Clear();
        updateQueue.Clear();
        //Blue Channel
        foreach (LightNode lightNode in backupQueue)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteRemovalPass(queue, LightingChannelMode.BLUE);
        PerformUpdatePasses(updateQueue, false, false, true);
    }

    /// <summary>
    /// Analyzes neighbor light values and determines if they need to be queued for update.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="colorMode"></param>
    /// <returns></returns>
    void ExecuteRemovalPass(Queue<LightNode> queue, LightingChannelMode colorMode) {
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
                lightValLeft = (light.position.x - 1 >= 0) ? lightValues[light.position.x-1, light.position.y].r : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? lightValues[light.position.x+1, light.position.y].r : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? lightValues[light.position.x, light.position.y-1].r : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? lightValues[light.position.x, light.position.y+1].r : -1f;

            break;
            case LightingChannelMode.GREEN:
                lightVal = light.color.g;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? lightValues[light.position.x-1, light.position.y].g : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? lightValues[light.position.x+1, light.position.y].g : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? lightValues[light.position.x, light.position.y-1].g : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? lightValues[light.position.x, light.position.y+1].g : -1f;
            break;
            case LightingChannelMode.BLUE:
                lightVal = light.color.b;
                if (lightVal <= 0) {
                    return;
                }
                lightValLeft = (light.position.x - 1 >= 0) ? lightValues[light.position.x-1, light.position.y].b : -1f;
                lightValRight = (light.position.x + 1 < worldWidth) ? lightValues[light.position.x+1, light.position.y].b : -1f;
                lightValDown = (light.position.y - 1 >= 0) ? lightValues[light.position.x, light.position.y-1].b : -1f;
                lightValUp = (light.position.y + 1 < worldHeight) ? lightValues[light.position.x, light.position.y+1].b : -1f;
            break;
            default:
                return;
        }

        ExtendRemovalPass(queue, light, lightVal, lightValLeft, Vector3Int.left, colorMode);
        ExtendRemovalPass(queue, light, lightVal, lightValUp, Vector3Int.up, colorMode);
        ExtendRemovalPass(queue, light, lightVal, lightValRight, Vector3Int.right, colorMode);
        ExtendRemovalPass(queue, light, lightVal, lightValDown, Vector3Int.down, colorMode);
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
    void ExtendRemovalPass(Queue<LightNode> queue, LightNode light, float startLightVal, float neighborLightVal, Vector3Int direction, LightingChannelMode colorMode) {
        int neighborX = light.position.x+direction.x;
        int neighborY = light.position.y+direction.y;

        if (neighborLightVal > 0f) {
            if (neighborLightVal < startLightVal) {
                Color currentColor = lightValues[neighborX, neighborY];
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
                lightValues[neighborX, neighborY] = newColor;

                QueueChunkLightTextureUpdate(neighborX, neighborY, newColor);
                QueueChunkBGLightTextureUpdate(neighborX, neighborY, newColor);

                queue.Enqueue(lightRemovalNode);
            } else {
                LightNode lightNode;
                lightNode.position = light.position + direction;
                lightNode.color = lightValues[neighborX, neighborY];
                updateQueue.Enqueue(lightNode);
            }
        }
    }
}
