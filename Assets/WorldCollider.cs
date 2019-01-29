using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldController))]
public class WorldCollider : MonoBehaviour
{
    public GameObject colliderParent;
    int smallSearchRadius = 1;

    Dictionary<int, BoxCollider2D> chunkCols = new Dictionary<int, BoxCollider2D>();

    private WorldController wCon;
    private WorldRenderer wRend;

    void Start() {
        wCon = GetComponent<WorldController>();
        wRend = GetComponent<WorldRenderer>();
    }

    //High Level Collider Functions
    //-------------------------------------------------------------------------------
        //Function for regenerating colliders for an entire chunk
        public void GenerateChunkColliders(GameObject chunkObj, int[,] world, int x, int y) {
            if (world == null) {
                Debug.LogError("Can't generate colliders - empty world!");
            }

            int chunkSize = WorldController.chunkSize;
            for (int i = y; i < y+chunkSize; i++) {
                for (int j = x; j < x+chunkSize; j++) {
                    if (isTileOpen(world, j, i)) {
                        GenerateSingleCollider(chunkObj, j-x, i-y, j, i);
                    }
                    else {
                        RemoveSingleCollider(chunkObj, j-x, i-y, j, i);
                    }
                }
            }
            
        }
        
        //Simplified version of above function
        public void GenerateChunkColliders(int chunk, int[,] world) {
            GameObject chunkObj = wRend.GetChunkObject(chunk);
            Vector2Int chunkPos = wCon.GetChunkPosition(chunk);
            GenerateChunkColliders(chunkObj, world, chunkPos.x, chunkPos.y);
        }

        //Function for regenerating colliders for one tile and its adjacent tiles
        public void GenerateTileColliders(int[,] world, int x, int y) {
            for (int i = y-smallSearchRadius; i < y+smallSearchRadius; i++) {
                for (int j = x-smallSearchRadius; j < x+smallSearchRadius; j++) {
                    int chunk = wCon.GetChunk(j, i);
                    GameObject chunkObj = wRend.GetChunkObject(chunk);
                    Vector2Int chunkPos = wCon.GetChunkPosition(chunk);
                    int inChunkX = j - chunkPos.x;
                    int inChunkY = i - chunkPos.y;

                    if (isTileOpen(world, j, i)) {
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
        //Generates a collider for one tile only
        void GenerateSingleCollider(GameObject colliderParent, int x, int y, int realX, int realY) {
            Vector2Int pos = new Vector2Int(realX, realY);
            int posHash = HashableInt(pos);
            if (chunkCols.ContainsKey(posHash)) {
                return;
            }
            BoxCollider2D newCol = colliderParent.AddComponent<BoxCollider2D>();
            newCol.size = Vector2.one;
            newCol.offset = new Vector2(x + .5f, y + .5f);
            chunkCols[posHash] = newCol;
        }

        //Removes a collider for one tile only
        void RemoveSingleCollider(GameObject colliderParent, int x, int y, int realX, int realY) {
            Vector2Int pos = new Vector2Int(realX, realY);
            int posHash = HashableInt(pos);
            if (!chunkCols.ContainsKey(posHash)) {
                return;
            }
            Destroy(chunkCols[posHash]);
            chunkCols.Remove(posHash);
        }

        //Checks a tile's adjacent tiles to see if it's at all open (in order to determine if it needs a collider)
        bool isTileOpen(int[,] world, int x, int y) {
            if (x > world.GetUpperBound(0) || x < world.GetLowerBound(0) || y > world.GetUpperBound(1) || y < world.GetLowerBound(1)) {
                return false;
            }
            if (world[x,y] == 0) {
                return false;
            }

            for (int i = x-1; i < x+2; i++) {
                for (int j = y-1; j < y+2; j++) {
                    if (i > world.GetUpperBound(0) || i < world.GetLowerBound(0) || j > world.GetUpperBound(1) || j < world.GetLowerBound(1)) {
                        return true;
                    }       

                    if (world[i,j] == 0) {
                        return true;
                    }
                }
            }

            return false;
        }
    //

    //Helper Functions
    //-------------------------------------------------------------------------------
        //Helper function to convert a Vector2 to an integer (for collider dictionary)
        static int HashableInt(Vector2Int vector)
        {
            int x = Mathf.RoundToInt(vector.x);
            int y = Mathf.RoundToInt(vector.y);
            return x * 1000 + y * 1000000;
        }
    //
}