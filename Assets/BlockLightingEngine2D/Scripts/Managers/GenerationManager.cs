using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles some simple terrain generation for light testing.
/// </summary>
public class GenerationManager : Singleton<GenerationManager>
{
    [Header("General Components")]
    public Tilemap tileMapBlocksFront;
    public Tilemap tileMapBlocksBack;
    public List<Tile> tiles;
    public enum TileType
    {
        AIR,
        DIRT,
        STONE,
        VINE
    }
    [Header("World Settings")]
    public int seed;
    public int worldWidth = 256;
    public int worldHeight = 256;
    [Range(0f, 1f)]
    public float surfaceHeightPosition;
    public float surfacePerlinSpeed;
    public float surfacePerlinHeightMultiplier;

    [Header("Generation")]
    public float caveMapPerlinSpeed;
    public float caveMapPerlinLevel;
    public float caveZonePerlinSpeed;
    public float caveZonePerlinLevel;
    public float cavePerlinSpeed;
    public float cavePerlinLevel;
    [Space]
    public float stoneDepthMin = 0;
    public float stoneDepthMax = Mathf.Infinity;
    public float stoneZonePerlinSpeed;
    public float stoneZonePerlinLevel;
    public float stonePerlinSpeed;
    public float stonePerlinLevel;

    private Vector2 perlinOffset;
    private float perlinOffsetMax = 100000f;
    private int[] surfaceHeights;
    private int surfaceHeightAverage;
    private float perlinAddition;


    private void Start()
    {
        surfaceHeights = new int[worldWidth];
        surfaceHeightAverage = (int)(worldHeight * surfaceHeightPosition);
        Camera.main.transform.position = new Vector3(worldWidth / 2, worldHeight * surfaceHeightPosition,
            Camera.main.transform.position.z);

        SetSeed(seed);
    }

    /// <summary>
    /// Generates a simplified terrain based on the current seed.
    /// </summary>
    public void GenerateTerrain()
    {
        SetSeed(seed);
        perlinOffset = new Vector2(UnityEngine.Random.Range(0f, perlinOffsetMax),
            UnityEngine.Random.Range(0f, perlinOffsetMax));

        // Generates a line of surface heights. Everything below it will be considered terrain
        GenerateSurfaceLevel();

        // Fill in terrain below each surface block
        for (int h = 0; h < worldWidth; h++)
        {
            int startHeight = surfaceHeights[h];            
            for (int v = startHeight; v >= 0; v--)
            {
                Vector3Int tilePosition = new Vector3Int(h, v, 0);
                TileType tileToSpawn = TileType.DIRT;

                // A simple perlin evaluation system checking if I should spawn stone or caves
                if (CheckPerlinEligibility(tilePosition, stoneDepthMin, stoneDepthMax,
                    stonePerlinSpeed, stonePerlinLevel,
                    stoneZonePerlinSpeed, stoneZonePerlinLevel))
                    tileToSpawn = TileType.STONE;
                if (CheckPerlinEligibility(tilePosition, 0, Mathf.Infinity,
                    cavePerlinSpeed, cavePerlinLevel,
                    caveZonePerlinSpeed, caveZonePerlinLevel,
                    caveMapPerlinSpeed, caveMapPerlinLevel))
                    tileToSpawn = TileType.AIR;

                // Update tilemaps
                tileMapBlocksBack.SetTile(tilePosition, tiles[(int)TileType.DIRT]);
                tileMapBlocksBack.SetColor(tilePosition, Color.black);
                if (tileToSpawn != TileType.AIR)
                {                    
                    tileMapBlocksFront.SetTile(tilePosition, tiles[(int)tileToSpawn]);
                    if (LightingManager.Instance.LightColors[tilePosition.x, tilePosition.y] == Color.clear)                   
                        tileMapBlocksFront.SetColor(tilePosition, Color.black);
                }
            }
        }
        // Done generation, now update all ambient lights (one block above each surface block)
        LightingManager.Instance.UpdateAllLights(true);
    }

