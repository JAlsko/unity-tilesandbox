using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles all lighting effects in the game.
/// 
/// Uses a 2D Color array to hold the lightmap and references a front and a back
/// Tilemap that contain the blocks, which is all you need to make this work.
/// </summary>
public class LightingManager : Singleton<LightingManager>
{    
    [Header("Block Lighting")]
    public GameObject lightSourceRoot;
    public GameObject lightSourcePrefab;
    public Color ambientLightColor = Color.white;
    [Tooltip("How far light can penetrate the front tile layer. 1 penetration = 1 block.")]
    public float lightPenetration;
    [Tooltip("How far light can penetrate the back tile layer. 1 penetration = 1 block.")]
    public float lightPenetrationBack;
    [Tooltip("The percentage of darkening of the back layer compared to the front layer.")]
    [Range(0f, 1f)]
    public float backLayerShadowFactor;
    public float LightFalloff { get; set; }
    public float LightFalloffBack { get; set; }
    public Color[,] LightColors { get; private set; }
    public enum LightingChannelMode
    {
        RED,
        GREEN,
        BLUE
    }    

    private float lightPassThreshold;
    private int worldWidth, worldHeight;
    private Vector3Int vectorRight, vectorUp, vectorLeft, vectorDown;
    private Queue<LightNode> queueUpdate, queueRemoval;
    private List<Vector3Int> removalPositions;
    private List<LightSource> removalLights;


    private void Start()
    {
        // Data structures and shortcuts
        LightColors = new Color[GenerationManager.Instance.worldWidth, GenerationManager.Instance.worldHeight];
        queueUpdate = new Queue<LightNode>();
        queueRemoval = new Queue<LightNode>();
        removalPositions = new List<Vector3Int>();
        removalLights = new List<LightSource>();

        worldWidth = GenerationManager.Instance.worldWidth;
        worldHeight = GenerationManager.Instance.worldHeight;
        vectorRight = Vector3Int.right;
        vectorUp = Vector3Int.up;
        vectorLeft = Vector3Int.left;
        vectorDown = Vector3Int.down;
    }

    private void Update()
    {
        // Updates light penetration values
        LightFalloff = 1.0f / lightPenetration;
        LightFalloffBack = 1.0f / lightPenetrationBack;
        lightPassThreshold = LightFalloffBack;
    }


    /// <summary>
    /// Destroys all LightSources in the game and clears all lighting data.
    /// </summary>
    public void Clear()
    {
        foreach (Transform transform in lightSourceRoot.transform)
            Destroy(transform.gameObject);
        LightColors = new Color[GenerationManager.Instance.worldWidth,
            GenerationManager.Instance.worldHeight];
    }


    /// <summary>
    /// Creates a light source.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="strength"></param>
    /// <param name="penetration"></param>
    /// <param name="penetrationBack"></param>
    /// <returns></returns>
    public LightSource CreateLightSource(Vector3Int position, Color color, float strength, bool updateLight = true)
    {
        LightSource lightSource = Instantiate(lightSourcePrefab, position, 
            Quaternion.identity, lightSourceRoot.transform).GetComponent<LightSource>();
        lightSource.InitializeLight(color, strength);
        if (updateLight)
            UpdateLight(lightSource);
        return lightSource;
    }


    /// <summary>
    /// Returns the LightSource at the given position or null if no LightSource was found.
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
    /// Updates all LightSources currently active in the game.
    /// </summary>
    /// <returns></returns>
    public void UpdateAllLights(bool clearData = true)
    {
        queueUpdate.Clear();

        /* Reset all lighting data if applicable. If the data is not cleared, light sources with no other change
         * will not spread due to all surrounding colors being equal to what the light would spread initially. */
        if (clearData)
            LightColors = new Color[GenerationManager.Instance.worldWidth, GenerationManager.Instance.worldHeight];    
        
        // Try to queue all LightSources in the game
        foreach (Transform child in lightSourceRoot.transform)
        {
            if (child.gameObject.activeSelf)
            {
                LightSource source = child.GetComponent<LightSource>();
                if (source != null && source.Initialized)
                {
                    LightNode lightNode;
                    lightNode.position = source.Position;
                    lightNode.color = source.lightColor;
                    LightColors[source.Position.x, source.Position.y] = source.lightColor * source.LightStrength;
                    queueUpdate.Enqueue(lightNode);
                }
            }
        }
        PerformLightPasses(queueUpdate);
    }

