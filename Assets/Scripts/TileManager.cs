using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using UnityEngine.Tilemaps;

[Serializable]
public class TileInfo {
    public TileBase tileBase;
    public float lightStrength;
    public Color lightColor = Color.white;
    public float maxTileHealth = 10f;
    public int digToolTier = 0;
    public Item dropItem;

    public bool isSupportTile = false;
    public bool variableSupport = false;
    public bool topSupport, leftSupport, rightSupport, bottomSupport, backSupport = false;
    public SupportTile supportTile;
}

public class TileManager : Singleton<TileManager>
{
    //List of rule tile assets corresponding to integer tile indices
    public List<TileInfo> allTiles = new List<TileInfo>();

    //Array and list of total textures to pack to atlas (List converted to array on PackTexture call)
    public Texture2D[] texturesToPack;
    private List<Texture2D> texturesToPackList = new List<Texture2D>();
    
    //Array for saving Rect array output from PackTextures
    public Rect[] uv_coords;

    //Primary material for rendering tile mesh
    public Material tileMaterial;

    //Max atlas size
    int maxAtlasSize = 1024;
    
    //Atlas Texture to be generated
    public Texture2D atlasTex;

    //Keeps track of texture pack status
    private bool texturesPacked = false;

    private TileRenderer wRend;

    //Array of rotated indices for tile neighbors and uv coordinates
    private static readonly int[,] AngledNeighborIndices =
    {
        {0, 1, 2, 3, 4, 5, 6, 7}, // 0
        {5, 3, 0, 6, 1, 7, 4, 2}, // 90
        {7, 6, 5, 4, 3, 2, 1, 0}, // 180
        {2, 4, 7, 1, 6, 0, 3, 5}, // 270
    };
    private static readonly int[,] AngledUVIndices = {
        {0, 1, 2, 3}, // 0
        {2, 0, 3, 1}, // 90
        {3, 2, 1, 0}, // 180
        {1, 3, 0, 2}, // 270
    };

    void Start()
    {
        wRend = GetComponent<TileRenderer>();
    }

    //Public methods
    //-------------------------------------------------------------------------------
        //Main texture packer
        /*public void PackRuleTileTextures() {
            //Initialize atlas texture with our specifications, and then pack textures into it
            atlasTex = new Texture2D(maxAtlasSize, maxAtlasSize, TextureFormat.ARGB32, false);
            atlasTex.filterMode = FilterMode.Point;
            
            int ruleTileIndex = 0;
            foreach (TileInfo tile in allTiles) {
                if (tile == null) {
                    Debug.Log("Null rule tile at index " + ruleTileIndex);
                    return;
                }

                foreach (MeshRuleTile.TilingRule tr in tile.tileBase.m_TilingRules) {
                    int spriteIndex = 0;
                    foreach (Texture2D sprite in tr.m_Sprites) {
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
        }*/

        public void InitializeSupportBlocks() {
            foreach (TileInfo tile in allTiles) {
                SupportTile sb = new SupportTile();
                sb.active = tile.isSupportTile;

                if (!tile.isSupportTile)
                    continue;
                
                sb.anySupport = tile.variableSupport;
                sb.requiredSupport[(int)SupportSide.top] = tile.topSupport;
                sb.requiredSupport[(int)SupportSide.left] = tile.leftSupport;
                sb.requiredSupport[(int)SupportSide.right] = tile.rightSupport;
                sb.requiredSupport[(int)SupportSide.bottom] = tile.bottomSupport;
                sb.requiredSupport[(int)SupportSide.back] = tile.backSupport;
                tile.supportTile = sb;
            }
        }

        //Public getter method for getting final UV coordinates for tile based on existing rules
        /*public Vector2[] GetTileUV(int[,] world, int x, int y) {
            if (!texturesPacked) {
                Debug.Log("Can't get tile UVs, textures not packed yet...");
                return null;
            }

            int tileIndex = world[x, y];
            TileInfo tile = allTiles[tileIndex];
            int[] neighbors = GetNeighbors(world, x, y);
            foreach (MeshRuleTile.TilingRule tr in tile.tileBase.m_TilingRules) {
                int matchedAngle = CheckRule(tr, neighbors);
                if (matchedAngle != -1) {
                    return GetCoordsFromRule(tr, matchedAngle);
                }
            }
            Debug.Log("Couldn't find matching TilingRule for tile " + x + ", " + y);
            return null;
        }*/
        