    /// <summary>
    /// A quick check whether the given PerlinNoise parameters exceed the given threshold.
    /// Used to check whether a type of block can spawn based on the perlin value for example.
    /// </summary>
    /// <param name="tilePosition"></param>
    /// <param name="perlinSpeed"></param>
    /// <param name="perlinLevel"></param>
    /// <returns></returns>
    private bool CheckPerlinLevel(Vector3Int tilePosition, float perlinSpeed, float perlinLevel)
    {
        return (Mathf.PerlinNoise(
            perlinOffset.x + tilePosition.x * perlinSpeed,
            perlinOffset.y + tilePosition.y * perlinSpeed) >= perlinLevel);
    }


    /// <summary>
    /// Uses multiple CheckPerlinLevel calls to extensively check whether a certain type of
    /// block can be spawned.
    /// </summary>
    /// <param name="tilePosition"></param>
    /// <param name="depthMin"></param>
    /// <param name="depthMax"></param>
    /// <param name="perlinSpeed"></param>
    /// <param name="perlinLevel"></param>
    /// <param name="zonePerlinSpeed"></param>
    /// <param name="zonePerlinLevel"></param>
    /// <param name="mapPerlinSpeed"></param>
    /// <param name="mapPerlinLevel"></param>
    /// <returns></returns>
    private bool CheckPerlinEligibility(Vector3Int tilePosition, float depthMin, float depthMax, float perlinSpeed, float perlinLevel,
        float zonePerlinSpeed = -1f, float zonePerlinLevel = -1f, float mapPerlinSpeed = -1f, float mapPerlinLevel = -1f)
    {
        int depth = surfaceHeights[tilePosition.x] - tilePosition.y;
        if (!(depth >= depthMin && depth < depthMax))
            return false;

        if ((mapPerlinSpeed == -1f && mapPerlinLevel == -1f) ||
            CheckPerlinLevel(tilePosition, mapPerlinSpeed, mapPerlinLevel))
        {
            if ((zonePerlinSpeed == -1f && zonePerlinLevel == -1f) ||
                CheckPerlinLevel(tilePosition, zonePerlinSpeed, zonePerlinLevel))
            {
                return (CheckPerlinLevel(tilePosition, perlinSpeed, perlinLevel));
            }
        }
        return false;
    }


    /// <summary>
    /// Generates the surface heights of the world using perlin noise with the current seed.
    /// </summary>
    /// <returns></returns>
    private void GenerateSurfaceLevel()
    {
        int index = 0;
        while (true)
        {
            // Stop if out of bounds
            if (index >= worldWidth)
                return;

            float noiseX = perlinOffset.x + perlinAddition;
            float noiseY = perlinOffset.y + perlinAddition;
            float addition = (Mathf.PerlinNoise(noiseX, noiseY)) * surfacePerlinHeightMultiplier;
            surfaceHeights[index] = surfaceHeightAverage + (int)addition;

            // Add an ambient LightSource one block above this surface block
            LightingManager.Instance.CreateLightSource(
                new Vector3Int(index, surfaceHeights[index] + 1, 0), 
                LightingManager.Instance.ambientLightColor, 1f, false);

            perlinAddition += surfacePerlinSpeed;
            index++;
        }
    }


    /// <summary>
    /// Changes the seed of the game to a new seed.
    /// </summary>
    /// <param name="newSeed"></param>
    public void SetSeed(int newSeed = -1)
    {
        seed = newSeed == -1 ? (int)DateTime.Now.Ticks : newSeed;
        UnityEngine.Random.InitState(seed);
    }


    /// <summary>
    /// Returns whether the given position is an air block or not.
    /// </summary>
    /// <param name="tilePosition"></param>
    /// <returns></returns>
    public bool IsAirBlock(Vector3Int tilePosition)
    {
        return (!tileMapBlocksFront.GetTile(tilePosition) && 
            !tileMapBlocksBack.GetTile(tilePosition));
    }

    /// <summary>
    /// Clears all tile and generation data.
    /// </summary>
    public void Clear()
    {
        tileMapBlocksFront.ClearAllTiles();
        tileMapBlocksBack.ClearAllTiles();

        surfaceHeights = new int[worldWidth];
        perlinAddition = 0;
    }
}