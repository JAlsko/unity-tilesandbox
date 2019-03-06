using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class UVTile {
	public string name;
	public Vector2[] uv_coords = new Vector2[4];
}

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(WorldCollider))]
[RequireComponent(typeof(TileManager))]
[RequireComponent(typeof(ChunkObjectsHolder))]
public class TileRenderer : MonoBehaviour {
	//Other neccessary world scripts
	private WorldController wCon;
	private WorldCollider wCol;
	private TileManager tMgr;
	private ChunkObjectsHolder cObjs;

	//Object to hold all chunk objects
	public Transform chunkParent;
	//Plain chunk prefab
	public GameObject defaultChunk;

	//Array of existing chunk objects
	GameObject[] chunkObjs;
	GameObject[] chunkBGs;
	MeshRenderer[] chunkLightmaps;

	void Start() {
		wCon = GetComponent<WorldController>();
		wCol = GetComponent<WorldCollider>();
		tMgr = GetComponent<TileManager>();
		cObjs = GetComponent<ChunkObjectsHolder>();

		chunkObjs = new GameObject[0];
		chunkLightmaps = new MeshRenderer[0];
	}

	//Render Functions
	//-------------------------------------------------------------------------------

		/// <summary>
        /// Renders chunk tiles for every chunk.
        /// </summary>
        /// <returns></returns>
		public void RenderAllChunks() {
			for (int chunk = 0; chunk < WorldController.GetChunkCount(); chunk++) {
				RenderChunkTiles(chunk);
				HideChunk(chunk);
			}
		}