        //Public getter method to check texture pack status
        public bool AreTexturesPacked() {
            return texturesPacked;
        }

        public TileInfo GetTile(int index) {
            return allTiles[index];
        }

    //

    //Internal methods
    //-------------------------------------------------------------------------------
        //Using a specific matched tile rule, pick one of its textures and return the uv coordinates
        Vector2[] GetCoordsFromRule(MeshRuleTile.TilingRule tr, int angle) {
            if (tr.m_Output == MeshRuleTile.TilingRule.OutputSprite.Single) {
                return GetUVCoords(tr.m_SpriteAtlasIndices[0], angle);
            } else {
                int randomTexture = UnityEngine.Random.Range(0, tr.m_SpriteAtlasIndices.Length);
                return GetUVCoords(tr.m_SpriteAtlasIndices[randomTexture], angle);
            }
        }

        Vector2[] GetCoordsFromRule(MeshRuleTile.TilingRule tr) {
            return GetCoordsFromRule(tr, 0);
        }

        //Using angle and UV-Texture index, get appropriate texture uv coordinates for specific tile texture
        Vector2[] GetUVCoords(int textureIndex, int angle) {
            Rect uv_rect = uv_coords[textureIndex];
            Vector2[] actualCoords = new Vector2[4];

            float rectWidthRadius = uv_rect.width;
            float rectHeightRadius = uv_rect.height;
            actualCoords[AngledUVIndices[angle/90, 0]] = new Vector2(uv_rect.position.x, uv_rect.position.y + rectHeightRadius);
            actualCoords[AngledUVIndices[angle/90, 1]] = new Vector2(uv_rect.position.x + rectWidthRadius, uv_rect.position.y + rectHeightRadius);
            actualCoords[AngledUVIndices[angle/90, 2]] = new Vector2(uv_rect.position.x, uv_rect.position.y);
            actualCoords[AngledUVIndices[angle/90, 3]] = new Vector2(uv_rect.position.x + rectWidthRadius, uv_rect.position.y);

            return actualCoords;
        }

        Vector2[] GetUVCoords(int textureIndex) {
            return GetUVCoords(textureIndex, 0);
        }

        //Method for getting array of properly indexed array of neighbors of a tile
        int[] GetNeighbors(int[,] world, int x, int y) {
                int neighborIndex = 0;
                int[] neighbors = new int[8];
                for (int i = 1; i >= -1; i--) {
                    for (int j = -1; j <= 1; j++) {
                        if (i == 0 && j == 0) {
                            continue;
                        }
                        else if (y+i < world.GetLowerBound(1) || y+i > world.GetUpperBound(1) || x+j < world.GetLowerBound(0) || x+j > world.GetUpperBound(0)) {
                            neighbors[neighborIndex] = 0;
                        } else {
                            neighbors[neighborIndex] = world[x+j, y+i];
                        }
                        neighborIndex++;
                    }
                }
                return neighbors;
            }

        //Determine matching rule for specific rule tile based on neighbor states
        int CheckRule(MeshRuleTile.TilingRule tr, int[] neighbors) {
            int maxAngle = 1;
            if (tr.m_RuleTransform == MeshRuleTile.TilingRule.Transform.Rotated) {
                maxAngle = 270;
            }

            for (int angle = 0; angle <= maxAngle; angle += 90) {
                if (RuleMatch(tr, neighbors, angle)) {
                    return angle;
                }
            }

            return -1;
        }

        //Basic neighbor check
        bool RuleMatch(MeshRuleTile.TilingRule tr, int[] neighbors, int angle) {
            int[] ruleNeighbors = tr.m_Neighbors;
            for (int neighbor = 0; neighbor < neighbors.Length; neighbor++) {
                int rotatedNeighbor = AngledNeighborIndices[angle/90, neighbor];
                if (ruleNeighbors[neighbor] == MeshRuleTile.TilingRule.Neighbor.DontCare) {
                    continue;
                }
                if (ruleNeighbors[neighbor] == MeshRuleTile.TilingRule.Neighbor.This) {
                    if (neighbors[rotatedNeighbor] == 0)
                        return false;
                } else if (ruleNeighbors[neighbor] == MeshRuleTile.TilingRule.Neighbor.NotThis) {
                    if (neighbors[rotatedNeighbor] != 0) {
                        return false;
                    }
                }
            }
            return true;
        }
    //
}
