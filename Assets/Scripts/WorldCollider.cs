using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(ChunkObjectsHolder))]
public class WorldCollider : MonoBehaviour
{
    int smallSearchRadius = 1;

    //A dictionary of colliders indexed by a hash of their position
    Dictionary<int, BoxCollider2D> chunkCols = new Dictionary<int, BoxCollider2D>();

    private WorldController wCon;
    private TileRenderer wRend;
    private ChunkObjectsHolder cObjs;

    void Start() {
        wCon = GetComponent<WorldController>();
        wRend = GetComponent<TileRenderer>();
        cObjs = GetComponent<ChunkObjectsHolder>();
    }

    //High Level Collider Functions
    //-------------------------------------------------------------------------------
        /// <summary>
        /// Generates colliders for all tiles in a chunk.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public void GenerateChunkColliders(int chunk) {
            Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);
            int x = chunkPos.x;
            int y = chunkPos.y;

            GameObject chunkObj = cObjs.GetChunkObject(chunk);

            int chunkSize = WorldController.chunkSize;
            for (int i = y; i < y+chunkSize; i++) {
                for (int j = x; j < x+chunkSize; j++) {
                    if (wCon.isTileOpen(j, i)) {
                        GenerateSingleCollider(chunkObj, j-x, i-y, j, i);
                    }
                    else {
                        RemoveSingleCollider(chunkObj, j-x, i-y, j, i);
                    }
                }
            }
            
        }

        /// <summary>
        /// Generates colliders for a tile and its relevant neighbors
        /// </summary>
        /// <param name="world"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void GenerateTileColliders(int[,] world, int x, int y) {
            for (int i = y-smallSearchRadius; i <= y+smallSearchRadius; i++) {
                for (int j = x-smallSearchRadius; j <= x+smallSearchRadius; j++) {
                    int chunk = WorldController.GetChunk(j, i);
                    GameObject chunkObj = cObjs.GetChunkObject(chunk);
                    Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);
                    int inChunkX = j - chunkPos.x;
                    int inChunkY = i - chunkPos.y;

                    if (wCon.isTileOpen(j, i)) {
                        GenerateSingleCollider(chunkObj, inChunkX, inChunkY, j, i);
                    } else {
                        RemoveSingleCollider(chunkObj, inChunkX, inChunkY, j, i);
                    }
                }
            }
        }
    //

    //Low Level Collider Functions
    //-------------------------------------------------------------------------------
        /// <summary>
        /// Creates a collider for a single tile (if one doesn't exist)
        /// </summary>
        /// <param name="colliderParent"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="realX"></param>
        /// <param name="realY"></param>
        /// <returns></returns>
        void GenerateSingleCollider(GameObject colliderParent, int x, int y, int realX, int realY) {
            Vector2Int pos = new Vector2Int(realX, realY);
            int posHash = Helpers.HashableInt(pos);
            if (chunkCols.ContainsKey(posHash)) {
                return;
            }
            BoxCollider2D newCol = colliderParent.AddComponent<BoxCollider2D>();
            newCol.size = Vector2.one;
            newCol.offset = new Vector2(x + .5f, y + .5f);
            chunkCols[posHash] = newCol;
        }

        /// <summary>
        /// Removes a collider for a single tile (if one does exist)
        /// </summary>
        /// <param name="colliderParent"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="realX"></param>
        /// <param name="realY"></param>
        /// <returns></returns>
        void RemoveSingleCollider(GameObject colliderParent, int x, int y, int realX, int realY) {
            Vector2Int pos = new Vector2Int(realX, realY);
            int posHash = Helpers.HashableInt(pos);
            if (!chunkCols.ContainsKey(posHash)) {
                return;
            }
            Destroy(chunkCols[posHash]);
            chunkCols.Remove(posHash);
        }
    //
}