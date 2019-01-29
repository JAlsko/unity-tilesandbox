using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using UnityEngine.Tilemaps;

public class RuleTileManager : MonoBehaviour
{
    public List<RuleTile> allTiles = new List<RuleTile>();
    public Texture2D[] texturesToPack;
    private List<Texture2D> texturesToPackList = new List<Texture2D>();
    public Rect[] uv_coords;

    public Material tileMaterial;

    int atlasSize = 1024;
    public Texture2D atlasTex;

    private bool texturesPacked = false;

    void Start()
    {
        atlasTex = new Texture2D(atlasSize, atlasSize, TextureFormat.ARGB32, false);
        atlasTex.filterMode = FilterMode.Point;
        PackRuleTileTextures();
    }

    void Update()
    {
        
    }

    public Vector2[] GetUVCoords(int textureIndex) {
        Rect uv_rect = uv_coords[textureIndex];
        Vector2[] actualCoords = new Vector2[4];

        float rectWidthRadius = uv_rect.width;
        float rectHeightRadius = uv_rect.height;
        actualCoords[0] = new Vector2(uv_rect.position.x, uv_rect.position.y + rectHeightRadius);
        actualCoords[1] = new Vector2(uv_rect.position.x + rectWidthRadius, uv_rect.position.y + rectHeightRadius);
        actualCoords[2] = new Vector2(uv_rect.position.x, uv_rect.position.y);
        actualCoords[3] = new Vector2(uv_rect.position.x + rectWidthRadius, uv_rect.position.y);

        return actualCoords;
    }

    [ContextMenu("PackRuleTileTextures")]
    public void PackRuleTileTextures() {
        int ruleTileIndex = 0;
        foreach (RuleTile rt in allTiles) {
            if (rt == null) {
                Debug.Log("Null rule tile at index " + ruleTileIndex);
                return;
            }

            foreach (RuleTile.TilingRule tr in rt.m_TilingRules) {
                int spriteIndex = 0;
                foreach (Texture2D sprite in tr.m_Sprites) {
                    Debug.Log(spriteIndex);
                    int spriteCount = texturesToPackList.Count;
                    tr.m_SpriteAtlasIndices[spriteIndex] = spriteCount;
                    texturesToPackList.Add(sprite);
                    spriteIndex++;
                }
            }
            ruleTileIndex++;
        }

        texturesToPack = texturesToPackList.ToArray();
        uv_coords = atlasTex.PackTextures(texturesToPack, 0, 2048, false);
        tileMaterial.SetTexture("_MainTex", atlasTex);
        texturesPacked = true;
    }

    public bool AreTexturesPacked() {
        return texturesPacked;
    }

    public int[] GetNeighbors(int[,] world, int x, int y) {
            int neighborIndex = 0;
            int[] neighbors = new int[8];
            for (int i = y+1; i > y-1; i--) {
                for (int j = x-1; j < x+1; j++) {
                    if (i < world.GetLowerBound(1) || i > world.GetUpperBound(1) || j < world.GetLowerBound(0) || j > world.GetUpperBound(0)) {
                        neighbors[neighborIndex] = 0;
                    } else {
                        neighbors[neighborIndex] = world[j, i];
                    }
                    neighborIndex++;
                }
            }
            return neighbors;
        }

    public Vector2[] GetTileUV(int[,] world, int x, int y) {
        if (!texturesPacked) {
            Debug.Log("Can't get tile UVs, textures not packed yet...");
            return null;
        }

        RuleTile rt = allTiles[world[x,y]];
        int[] neighbors = GetNeighbors(world, x, y);
        foreach (RuleTile.TilingRule tr in rt.m_TilingRules) {
            if (RuleMatch(tr, neighbors)) {
                return GetCoordsFromRule(tr);
            }
        }
        Debug.Log("Couldn't find matching TilingRule for tile " + x + ", " + y);
        return null;
    }

    bool RuleMatch(RuleTile.TilingRule tr, int[] neighbors) {
        int[] ruleNeighbors = tr.m_Neighbors;
        for (int neighbor = 0; neighbor < neighbors.Length; neighbor++) {
            if (ruleNeighbors[neighbor] == RuleTile.TilingRule.Neighbor.This) {
                if (neighbors[neighbor] == 0) 
                    return false;
            } else if (ruleNeighbors[neighbor] == RuleTile.TilingRule.Neighbor.NotThis) {
                if (neighbors[neighbor] != 0) {
                    return false;
                }
            }
        }
        return true;
    }

    Vector2[] GetCoordsFromRule(RuleTile.TilingRule tr) {
        if (tr.m_Output == RuleTile.TilingRule.OutputSprite.Single) {
            return GetUVCoords(tr.m_SpriteAtlasIndices[0]);
        } else {
            int randomTexture = UnityEngine.Random.Range(0, tr.m_SpriteAtlasIndices.Length);
            return GetUVCoords(tr.m_SpriteAtlasIndices[randomTexture]);
        }
    }
}
