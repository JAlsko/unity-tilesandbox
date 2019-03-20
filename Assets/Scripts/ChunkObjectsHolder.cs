using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkObjectsHolder : MonoBehaviour
{
    //Object to hold all chunk objects
	public Transform chunkParent;
	//Plain chunk prefab
	public GameObject defaultChunk;

    GameObject[] chunkObjs;
	GameObject[] chunkFGs;
	GameObject[] chunkBGs;
	GameObject[] chunkCols;
	MeshRenderer[] chunkLightmaps;
	MeshRenderer[] chunkBGLightmaps;
    Tilemap[] chunkLiquidTilemaps;

    public void InitializeChunkObjects() {
        int chunkSize = WorldController.chunkSize;
        int worldChunkCount = WorldController.GetChunkCount();

        chunkObjs = new GameObject[worldChunkCount];
        chunkFGs = new GameObject[worldChunkCount];
        chunkBGs = new GameObject[worldChunkCount];
        chunkCols = new GameObject[worldChunkCount];
        chunkLightmaps = new MeshRenderer[worldChunkCount];
        chunkBGLightmaps = new MeshRenderer[worldChunkCount];
        chunkLiquidTilemaps = new Tilemap[worldChunkCount];

        //Initializing chunk object array
        for (int chunk = 0; chunk < worldChunkCount; chunk++) {
            GameObject newChunkObj = Instantiate(defaultChunk);
            Vector2Int newChunkPos = WorldController.GetChunkPosition(chunk);
            newChunkObj.transform.parent = chunkParent;
            newChunkObj.name = "Chunk_" + chunk;
            newChunkObj.transform.position = new Vector3(newChunkPos.x, newChunkPos.y, 0);
            newChunkObj.transform.rotation = Quaternion.identity;
            newChunkObj.transform.localScale = Vector3.one;
            chunkObjs[chunk] = newChunkObj;
            
            //GameObject chunkBG = newChunkObj.transform.Find("ChunkBG").gameObject;
            GameObject chunkFG = newChunkObj.transform.Find("FGTilemap").gameObject;
            GameObject chunkBG = newChunkObj.transform.Find("BGTilemap").gameObject;
            GameObject chunkCol = newChunkObj.transform.Find("ChunkBG").gameObject;
            BoxCollider2D bgCol = chunkCol.GetComponent<BoxCollider2D>();
            bgCol.offset = new Vector2(chunkSize/2, chunkSize/2);
            bgCol.size = new Vector2(chunkSize, chunkSize);
            chunkFGs[chunk] = chunkFG;
            chunkFG.GetComponentInChildren<Tilemap>().SetTileFlags(Vector3Int.zero, TileFlags.None);
            chunkBGs[chunk] = chunkBG;

            Transform chunkLightmapObj = newChunkObj.transform.Find("LightMap");
            chunkLightmapObj.localPosition = new Vector3(chunkSize/2, chunkSize/2, chunkLightmapObj.localPosition.z);
            chunkLightmapObj.localScale = new Vector3(chunkSize, chunkSize, 1);
            chunkLightmaps[chunk] = chunkLightmapObj.GetComponent<MeshRenderer>();

            Transform chunkBGLightmapObj = newChunkObj.transform.Find("BGLightMap");
            chunkBGLightmapObj.localPosition = new Vector3(chunkSize/2, chunkSize/2, chunkBGLightmapObj.localPosition.z);
            chunkBGLightmapObj.localScale = new Vector3(chunkSize, chunkSize, 1);
            chunkBGLightmaps[chunk] = chunkBGLightmapObj.GetComponent<MeshRenderer>();

            Tilemap chunkLiquidTilemap = newChunkObj.transform.Find("LiquidTilemap").GetComponentInChildren<Tilemap>();
            chunkLiquidTilemaps[chunk] = chunkLiquidTilemap;
        }
    }

    public GameObject GetChunkObject(int chunk) {
        return chunkObjs[chunk];
    }

    public GameObject GetChunkFG(int chunk) {
        return chunkFGs[chunk];
    }

    public GameObject GetChunkBG(int chunk) {
        return chunkBGs[chunk];
    }

    public Tilemap GetChunkLiquidTilemap(int chunk) {
        if (chunk < 0 || chunk > WorldController.GetChunkCount()) {
            Debug.Log("Trying to get tilemap for chunk " + chunk);
            return null;
        }
        return chunkLiquidTilemaps[chunk];
    }

    public void HideChunk(int chunk) {
        chunkObjs[chunk].SetActive(false);
    }

    public void ShowChunk(int chunk) {
        chunkObjs[chunk].SetActive(true);
    }

    public void UpdateShownChunks(int[] chunksToRender, int[] chunksToHide) {
        foreach (int chunk in chunksToRender) {
            if (chunk >= WorldController.GetChunkCount() || chunk < 0) {
                continue;
            }
            ShowChunk(chunk);
        }

        foreach (int chunk in chunksToHide) {
            if (chunk >= WorldController.GetChunkCount() || chunk < 0) {
                continue;
            }
            HideChunk(chunk);
        }
    }

    public List<Transform> GetAllChunkParents() {
        List<Transform> allChunkParents = new List<Transform>();
        foreach (GameObject chunkObj in chunkObjs) {
            allChunkParents.Add(chunkObj.transform);
        }

        return allChunkParents;
    }
}
