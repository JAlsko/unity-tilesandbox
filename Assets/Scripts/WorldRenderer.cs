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
public class WorldRenderer : MonoBehaviour {
	//UV coords for each tile type
	public List<UVTile> tileBases = new List<UVTile>();

	//Other neccessary world scripts
	private WorldController wCon;
	private WorldCollider wCol;
	private TileManager tMgr;

	//World array (gets updated by world controller)
	int[,] world_fg;
	int[,] world_bg;

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

		chunkObjs = new GameObject[0];
		chunkLightmaps = new MeshRenderer[0];
	}

	//Chunk Object Functions
	//-------------------------------------------------------------------------------
		//Called once at start to fill chunk objects array with default chunks
		public void InitializeChunkObjects(int[,] world) {
			int chunkSize = WorldController.chunkSize;
			int worldWidth = world.GetUpperBound(0)+1;
			int worldHeight = world.GetUpperBound(1)+1;

			chunkObjs = new GameObject[WorldController.GetWorldChunkWidth() * WorldController.GetWorldChunkHeight()];
			chunkBGs = new GameObject[WorldController.GetWorldChunkWidth() * WorldController.GetWorldChunkHeight()];
			chunkLightmaps = new MeshRenderer[WorldController.GetWorldChunkWidth() * WorldController.GetWorldChunkHeight()];

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
				
				GameObject chunkBG = newChunkObj.transform.Find("ChunkBG").gameObject;
				BoxCollider2D bgCol = chunkBG.GetComponent<BoxCollider2D>();
				bgCol.offset = new Vector2(chunkSize/2, chunkSize/2);
				bgCol.size = new Vector2(chunkSize, chunkSize);
				chunkBGs[chunk] = chunkBG;

				Transform chunkLightmapObj = newChunkObj.transform.Find("Lightmap");
				chunkLightmapObj.localPosition = new Vector3(chunkSize/2, chunkSize/2, -1);
				chunkLightmapObj.localScale = new Vector3(chunkSize, chunkSize, 1);
				chunkLightmaps[chunk] = chunkLightmapObj.GetComponent<MeshRenderer>();
			}
		}

		public GameObject GetChunkObject(int chunk) {
			return chunkObjs[chunk];
		}

		public List<Transform> GetAllChunkParents() {
			List<Transform> allChunkParents = new List<Transform>();
			foreach (GameObject chunkObj in chunkObjs) {
				allChunkParents.Add(chunkObj.transform);
			}

			return allChunkParents;
		}
	//

	//Render Functions
	//-------------------------------------------------------------------------------
		//Public function to handle showing/hiding new chunks
		public void RenderChunks(int[] chunksToRender, int[] chunksToHide) {
			world_fg = wCon.GetWorld(0);
			world_bg = wCon.GetWorld(1);
			if (chunkObjs.Length <= 0) {
				InitializeChunkObjects(world_fg);
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
			world_fg = wCon.GetWorld(0);
			world_bg = wCon.GetWorld(1);
			if (chunkObjs.Length <= 0) {
				InitializeChunkObjects(world_fg);
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

			int[,] chunkTiles = wCon.GetChunkTiles(chunk, 0);
			int[,] chunkBGTiles = wCon.GetChunkTiles(chunk, 1);

			int chunkSize = WorldController.chunkSize;

			int vertexCount = chunkSize * chunkSize * 4;

			Vector2Int chunkPos = wCon.GetChunkPosition(chunk);

			GameObject chunkObj = chunkObjs[chunk];
			GameObject chunkBGObj = chunkBGs[chunk];

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

					//Set up vertices in TL->TR->BL->BR order
					fgVertices[vertexIndex] = new Vector3( x, y+1, 0);//(x/worldWidth)*meshScale.x , ((y+1)/worldHeight)*meshScale.y , 0 );
					fgVertices[vertexIndex + 1] = new Vector3( x+1, y+1, 0);//((x+1)/worldWidth)*meshScale.x, ((y+1)/worldHeight)*meshScale.y, 0 );
					fgVertices[vertexIndex + 2] = new Vector3( x, y, 0);//(x/worldWidth)*meshScale.x , (y/worldHeight)*meshScale.y , 0 );
					fgVertices[vertexIndex + 3] = new Vector3( x+1, y, 0);//((x+1)/worldWidth)*meshScale.x, (y/worldHeight)*meshScale.y, 0 );

					//Set up triangles in 0->3->2 and 0->1->3 orders
					fgTriangles[triangleIndex+0] = vertexIndex+0;
					fgTriangles[triangleIndex+1] = vertexIndex+3;
					fgTriangles[triangleIndex+2] = vertexIndex+2;
					fgTriangles[triangleIndex+3] = vertexIndex+0;
					fgTriangles[triangleIndex+4] = vertexIndex+1;
					fgTriangles[triangleIndex+5] = vertexIndex+3;
					
					//Normals don't work, dunno why
					fgNormals[vertexIndex] = Vector3.back;
					fgNormals[vertexIndex+1] = Vector3.back;
					fgNormals[vertexIndex+2] = Vector3.back;
					fgNormals[vertexIndex+3] = Vector3.back;

					//Connect UVTile coords to uv array
					if (tMgr.AreTexturesPacked()) {
						Vector2[] tileUVs = tMgr.GetTileUV(world_fg, (int)x + chunkPos.x, (int)y + chunkPos.y);
						for (int i = 0; i < 4; i++) {
							Vector2 tileUV = tileUVs[i];
							fgUV[vertexIndex+i] = tileUV;
						}
					}
					
					else {
						UVTile currentTile = tileBases[chunkTiles[(int)x,(int)y]];
						for (int i = 0; i < 4; i++) {
							fgUV[vertexIndex+i] = currentTile.uv_coords[i];
						}
					}

					//On next tile, skip past current 4 vertices and current 6 triangle vertices (and increment tile index)
					vertexIndex += 4;
					triangleIndex += 6;
					tileIndex++;
				}
			}

			Mesh mainMesh = new Mesh();
			mainMesh.vertices = fgVertices;
			mainMesh.triangles = fgTriangles;
			mainMesh.normals = fgNormals;
			mainMesh.uv = fgUV;

			chunkMF.mesh = mainMesh;

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

					//Set up vertices in TL->TR->BL->BR order
					bgVertices[vertexIndex] = new Vector3( x, y+1, 0);//(x/worldWidth)*meshScale.x , ((y+1)/worldHeight)*meshScale.y , 0 );
					bgVertices[vertexIndex + 1] = new Vector3( x+1, y+1, 0);//((x+1)/worldWidth)*meshScale.x, ((y+1)/worldHeight)*meshScale.y, 0 );
					bgVertices[vertexIndex + 2] = new Vector3( x, y, 0);//(x/worldWidth)*meshScale.x , (y/worldHeight)*meshScale.y , 0 );
					bgVertices[vertexIndex + 3] = new Vector3( x+1, y, 0);//((x+1)/worldWidth)*meshScale.x, (y/worldHeight)*meshScale.y, 0 );

					//Set up triangles in 0->3->2 and 0->1->3 orders
					bgTriangles[triangleIndex+0] = vertexIndex+0;
					bgTriangles[triangleIndex+1] = vertexIndex+3;
					bgTriangles[triangleIndex+2] = vertexIndex+2;
					bgTriangles[triangleIndex+3] = vertexIndex+0;
					bgTriangles[triangleIndex+4] = vertexIndex+1;
					bgTriangles[triangleIndex+5] = vertexIndex+3;
					
					//Normals don't work, dunno why
					bgNormals[vertexIndex] = Vector3.back;
					bgNormals[vertexIndex+1] = Vector3.back;
					bgNormals[vertexIndex+2] = Vector3.back;
					bgNormals[vertexIndex+3] = Vector3.back;

					//Connect UVTile coords to uv array
					if (tMgr.AreTexturesPacked()) {
						Vector2[] tileUVs = tMgr.GetTileUV(world_bg, (int)x + chunkPos.x, (int)y + chunkPos.y);
						for (int i = 0; i < 4; i++) {
							Vector2 tileUV = tileUVs[i];
							bgUV[vertexIndex+i] = tileUV;
						}
					}
					
					else {
						UVTile currentTile = tileBases[chunkTiles[(int)x,(int)y]];
						for (int i = 0; i < 4; i++) {
							bgUV[vertexIndex+i] = currentTile.uv_coords[i];
						}
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

			wCol.GenerateChunkColliders(chunkObj, world_fg, chunkPos.x, chunkPos.y);
		}

		//Functions for hiding/showing chunks but not updating them
		public void HideChunk(int chunk) {
			chunkObjs[chunk].SetActive(false);
		}

		public void ShowChunk(int chunk) {
			chunkObjs[chunk].SetActive(true);
		}

		public void RenderChunkLightmap(int chunk, Material lightmap_mat) {
			chunkLightmaps[chunk].material = lightmap_mat;
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
