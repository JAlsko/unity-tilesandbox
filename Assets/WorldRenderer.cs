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
[RequireComponent(typeof(RuleTileManager))]
public class WorldRenderer : MonoBehaviour {
	//UV coords for each tile type
	public List<UVTile> tileBases = new List<UVTile>();

	//Other neccessary world scripts
	private WorldController wCon;
	private WorldCollider wCol;
	private RuleTileManager rMgr;

	//World array (gets updated by world controller)
	int[,] world;

	//Object to hold all chunk objects
	public Transform chunkParent;
	//Plain chunk prefab
	public GameObject defaultChunk;

	//Array of existing chunk objects
	GameObject[] chunkObjs;

	void Start() {
		wCon = GetComponent<WorldController>();
		wCol = GetComponent<WorldCollider>();
		rMgr = GetComponent<RuleTileManager>();

		chunkObjs = new GameObject[0];
	}

	//Chunk Object Functions
	//-------------------------------------------------------------------------------
		//Called once at start to fill chunk objects array with default chunks
		void InitializeChunkObjects(int[,] world) {
			int chunkSize = WorldController.chunkSize;
			int worldWidth = world.GetUpperBound(0)+1;
			int worldHeight = world.GetUpperBound(1)+1;

			chunkObjs = new GameObject[worldWidth/chunkSize * worldHeight/chunkSize];

			//Initializing chunk object array
			for (int chunk = 0; chunk < chunkObjs.Length; chunk++) {
				GameObject newChunkObj = Instantiate(defaultChunk);
				Vector2Int newChunkPos = wCon.GetChunkPosition(chunk);
				newChunkObj.transform.parent = chunkParent;
				newChunkObj.name = "Chunk_" + chunk;
				newChunkObj.transform.position = new Vector3(newChunkPos.x, newChunkPos.y, 0);
				newChunkObj.transform.rotation = Quaternion.identity;
				newChunkObj.transform.localScale = Vector3.one;
				chunkObjs[chunk] = newChunkObj;
			}
		}

		public GameObject GetChunkObject(int chunk) {
			return chunkObjs[chunk];
		}
	//

	//Render Functions
	//-------------------------------------------------------------------------------
		//Public function to handle showing/hiding new chunks
		public void RenderChunks(int[] chunksToRender, int[] chunksToHide) {
			world = wCon.GetWorld();
			if (chunkObjs.Length <= 0) {
				InitializeChunkObjects(world);
			}

			foreach (int chunk in chunksToRender) {
				if (chunk >= chunkObjs.Length || chunk < 0) {
					continue;
				}
				ShowChunk(chunk);
			}

			foreach (int chunk in chunksToHide) {
				if (chunk >= chunkObjs.Length || chunk < 0) {
					continue;
				}
				HideChunk(chunk);
			}
		}

		//Function used for initial render of all existing chunks
		[ContextMenu("RenderAllChunks")]
		public void RenderAllChunks() {
			world = wCon.GetWorld();
			if (chunkObjs.Length <= 0) {
				InitializeChunkObjects(world);
			}

			for (int chunk = 0; chunk < chunkObjs.Length; chunk++) {
				RenderChunk(chunk);
				HideChunk(chunk);
			}
		}

