using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(WorldRenderer))]
[RequireComponent(typeof(TileManager))]
public class LightController : MonoBehaviour
{
    public enum LightingChannelMode
    {
        RED,
        GREEN,
        BLUE
    } 

    private WorldController wCon;
    private WorldRenderer wRend;
    private TileManager tMgr;

    private int[,] world_fg;
    private int[,] world_bg;
    private int worldWidth;
    private int worldHeight;

    private Color[,] lightValues;
    private Texture2D lightTex;

    private Queue<LightNode> removalQueue, updateQueue;
    private List<LightSource> postRemovalUpdates;
    private List<Vector3Int> removalPositions;

    public Color ambientColor;
    [Range(0f, 1f)]
    public float ambientStrength = 1f;

    public int blockLightPenetration;
    public int bgLightPenetration;

    [Range(0f, 1f)]
    public float bgShadowMult;

    float blockFalloff;
    float bgFalloff;
    public float passThreshold;

    public Shader lightMapShader;
    public Material lightMapMat;
    public Material bgLightMapMat;
    private Texture2D lightMapTex;
    private Texture2D bgLightMapTex;

    public Transform lightSourceParent;
    public GameObject lightSourcePrefab;

    public void Start() {
        wCon = GetComponent<WorldController>();
        wRend = GetComponent<WorldRenderer>();
        tMgr = GetComponent<TileManager>();

        blockFalloff = 1f/blockLightPenetration;
        bgFalloff = 1f/bgLightPenetration;
    }

    public void InitializeWorld(int[,] newWorld_fg, int[,] newWorld_bg) {
        world_fg = newWorld_fg;
        world_bg = newWorld_bg;

        worldWidth = world_fg.GetUpperBound(0)+1;
        worldHeight = world_fg.GetUpperBound(1)+1;

        if (lightValues == null) {
            lightValues = new Color[worldWidth,worldHeight];
        }

        lightMapTex = new Texture2D(worldWidth, worldHeight, TextureFormat.RGBAFloat, false);
        bgLightMapTex = new Texture2D(worldWidth, worldHeight, TextureFormat.RGBAFloat, false);
        //lightMapMat = new Material(lightMapShader);
        lightMapTex.alphaIsTransparency = true;
        bgLightMapTex.alphaIsTransparency = true;
        lightMapTex.filterMode = FilterMode.Point;
        bgLightMapTex.filterMode = FilterMode.Point;

        lightValues = GetBlackArray(worldWidth, worldHeight);  
        Color[] flattenedColors = flattenColorArray(lightValues);
        lightMapTex.SetPixels(flattenedColors);
        bgLightMapTex.SetPixels(flattenedColors);
        lightMapTex.Apply();
        bgLightMapTex.Apply();
        lightMapMat.SetTexture("_MainTex", lightMapTex);
        bgLightMapMat.SetTexture("_MainTex", bgLightMapTex);

        updateQueue = new Queue<LightNode>();
        removalQueue = new Queue<LightNode>();
        postRemovalUpdates = new List<LightSource>();
        removalPositions = new List<Vector3Int>();

        HandleNewWorld();
    }

    Color[,] GetBlackArray(int width, int height) {
        Color blackColor = new Color(0, 0, 0, 1);
        Color[,] blackArr = new Color[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (world_fg != null) {
                    if (world_fg[x, y] == 0)
                        continue;
                }
                blackArr[x, y] = blackColor;
            }
        }