		/// <summary>
		/// The primary function for rendering a chunk's tiles.
		/// Builds grid mesh and maps appropriate tile texture uvs to each set of vertices.
		/// </summary>
		/// <param name="chunk"></param>
		/// <returns></returns>
		public void RenderChunkTiles(int chunk) {
			int[,] chunkTiles = wCon.GetChunkTiles(chunk, 0);
			int[,] chunkBGTiles = wCon.GetChunkTiles(chunk, 1);

			int chunkSize = WorldController.chunkSize;

			int vertexCount = chunkSize * chunkSize * 4;

			Vector2Int chunkPos = WorldController.GetChunkPosition(chunk);

			GameObject chunkObj = cObjs.GetChunkObject(chunk);
			GameObject chunkBGObj = cObjs.GetChunkBG(chunk);

			if (!chunkObj.GetComponent<MeshFilter>() || !chunkBGObj.GetComponent<MeshFilter>()) {
				Debug.LogError("Chunk #" + chunk + "'s object has no mesh! Can't render!");
				return;
			}

			MeshFilter chunkMF = chunkObj.GetComponent<MeshFilter>();
			MeshFilter bgMF = chunkBGObj.GetComponent<MeshFilter>();

			Vector3[] fgVertices = new Vector3[vertexCount];
			int[] fgTriangles = new int[(vertexCount/2)*3];
			Vector3[] fgNormals = new Vector3[vertexCount];
			Vector2[] fgUV = new Vector2[vertexCount];

			int vertexIndex = 0;
			int triangleIndex = 0;
			int tileIndex = 0;

			//Loop through each tile in the world
			for (float x = 0; x < chunkSize; x++) {
				for (float y = 0; y < chunkSize; y++) {
					if (chunkTiles[(int)x, (int)y] == 0) {
						continue;
					}

					//Set up vertices in TopLeft->TopRight->BottomLeft->BottomRight order
					fgVertices[vertexIndex] = new Vector3(x, y+1, 0);
					fgVertices[vertexIndex + 1] = new Vector3(x+1, y+1, 0);
					fgVertices[vertexIndex + 2] = new Vector3(x, y, 0);
					fgVertices[vertexIndex + 3] = new Vector3(x+1, y, 0);

					//Set up triangles in 0->3->2 and 0->1->3 orders
					fgTriangles[triangleIndex+0] = vertexIndex+0;
					fgTriangles[triangleIndex+1] = vertexIndex+3;
					fgTriangles[triangleIndex+2] = vertexIndex+2;
					fgTriangles[triangleIndex+3] = vertexIndex+0;
					fgTriangles[triangleIndex+4] = vertexIndex+1;
					fgTriangles[triangleIndex+5] = vertexIndex+3;
					
					fgNormals[vertexIndex] = Vector3.back;
					fgNormals[vertexIndex+1] = Vector3.back;
					fgNormals[vertexIndex+2] = Vector3.back;
					fgNormals[vertexIndex+3] = Vector3.back;

					//Connect UVTile coords to uv array
					if (tMgr.AreTexturesPacked()) {
						Vector2[] tileUVs = tMgr.GetTileUV(wCon.GetWorld(0), (int)x + chunkPos.x, (int)y + chunkPos.y);
						for (int i = 0; i < 4; i++) {
							Vector2 tileUV = tileUVs[i];
							fgUV[vertexIndex+i] = tileUV;
						}
					}
					
					else {
						Debug.LogError("Tile textures not packed! Can't build tile mesh!");
						return;
					}

					//On next tile, skip past current 4 vertices and current 6 triangle vertices (and increment tile index)
					vertexIndex += 4;
					triangleIndex += 6;
					tileIndex++;
				}
			}

			//Build the new foreground mesh with our filled arrays
			Mesh mainMesh = new Mesh();
			mainMesh.vertices = fgVertices;
			mainMesh.triangles = fgTriangles;
			mainMesh.normals = fgNormals;
			mainMesh.uv = fgUV;

			chunkMF.mesh = mainMesh;

			//Repeat process for background tiles
			Vector3[] bgVertices = new Vector3[vertexCount];
			int[] bgTriangles = new int[(vertexCount/2)*3];
			Vector3[] bgNormals = new Vector3[vertexCount];
			Vector2[] bgUV = new Vector2[vertexCount];

			vertexIndex = 0;
			triangleIndex = 0;
			tileIndex = 0;

			for (float x = 0; x < chunkSize; x++) {
				for (float y = 0; y < chunkSize; y++) {
					if (chunkBGTiles[(int)x, (int)y] == 0) {
						continue;
					}

					//Set up vertices in TopLeft->TopRight->BottomLeft->BottomRight order
					bgVertices[vertexIndex] = new Vector3(x, y+1, 0);
					bgVertices[vertexIndex + 1] = new Vector3(x+1, y+1, 0);
					bgVertices[vertexIndex + 2] = new Vector3(x, y, 0);
					bgVertices[vertexIndex + 3] = new Vector3(x+1, y, 0);

					//Set up triangles in 0->3->2 and 0->1->3 orders
					bgTriangles[triangleIndex+0] = vertexIndex+0;
					bgTriangles[triangleIndex+1] = vertexIndex+3;
					bgTriangles[triangleIndex+2] = vertexIndex+2;
					bgTriangles[triangleIndex+3] = vertexIndex+0;
					bgTriangles[triangleIndex+4] = vertexIndex+1;
					bgTriangles[triangleIndex+5] = vertexIndex+3;
					
					bgNormals[vertexIndex] = Vector3.back;
					bgNormals[vertexIndex+1] = Vector3.back;
					bgNormals[vertexIndex+2] = Vector3.back;
					bgNormals[vertexIndex+3] = Vector3.back;

					//Connect UVTile coords to uv array
					if (tMgr.AreTexturesPacked()) {
						Vector2[] tileUVs = tMgr.GetTileUV(wCon.GetWorld(1), (int)x + chunkPos.x, (int)y + chunkPos.y);
						for (int i = 0; i < 4; i++) {
							Vector2 tileUV = tileUVs[i];
							bgUV[vertexIndex+i] = tileUV;
						}
					}
					
					else {
						Debug.LogError("Tile textures not packed! Can't build tile mesh!");
						return;
					}

					//On next tile, skip past current 4 vertices and current 6 triangle vertices (and increment tile index)
					vertexIndex += 4;
					triangleIndex += 6;
					tileIndex++;
				}
			}

			Mesh bgMesh = new Mesh();
			bgMesh.vertices = bgVertices;
			bgMesh.triangles = bgTriangles;
			bgMesh.normals = bgNormals;
			bgMesh.uv = bgUV;

			bgMF.mesh = bgMesh;

			ShowChunk(chunk);
		}

		void HideChunk(int chunk) {
			cObjs.HideChunk(chunk);
		}

		void ShowChunk(int chunk) {
			cObjs.ShowChunk(chunk);
		}
	//
}