		//Function for updated rendering of one chunk
		public void RenderChunk(int chunk) {
			//RuleTile rt = ruleTiles[world[x,y]]
			//Vector2Int[] uv_coords = rt.analyze(world, x, y)

			int[,] chunkTiles = wCon.GetChunkTiles(chunk);

			int chunkSize = WorldController.chunkSize;

			int vertexCount = chunkSize * chunkSize * 4;

			GameObject chunkObj = chunkObjs[chunk];

			if (!chunkObj.GetComponent<MeshFilter>()) {
				Debug.LogError("Chunk #" + chunk + "'s object has no mesh! Can't render!");
				return;
			}
			MeshFilter chunkMF = chunkObj.GetComponent<MeshFilter>();

			Vector3[] vertices = new Vector3[vertexCount];
			int[] triangles = new int[(vertexCount/2)*3];
			Vector3[] normals = new Vector3[vertexCount];
			Vector2[] uv = new Vector2[vertexCount];

			int vertexIndex = 0;
			int triangleIndex = 0;
			int tileIndex = 0;

			//Loop through each tile in the world
			for (float x = 0; x < chunkSize; x++) {
				for (float y = 0; y < chunkSize; y++) {
					if (chunkTiles[(int)x, (int)y] == 0) {
						continue;
					}

					//Set up vertices in TL->TR->BL->BR order
					vertices[vertexIndex] = new Vector3( x, y+1, 0);//(x/worldWidth)*meshScale.x , ((y+1)/worldHeight)*meshScale.y , 0 );
					vertices[vertexIndex + 1] = new Vector3( x+1, y+1, 0);//((x+1)/worldWidth)*meshScale.x, ((y+1)/worldHeight)*meshScale.y, 0 );
					vertices[vertexIndex + 2] = new Vector3( x, y, 0);//(x/worldWidth)*meshScale.x , (y/worldHeight)*meshScale.y , 0 );
					vertices[vertexIndex + 3] = new Vector3( x+1, y, 0);//((x+1)/worldWidth)*meshScale.x, (y/worldHeight)*meshScale.y, 0 );

					//Set up triangles in 0->3->2 and 0->1->3 orders
					triangles[triangleIndex+0] = vertexIndex+0;
					triangles[triangleIndex+1] = vertexIndex+3;
					triangles[triangleIndex+2] = vertexIndex+2;
					triangles[triangleIndex+3] = vertexIndex+0;
					triangles[triangleIndex+4] = vertexIndex+1;
					triangles[triangleIndex+5] = vertexIndex+3;
					
					//Normals don't work, dunno why
					normals[vertexIndex] = Vector3.back;
					normals[vertexIndex+1] = Vector3.back;
					normals[vertexIndex+2] = Vector3.back;
					normals[vertexIndex+3] = Vector3.back;

					//Connect UVTile coords to uv array
					if (rMgr.AreTexturesPacked()) {
						Vector2[] tileUVs = rMgr.GetTileUV(chunkTiles, (int)x, (int)y);
						for (int i = 0; i < 4; i++) {
							uv[vertexIndex+i] = tileUVs[i];
						}
					}
					
					else {
						UVTile currentTile = tileBases[chunkTiles[(int)x,(int)y]];
						for (int i = 0; i < 4; i++) {
							uv[vertexIndex+i] = currentTile.uv_coords[i];
						}
					}

					//On next tile, skip past current 4 vertices and current 6 triangle vertices (and increment tile index)
					vertexIndex += 4;
					triangleIndex += 6;
					tileIndex++;
				}
			}

			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.normals = normals;
			mesh.uv = uv;

			//ONLY UNCOMMENT FOR SMALL WORLDS - WILL CRASH UNITY OTHERWISE
			//Debug.Log("World width: " + worldWidth + " -- World height: " + worldHeight);
			//Debug.Log("Vertices: " + PrintV3Arr(vertices));
			//Debug.Log("Triangles: " + PrintIntArr(triangles));
			//Debug.Log("UVs: " + PrintV2Arr(uv));

			chunkMF.mesh = mesh;

			ShowChunk(chunk);

			Vector2Int chunkPos = wCon.GetChunkPosition(chunk);
			wCol.GenerateChunkColliders(chunkObj, world, chunkPos.x, chunkPos.y);
		}

		//Functions for hiding/showing chunks but not updating them
		public void HideChunk(int chunk) {
			chunkObjs[chunk].SetActive(false);
		}

		public void ShowChunk(int chunk) {
			chunkObjs[chunk].SetActive(true);
		}
	//

	//Helper Functions
	//-------------------------------------------------------------------------------
		string PrintV3Arr(Vector3[] arr) {
			string str = "";
			foreach (Vector3 vec in arr) {
				str += vec + " ";
			}
			return str;
		}

		string PrintV2Arr(Vector2[] arr) {
			string str = "";
			foreach (Vector2 vec in arr) {
				str += vec + " ";
			}
			return str;
		}

		string PrintIntArr(int[] arr) {
			string str = "";
			foreach (int n in arr) {
				str += n + " ";
			}
			return str;
		}

		string Print2DIntArr(int[,] arr) {
			string str = "";
			for (int x = 0; x < arr.GetUpperBound(0); x++) {
				for (int y = 0; y < arr.GetUpperBound(1); y++) {
					str += arr[x,y] + " ";
				}
				str += "\n";
			}
			return str;
		}
	//
}