    /// <summary>
    /// Updates the given LightSource by trying to spread its light to surrounding tiles.
    /// This works on an overwrite basis. If the surrounding light is stronger, it stops.
    /// </summary>
    /// <param name="light"></param>
    public void UpdateLight(LightSource light)
    {        
        LightNode lightNode;
        lightNode.position = light.Position;
        lightNode.color = light.lightColor * light.LightStrength;

        // Set the color of the light source's tiles
        Color currentColor = LightColors[light.Position.x, light.Position.y];
        LightColors[light.Position.x, light.Position.y] = new Color(
            Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
            Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
            Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength));
        currentColor = LightColors[light.Position.x, light.Position.y];
        GenerationManager.Instance.tileMapBlocksFront.SetColor(light.Position, currentColor);
        GenerationManager.Instance.tileMapBlocksBack.SetColor(light.Position, new Color(
            Mathf.Clamp(currentColor.r * backLayerShadowFactor, 0f, 1f),
            Mathf.Clamp(currentColor.g * backLayerShadowFactor, 0f, 1f),
            Mathf.Clamp(currentColor.b * backLayerShadowFactor, 0f, 1f)));

        removalPositions.Clear();
        queueUpdate.Clear();
        queueUpdate.Enqueue(lightNode); 
        
        PerformLightPasses(queueUpdate);
    }


    /// <summary>
    /// Removes all light of the given LightSource.
    /// </summary>
    /// <param name="light"></param>
    /// <param name="updateLight"></param>
    public void RemoveLight(LightSource light)
    {               
        LightNode lightNode;
        lightNode.position = light.Position;

        /* If there are many LightSources close together, the color of the LightSource is drowned out.
         * To correctly remove this enough, instead of using the light's color to remove, use the color 
         * of the LightSource's tile if that color is greater. */
        Color currentColor = LightColors[light.Position.x, light.Position.y];
        lightNode.color = new Color(
            Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
            Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
            Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength));           
        LightColors[light.Position.x, light.Position.y] = Color.black;
        GenerationManager.Instance.tileMapBlocksFront.SetColor(light.Position, Color.black);
        GenerationManager.Instance.tileMapBlocksBack.SetColor(light.Position, Color.black);

        queueRemoval.Clear();
        queueRemoval.Enqueue(lightNode);
        PerformLightRemovalPasses(queueRemoval);

        /* If we touched LightSources during removal spreading, we completely drowned out their color.
         * In order to correctly fill it in, we need to update these lights. */
        foreach (LightSource lightSource in removalLights)
            if (lightSource != light)
                UpdateLight(lightSource);
    }


    /// <summary>
    /// Generates colored lighting for every queued node.
    /// Makes use of Breadth-First Search using a FIFO Queue. This way, all blocks are only visited once.
    /// Executes passes per light channel (RGB) to ensure correct blending.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="redChannel"></param>
    /// <param name="greenChannel"></param>
    /// <param name="blueChannel"></param>
    private void PerformLightPasses(Queue<LightNode> queue, bool redChannel = true, 
        bool greenChannel = true, bool blueChannel = true)
    {
        if (!redChannel && !greenChannel && !blueChannel)
            return;

        /* Generate a backup queue to refill the original queue after each channel
         * (since every channel execution empties the queue). 
         * 
         * In case of being called during light removal, an extra check ensures only 
         * the outer edge nodes of stronger lighted tiles will fill in the removed light
         * if applicable. */
        Queue<LightNode> queueBackup = new Queue<LightNode>();
        foreach (LightNode lightNode in queue)
            if (!removalPositions.Contains(lightNode.position))
                queueBackup.Enqueue(lightNode);
        queue.Clear();

        // Spread light for each channel
        if (redChannel)
        {
            if (queue.Count == 0)
                foreach (LightNode lightNode in queueBackup)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteLightingPass(queue, LightingChannelMode.RED);
        }
        if (greenChannel)
        {
            if (queue.Count == 0)
                foreach (LightNode lightNode in queueBackup)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteLightingPass(queue, LightingChannelMode.GREEN);
        }
        if (blueChannel)
        {
            if (queue.Count == 0)
                foreach (LightNode lightNode in queueBackup)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
                ExecuteLightingPass(queue, LightingChannelMode.BLUE);
        }
    }


    /// <summary>
    /// Removes all queued nodes' light channels.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="updateLight"></param>
    private void PerformLightRemovalPasses(Queue<LightNode> queue)
    {
        removalLights.Clear();

        /* Generate a backup queue to refill the original queue after each channel
         * (since every channel execution empties the queue).*/
        Queue<LightNode> queueBackup = new Queue<LightNode>();
        foreach (LightNode lightNode in queue)
            queueBackup.Enqueue(lightNode);
        
        removalPositions.Clear();        
        queueUpdate.Clear();
        while (queue.Count > 0)
            ExecuteLightingRemovalPass(queue, LightingChannelMode.RED); 
        // Fill in empty space if I found stronger (red channel) tiles
        PerformLightPasses(queueUpdate, true, false, false);

        removalPositions.Clear();
        queueUpdate.Clear();
        foreach (LightNode lightNode in queueBackup)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteLightingRemovalPass(queue, LightingChannelMode.GREEN);
        // Fill in empty space if I found stronger (green channel) tiles
        PerformLightPasses(queueUpdate, false, true, false);

        removalPositions.Clear();
        queueUpdate.Clear();
        foreach (LightNode lightNode in queueBackup)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteLightingRemovalPass(queue, LightingChannelMode.BLUE);
        // Fill in empty space if I found stronger (blue channel) tiles
        PerformLightPasses(queueUpdate, false, false, true);
    }


    /// <summary>
    /// Spreads the given channel light of all queued nodes in 4 directions.
    /// This is one pass that scans left, down, right and up in that order, so
    /// at most 4 tiles are affected by a single pass per node.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="mode"></param>
    private void ExecuteLightingPass(Queue<LightNode> queue, LightingChannelMode mode)
    {
        // Get the LightNode that's first in line
        LightNode light = queue.Dequeue();
        float lightValue, lightValueLeft, lightValueDown, lightValueRight, lightValueUp;

        /* Obtain surrounding light values from the corresponding channel to lessen overhead
         * on extension passes. */
        switch (mode)
        {
            case LightingChannelMode.RED:
                if (light.color.r <= 0f)
                    return;

                lightValue = light.color.r;
                lightValueLeft = (light.position.x - 1 >= 0) ? LightColors[light.position.x - 1, light.position.y].r : -1f;
                lightValueDown = (light.position.y - 1 >= 0) ? LightColors[light.position.x, light.position.y - 1].r : -1f;
                lightValueRight = (light.position.x + 1 < worldWidth) ? LightColors[light.position.x + 1, light.position.y].r : -1f;
                lightValueUp = (light.position.y + 1 < worldHeight) ? LightColors[light.position.x, light.position.y + 1].r : -1f;
                break;
            case LightingChannelMode.GREEN:
                if (light.color.g <= 0f)
                    return;

                lightValue = light.color.g;
                lightValueLeft = (light.position.x - 1 >= 0) ? LightColors[light.position.x - 1, light.position.y].g : -1f;
                lightValueDown = (light.position.y - 1 >= 0) ? LightColors[light.position.x, light.position.y - 1].g : -1f;
                lightValueRight = (light.position.x + 1 < worldWidth) ? LightColors[light.position.x + 1, light.position.y].g : -1f;
                lightValueUp = (light.position.y + 1 < worldHeight) ? LightColors[light.position.x, light.position.y + 1].g : -1f;
                break;
            case LightingChannelMode.BLUE:
                if (light.color.b <= 0f)
                    return;

                lightValue = light.color.b;
                lightValueLeft = (light.position.x - 1 >= 0) ? LightColors[light.position.x - 1, light.position.y].b : -1f;
                lightValueDown = (light.position.y - 1 >= 0) ? LightColors[light.position.x, light.position.y - 1].b : -1f;
                lightValueRight = (light.position.x + 1 < worldWidth) ? LightColors[light.position.x + 1, light.position.y].b : -1f;
                lightValueUp = (light.position.y + 1 < worldHeight) ? LightColors[light.position.x, light.position.y + 1].b : -1f;
                break;
            default:
                return;
        }

        // Try and spread its light to surrounding blocks
        ExtendQueueLightPass(queue, light, lightValue, lightValueLeft, vectorLeft, mode);
        ExtendQueueLightPass(queue, light, lightValue, lightValueDown, vectorDown, mode);
        ExtendQueueLightPass(queue, light, lightValue, lightValueRight, vectorRight, mode);
        ExtendQueueLightPass(queue, light, lightValue, lightValueUp, vectorUp, mode);       
    }

    /// <summary>
    /// Try to extend the light in the given direction and add to the queue if succesful.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="light"></param>
    /// <param name="lightValue"></param>
    /// <param name="lightValueDirection"></param>
    /// <param name="direction"></param>
    /// <param name="mode"></param>
    private void ExtendQueueLightPass(Queue<LightNode> queue, LightNode light, float lightValue,
        float lightValueDirection, Vector3Int direction, LightingChannelMode mode)
    {
        if (lightValueDirection != -1f)
        {
            bool hasBackTile = GenerationManager.Instance.tileMapBlocksBack.GetTile(light.position + direction);
            if (hasBackTile)
            {
                float blockFalloff = LightFalloffBack;
                bool hasFrontTile = GenerationManager.Instance.tileMapBlocksFront.GetTile(light.position + direction);
                if (hasFrontTile)
                    blockFalloff = LightFalloff;

                /* Spread light if the tile's channel color in this direction is lower in lightValue even after compensating
                 * its falloff. lightPassThreshold acts as an additional performance boost and defaults to lightFalloffBack. 
                 * It basically makes sure that only tiles that are at least two falloffs down are evaluated instead of just
                 * one falloff. This extra threshold can be adjusted but can result in ugly lighting artifacts with high values. */
                if (lightValueDirection + blockFalloff + lightPassThreshold < lightValue)
                {
                    lightValue = Mathf.Clamp(lightValue - blockFalloff, 0f, 1f);
                    Color currentColor = LightColors[light.position.x + direction.x, light.position.y + direction.y];
                    Color newColor =
                        (mode == LightingChannelMode.RED ?
                            new Color(lightValue, currentColor.g, currentColor.b) :
                        (mode == LightingChannelMode.GREEN ?
                            new Color(currentColor.r, lightValue, currentColor.b) :
                            new Color(currentColor.r, currentColor.g, lightValue)));

                    LightNode lightNode;
                    lightNode.position = light.position + direction;
                    lightNode.color = newColor;
                    LightColors[light.position.x + direction.x, light.position.y + direction.y] = newColor;

                    // Update tilemaps with visuals
                    GenerationManager.Instance.tileMapBlocksFront.SetColor(light.position + direction, newColor);
                    GenerationManager.Instance.tileMapBlocksBack.SetColor(light.position + direction, new Color(
                        Mathf.Clamp(newColor.r * backLayerShadowFactor, 0f, 1f), 
                        Mathf.Clamp(newColor.g * backLayerShadowFactor, 0f, 1f),
                        Mathf.Clamp(newColor.b * backLayerShadowFactor, 0f, 1f)));

                    queue.Enqueue(lightNode);
                }
            }
        }
    }

    /// <summary>
    /// Removes the given channel light of all queued nodes in 4 directions.
    /// This is one pass that scans left, down, right and up in that order, so
    /// at most 4 tiles are affected by a single pass.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="mode"></param>
    private void ExecuteLightingRemovalPass(Queue<LightNode> queue, LightingChannelMode mode)
    {
        // Get the LightNode that's first in line
        LightNode light = queue.Dequeue();
        float lightValue, lightValueLeft, lightValueDown, lightValueRight, lightValueUp;

        /* Detect passing over LightSources while removing to update them later, since when we touch
         * such a LightSource, it means we completely drowned out that color before and need to
         * update the light again to fill in the blanks correctly. */
        LightSource lightSource = GetLightSource(light.position);
        if (lightSource != null && !removalLights.Contains(lightSource))
            removalLights.Add(lightSource);

        removalPositions.Add(light.position);

        /* Obtain surrounding light values from the corresponding channel to lessen overhead
         * on extension passes. */
        switch (mode)
        {
            case LightingChannelMode.RED:
                if (light.color.r <= 0f)
                    return;

                lightValue = light.color.r;
                lightValueLeft = (light.position.x - 1 >= 0) ? LightColors[light.position.x - 1, light.position.y].r : -1f;
                lightValueDown = (light.position.y - 1 >= 0) ? LightColors[light.position.x, light.position.y - 1].r : -1f;
                lightValueRight = (light.position.x + 1 < worldWidth) ? LightColors[light.position.x + 1, light.position.y].r : -1f;
                lightValueUp = (light.position.y + 1 < worldHeight) ? LightColors[light.position.x, light.position.y + 1].r : -1f;
                break;
            case LightingChannelMode.GREEN:
                if (light.color.g <= 0f)
                    return;

                lightValue = light.color.g;
                lightValueLeft = (light.position.x - 1 >= 0) ? LightColors[light.position.x - 1, light.position.y].g : -1f;
                lightValueDown = (light.position.y - 1 >= 0) ? LightColors[light.position.x, light.position.y - 1].g : -1f;
                lightValueRight = (light.position.x + 1 < worldWidth) ? LightColors[light.position.x + 1, light.position.y].g : -1f;
                lightValueUp = (light.position.y + 1 < worldHeight) ? LightColors[light.position.x, light.position.y + 1].g : -1f;
                break;
            case LightingChannelMode.BLUE:
                if (light.color.b <= 0f)
                    return;

                lightValue = light.color.b;
                lightValueLeft = (light.position.x - 1 >= 0) ? LightColors[light.position.x - 1, light.position.y].b : -1f;
                lightValueDown = (light.position.y - 1 >= 0) ? LightColors[light.position.x, light.position.y - 1].b : -1f;
                lightValueRight = (light.position.x + 1 < worldWidth) ? LightColors[light.position.x + 1, light.position.y].b : -1f;
                lightValueUp = (light.position.y + 1 < worldHeight) ? LightColors[light.position.x, light.position.y + 1].b : -1f;
                break;
            default:
                return;
        }

        // Try and spread the light removal
        ExtendQueueLightRemovalPass(queue, light, lightValue, lightValueLeft, vectorLeft, mode);
        ExtendQueueLightRemovalPass(queue, light, lightValue, lightValueDown, vectorDown, mode);
        ExtendQueueLightRemovalPass(queue, light, lightValue, lightValueRight, vectorRight, mode);
        ExtendQueueLightRemovalPass(queue, light, lightValue, lightValueUp, vectorUp, mode);
    }

    /// <summary>
    /// Try to extend the removal of light in the given direction.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="light"></param>
    /// <param name="lightValue"></param>
    /// <param name="lightValueDirection"></param>
    /// <param name="direction"></param>
    /// <param name="mode"></param>
    private void ExtendQueueLightRemovalPass(Queue<LightNode> queue, LightNode light, float lightValue,
        float lightValueDirection, Vector3Int direction, LightingChannelMode mode)
    {
        if (lightValueDirection > 0f)
        {
            // Continue removing and extending while the block I'm looking at has a lower lightValue for this channel
            if (lightValueDirection < lightValue)
            {
                Color currentColor = LightColors[light.position.x + direction.x, light.position.y + direction.y];
                Color newColor =
                    (mode == LightingChannelMode.RED ?
                        new Color(0f, currentColor.g, currentColor.b) :
                    (mode == LightingChannelMode.GREEN ?
                        new Color(currentColor.r, 0f, currentColor.b) :
                        new Color(currentColor.r, currentColor.g, 0f)));

                LightNode lightRemovalNode;
                lightRemovalNode.position = light.position + direction;
                lightRemovalNode.color = currentColor;
                LightColors[light.position.x + direction.x, light.position.y + direction.y] = newColor;

                GenerationManager.Instance.tileMapBlocksFront.SetColor(light.position + direction, newColor);
                GenerationManager.Instance.tileMapBlocksBack.SetColor(light.position + direction, new Color(
                    Mathf.Clamp(newColor.r * backLayerShadowFactor, 0f, 1f),
                    Mathf.Clamp(newColor.g * backLayerShadowFactor, 0f, 1f),
                    Mathf.Clamp(newColor.b * backLayerShadowFactor, 0f, 1f)));

                queue.Enqueue(lightRemovalNode);
            }
            /* I just found a tile with a higher lightValue for this channel which means another strong light source
             * is nearby. Add tile to the update queue and spread their light after all removal to fill in the blanks 
             * this removal leaves behind.
             *   
             * Because we switch between two different falloff rates, this sometimes targets tiles within its own
             * light. These are later filtered out before spreading the light (using removalPositions). */
            else
            {
                LightNode lightNode;
                lightNode.position = light.position + direction;
                lightNode.color = LightColors[light.position.x + direction.x, light.position.y + direction.y];
                queueUpdate.Enqueue(lightNode);
            }
        }
    }
}