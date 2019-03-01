using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkObjectsHolder : MonoBehaviour
{
    //Object to hold all chunk objects
	public Transform chunkParent;
	//Plain chunk prefab
	public GameObject defaultChunk;

    GameObject[] chunkObjs;
	GameObject[] chunkBGs;
	MeshRenderer[] chunkLightmaps;

    void Start()
    {
        chunkObjs = new GameObject[0];
		chunkLightmaps = new MeshRenderer[0];
    }

    public void InitializeChunkObjects() {
        int chunkSize = WorldController.chunkSize;
        int worldChunkCount = WorldController.GetChunkCount();

        chunkObjs = new GameObject[worldChunkCount];
        chunkBGs = new GameObject[worldChunkCount];
        chunkLightmaps = new MeshRenderer[worldChunkCount];

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
            
            GameObject chunkBG = newChunkObj.transform.Find("ChunkBG").gameObject;
            BoxCollider2D bgCol = chunkBG.GetComponent<BoxCollider2D>();
            bgCol.offset = new Vector2(chunkSize/2, chunkSize/2);
            bgCol.size = new Vector2(chunkSize, chunkSize);
            chunkBGs[chunk] = chunkBG;

            Transform chunkLightmapObj = newChunkObj.transform.Find("LightMap");
            chunkLightmapObj.localPosition = new Vector3(chunkSize/2, chunkSize/2, chunkLightmapObj.localPosition.z);
            chunkLightmapObj.localScale = new Vector3(chunkSize, chunkSize, 1);

            Transform chunkBGLightmapObj = newChunkObj.transform.Find("BGLightMap");
            chunkBGLightmapObj.localPosition = new Vector3(chunkSize/2, chunkSize/2, chunkBGLightmapObj.localPosition.z);
            chunkBGLightmapObj.localScale = new Vector3(chunkSize, chunkSize, 1);
        }
    }

    public GameObject GetChunkObject(int chunk) {
        return chunkObjs[chunk];
    }

    public GameObject GetChunkBG(int chunk) {
        return chunkBGs[chunk];
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
