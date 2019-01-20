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


public class WorldRenderer : MonoBehaviour {
	public List<UVTile> tileBases = new List<UVTile>();

	public WorldGenerator wGen;

	public MeshRenderer mr;
	public MeshFilter mf;
	public MeshCollider mc;

	int[,] world;

	Vector3 meshScale;

	void Start() {
		meshScale = new Vector3(transform.localScale.x, transform.localScale.y,0);
	}

	[ContextMenu("Render World")]
	void RenderWorld() {
		world = wGen.GetNewPerlinWorld(wGen.width, wGen.height);
		Debug.Log("Generating " + wGen.width + "x" + wGen.height + " size world");
		int worldWidth = world.GetUpperBound(0) + 1;
		int worldHeight = world.GetUpperBound(1) + 1;

		int vertexCount = worldWidth * worldHeight * 4;

		Vector3[] vertices = new Vector3[vertexCount];
		int[] triangles = new int[(vertexCount/2)*3];
		Vector3[] normals = new Vector3[vertexCount];
		Vector2[] uv = new Vector2[vertexCount];

		int vertexIndex = 0;
		int triangleIndex = 0;
		int tileIndex = 0;

		//Loop through each tile in the world
		for (float x = 0; x < worldWidth; x++) {
			for (float y = 0; y < worldHeight; y++) {
				if (world[(int)x, (int)y] == 0) {
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
				UVTile currentTile = tileBases[world[(int)x,(int)y]];
				for (int i = 0; i < 4; i++) {
					uv[vertexIndex+i] = currentTile.uv_coords[i];
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

		mf.mesh = mesh;

		//MeshSimplifier.Weld(mc.sharedMesh, 0, 16);
		//MeshSimplifier.Simplify(mc.sharedMesh);
	}

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
		foreach (int vec in arr) {
			str += vec + " ";
		}
		return str;
	}

}