        return blackArr;
    }

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

    Color GetLightAlpha(Color color) {
        /*float alphaVal = (1-((color.r + color.g + color.b) / 3f));
        color.a = alphaVal;
        color.r *= 1-alphaVal;
        color.g *= 1-alphaVal;
        color.b *= 1-alphaVal;*/
        return color;
    }

    void QueueLightTextureUpdate(int x, int y, Color newColor) {
        Color alphaAdjustedColor = GetLightAlpha(newColor);
        if (world_bg[x, y] == 0 && world_fg[x, y] == 0) {
            alphaAdjustedColor = Color.white;
        }
        lightMapTex.SetPixel(x, y, alphaAdjustedColor);
    }

    void QueueBGLightTextureUpdate(int x, int y, Color newColor) {
        Color shadowedColor = new Color(
            newColor.r * bgShadowMult,
            newColor.g * bgShadowMult,
            newColor.b * bgShadowMult,
            newColor.a);

        Color alphaAdjustedColor = GetLightAlpha(shadowedColor);

        if (world_bg[x, y] == 0 && world_fg[x, y] == 0) {
            alphaAdjustedColor = Color.white;
        }

        bgLightMapTex.SetPixel(x, y, alphaAdjustedColor);
    }

    void ApplyLightTextureUpdates() {
        lightMapTex.Apply();
        bgLightMapTex.Apply();
    }

    public void HandleNewWorld() {
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
                        CreateLightSource(tilePosition + Vector3Int.right, ambientColor, ambientStrength);
                if (wCon.isSky(x - 1, y))
                    if (!GetLightSource(tilePosition + Vector3Int.left))
                        CreateLightSource(tilePosition + Vector3Int.left, ambientColor, ambientStrength);
                if (wCon.isSky(x, y + 1))
                    if (!GetLightSource(tilePosition + Vector3Int.up))
                        CreateLightSource(tilePosition + Vector3Int.up, ambientColor, ambientStrength);
                if (wCon.isSky(x, y - 1))
                    if (!GetLightSource(tilePosition + Vector3Int.down))
                        CreateLightSource(tilePosition + Vector3Int.down, ambientColor, ambientStrength);
                }
        }

        ApplyLightTextureUpdates();
    }

    public void HandleNewBlock(int x, int y, int newTile, int bgTile) {
        TileData newTileData = tMgr.allTiles[newTile];

        if (newTile == 0) {
            if (bgTile != 0) {
                if (GetLightSource(new Vector3Int(x, y, 0))) {
                    LightSource lightSource = GetLightSource(new Vector3Int(x, y, 0));
                    if (lightSource != null && lightSource.lightColor != ambientColor)
                    {
                        RemoveLight(lightSource);
                        lightSource.gameObject.SetActive(false);
                        Destroy(lightSource.gameObject);
                    }
                }

                else {
                    Color currentColor = lightValues[x, y];
                    if (currentColor != Color.black && currentColor != Color.clear)
                    {
                        // The amount of brightness to add is the difference between falloffs!
                        float colorIncrement = blockFalloff - bgFalloff;
                        currentColor = new Color(
                            Mathf.Clamp(currentColor.r + colorIncrement, 0f, 1f),
                            Mathf.Clamp(currentColor.g + colorIncrement, 0f, 1f),
                            Mathf.Clamp(currentColor.b + colorIncrement, 0f, 1f));

                        QueueLightTextureUpdate(x, y, currentColor);
                        QueueBGLightTextureUpdate(x, y, currentColor);

                        // Create a light source to update light values, then remove it
                        LightSource source = CreateLightSource(new Vector3Int(x, y, 0), currentColor, 1f);
                        source.gameObject.SetActive(false);
                        Destroy(source.gameObject);
                    }
                }
            }

            else {
                if (GetLightSource(new Vector3Int(x, y, 0))) {
                    LightSource lightSource = GetLightSource(new Vector3Int(x, y, 0));
                    if (lightSource != null && lightSource.lightColor != ambientColor)
                    {
                        RemoveLight(lightSource);
                        lightSource.gameObject.SetActive(false);
                        Destroy(lightSource.gameObject);
                    }
                }

                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                CreateLightSource(tilePosition, ambientColor, 1f);
            }

            /*if (bgTile == 0) {
                if (GetLightSource(new Vector3Int(x, y, 0))) {
                    CreateLightSource(new Vector3Int(x, y, 0), ambientColor, 1f);
                }
            }*/
        }

        else if (newTileData.lightStrength > 0) {
            if (!GetLightSource(new Vector3Int(x, y, 0))) {
                CreateLightSource(new Vector3Int(x, y, 0), newTileData.lightColor, newTileData.lightStrength);
            } else {
                LightSource existingLight = GetLightSource(new Vector3Int(x, y, 0));
                if (existingLight.LightStrength <= newTileData.lightStrength) {
                    CreateLightSource(new Vector3Int(x, y, 0), newTileData.lightColor, newTileData.lightStrength);
                }
            }
        } else {
            QueueLightTextureUpdate(x, y, Color.black);
            QueueBGLightTextureUpdate(x, y, Color.black);

            // Mark the possible air blocks around it as light sources
            Vector3Int tilePosition = new Vector3Int(x, y, 0);
            if (wCon.isSky(x + 1, y))
                if (!GetLightSource(tilePosition + Vector3Int.right))
                    CreateLightSource(tilePosition + Vector3Int.right, ambientColor, ambientStrength);
            if (wCon.isSky(x - 1, y))
                if (!GetLightSource(tilePosition + Vector3Int.left))
                    CreateLightSource(tilePosition + Vector3Int.left, ambientColor, ambientStrength);
            if (wCon.isSky(x, y + 1))
                if (!GetLightSource(tilePosition + Vector3Int.up))
                    CreateLightSource(tilePosition + Vector3Int.up, ambientColor, ambientStrength);
            if (wCon.isSky(x, y - 1))
                if (!GetLightSource(tilePosition + Vector3Int.down))
                    CreateLightSource(tilePosition + Vector3Int.down, ambientColor, ambientStrength);

            LightSource existingLight = GetLightSource(new Vector3Int(x, y, 0));
            if (existingLight == null)
                existingLight = CreateLightSource(new Vector3Int(x, y, 0), lightValues[x, y], 1f, false);

            RemoveLight(existingLight);
            existingLight.gameObject.SetActive(false);
            Destroy(existingLight.gameObject);
        }

        ApplyLightTextureUpdates();
    }

    LightSource CreateLightSource(Vector3Int position, Color color, float strength, bool doUpdate = true) {
        LightSource newLightSource = Instantiate(lightSourcePrefab, position, Quaternion.identity, lightSourceParent).GetComponent<LightSource>();
        newLightSource.InitializeLight(color, strength);

        if (doUpdate)
            UpdateLight(newLightSource);

        int positionHash = WorldCollider.HashableInt(position.x, position.y);

        return newLightSource;
    }

    public LightSource GetLightSource(Vector3Int position)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(position.x + 0.5f, position.y + 0.5f), 
            Vector2.zero, 0f, 1 << 8);
        return hit ? hit.collider.GetComponent<LightSource>() : null;
    }

    /*
    LightSource GetLightSource(Vector3Int position) {
        int positionHash = WorldCollider.HashableInt(position.x, position.y);
        if (lightSourcesDict.ContainsKey(positionHash)) {
            return lightSourcesDict[positionHash];
        } else {
            //Debug.Log("No light source found at " + position.x + ", " + position.y);
            return null;
        }
    }*/

    [ContextMenu("Update all lights")]
    public void UpdateAllLights()
    {
        /* Reset all lighting data if applicable. If the data is not cleared, light sources with no other change
         * will not spread due to all surrounding colors being equal to what the light would spread initially. */

        updateQueue.Clear();

        lightValues = GetBlackArray(worldWidth, worldHeight);
        lightMapTex.SetPixels(flattenColorArray(lightValues));
        lightMapTex.Apply();

        // Try to queue all LightSources in the game
        foreach (Transform child in lightSourceParent.transform)
        {
            if (child.gameObject.activeSelf)
            {
                LightSource source = child.GetComponent<LightSource>();
                if (source != null && source.Initialized)
                {
                    LightNode lightNode;
                    lightNode.position = source.Position;
                    lightNode.color = source.lightColor;
                    lightValues[source.Position.x, source.Position.y] = source.lightColor * source.LightStrength;
                    updateQueue.Enqueue(lightNode);
                }
            }
        }
        PerformUpdatePasses(updateQueue);
    }

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
        
        QueueLightTextureUpdate(light.Position.x, light.Position.y, currentColor);
        QueueBGLightTextureUpdate(light.Position.x, light.Position.y, currentColor);

        removalPositions.Clear();
        updateQueue.Clear();
        updateQueue.Enqueue(lightNode); 
        
        PerformUpdatePasses(updateQueue);
    }

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
        QueueLightTextureUpdate(light.Position.x, light.Position.y, Color.black);
        QueueBGLightTextureUpdate(light.Position.x, light.Position.y, Color.black);

        removalQueue.Clear();
        removalQueue.Enqueue(lightNode);
        PerformRemovalPasses(removalQueue);

        foreach (LightSource lightSrc in postRemovalUpdates) {
            if (lightSrc != light) {
                UpdateLight(lightSrc);
            }
        }

        int positionHash = WorldCollider.HashableInt(light.Position.x, light.Position.y);
    }

    void PerformUpdatePasses(Queue<LightNode> queue, bool redChannel = true, bool greenChannel = true, bool blueChannel = true) {
        if (!redChannel && !greenChannel && !blueChannel) {
            return;
        }

        Queue<LightNode> backupQueue = new Queue<LightNode>();
        foreach (LightNode lNode in queue) {
            if (!removalPositions.Contains(lNode.position))
                backupQueue.Enqueue(lNode);
        }
        queue.Clear();

        //Red Channel
        if (redChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.RED);
        }

        //Green Channel
        if (greenChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.GREEN);
        }

        //Blue Channel
        if (blueChannel) {
            if (queue.Count == 0)
                foreach (LightNode lightNode in backupQueue)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteUpdatePass(queue, LightingChannelMode.BLUE);
        }
    }

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

                QueueLightTextureUpdate(neighborX, neighborY, newColor);
                QueueBGLightTextureUpdate(neighborX, neighborY, newColor);

                queue.Enqueue(newLightNode);
            }
        }

        /*if (world[neighborX, neighborY] == 0) {
            return;
        }*/
    }

    void PerformRemovalPasses(Queue<LightNode> queue) {
        postRemovalUpdates.Clear();

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

    void ExecuteRemovalPass(Queue<LightNode> queue, LightingChannelMode colorMode) {
        LightNode light = queue.Dequeue();

        float lightVal, lightValLeft, lightValUp, lightValRight, lightValDown;

        int positionHash = WorldCollider.HashableInt(light.position.x, light.position.y);
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

                QueueLightTextureUpdate(neighborX, neighborY, newColor);
                QueueBGLightTextureUpdate(neighborX, neighborY, newColor);

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
