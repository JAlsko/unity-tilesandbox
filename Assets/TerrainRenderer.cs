using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Tilemaps;

public class TerrainRenderer : MonoBehaviour {

	public TerrainGenerator tgen;

	public Tilemap tmap;
	public List<TileBase> tbases = new List<TileBase>();

	public int chunkSize = 8;
	public Vector2Int chunkRenderAmounts = new Vector2Int();

	public Transform player;


	public int[,] currentTerrain;
	public int[,] showingTerrain;
	public List<int> showingChunks;

	public int prevChunk;
	public int curChunk;

	public int chunkRenderRadius = 1;
	int chunkRenderDiameter;

	void Start () {
		currentTerrain = tgen.currentTerrain;
		chunkRenderDiameter = (chunkRenderRadius*2)+1;
		showingChunks = new List<int>();
		showingTerrain = new int[currentTerrain.GetUpperBound(0),currentTerrain.GetUpperBound(1)];
		prevChunk = GetCurrentChunk();
	}
	
	void FixedUpdate () {
		GetCurrentChunk();
	}

	int GetCurrentChunk() {
		if (currentTerrain == null) {
			//Debug.Log("No terrain!");
			currentTerrain = tgen.currentTerrain;
			return 0;
		}
		prevChunk = curChunk;
		int currentChunk = 0;

		int terrainChunkWidth = GetTerrainChunkWidth(currentTerrain);
		int adjustedX = (int)(((int)player.position.x)/chunkSize);
		int adjustedY = (int)(((int)player.position.y)/chunkSize);
		currentChunk = (adjustedY * terrainChunkWidth) + adjustedX;

		curChunk = currentChunk;

		if (curChunk != prevChunk) {
			UpdateCulledTerrain();
		}
		return currentChunk;
	}

	int GetChunk(int x, int y, int terrainChunkWidth) {
		if (currentTerrain == null) {
			//Debug.Log("No terrain!");
			currentTerrain = tgen.currentTerrain;
			return 0;
		}
		int chunk = 0;

		int adjustedX = (int)(x/chunkSize);
		int adjustedY = (int)(y/chunkSize);
		chunk = (adjustedY * terrainChunkWidth) + adjustedX;

		return chunk;
	}

	int GetTerrainChunkWidth(int[,] terrain) {
		return (terrain.GetUpperBound(0)+1)/chunkSize;
	}

	int[] GetChunksToRender(int currentChunk) {
		int chunkDiameter = (chunkRenderRadius*2)+1;
		int numChunks = chunkDiameter*chunkDiameter;
		int terrainChunkWidth = GetTerrainChunkWidth(currentTerrain);
		int[] chunksToRender = new int[numChunks];

		int currentAddedChunk = 0;
		for (int i = -chunkDiameter/2; i <= chunkDiameter/2; i++) {
			for (int j = -chunkDiameter/2; j <= chunkDiameter/2; j++) {
				chunksToRender[currentAddedChunk] = currentChunk + terrainChunkWidth*i + j;
				currentAddedChunk++;
			}
		}

		return chunksToRender;
	}

	List<int> GetListFromArr(int[] arr) {
		List<int> newList = new List<int>();
		foreach (int i in arr) {
			newList.Add(i);
		}
		return newList;
	}

	int[] GetArrFromList(List<int> li) {
		int[] newArr = new int[li.Count];
		int index = 0;
		foreach (int i in li) {
			newArr[index] = i;
			index++;
		}
		return newArr;
	}

	string ArrToString(int[] arr) {
		string arrString = "";
		foreach (int i in arr) {
			arrString += (i + " ");
		}
		return arrString;
	}

	int[,] UpdateCulledTerrain() {
		if (currentTerrain == null) {
			return null;
		}
		int terrainChunkWidth = currentTerrain.GetLength(0)/chunkSize;


		int[] chunksShowing = GetArrFromList(showingChunks);
		int[] chunksToRender = GetChunksToRender(curChunk);
		int[] oldChunksToRender = new int[chunksToRender.Length];
		chunksToRender.CopyTo(oldChunksToRender, 0);

		int[] chunksToHide = (chunksShowing.Except(chunksToRender)).ToArray();
		chunksToRender = (chunksToRender.Except(chunksShowing)).ToArray();

		foreach (int i in chunksToRender) {
			if (showingChunks.Contains(i)) {
				continue;
			}
			ShowChunk(currentTerrain, showingTerrain, i);
		}
		foreach (int i in chunksToHide) {
			ClearChunk(showingTerrain, i);
		}
		
		/*for (int y = 0; y < newTerrain.GetLength(1); y += chunkSize) {
			for (int x = 0; x < newTerrain.GetLength(0); x += chunkSize) {
				if (GetChunk(x, y, terrainChunkWidth) == curChunk) {
					ShowChunk(currentTerrain, newTerrain, x, y);
				}
			}
		}*/

		tgen.RenderMap(showingTerrain);

		showingChunks = GetListFromArr(oldChunksToRender);

		return showingTerrain;
	}

	int[,] Copy2DArray(int[,] toCopy) {
		int[,] newArr = new int[toCopy.GetUpperBound(0), toCopy.GetUpperBound(1)];
		for (int x = 0; x < newArr.GetUpperBound(0); x++) {
			for (int y = 0; y < newArr.GetUpperBound(1); y++) {
				newArr[x, y] = toCopy[x, y];
			}
		}

		return newArr;
	}

	[ContextMenu("Decorate chunks")]
	public void DecorateCurrentChunks() {
		DecorateChunks(currentTerrain);
	}

	void DecorateChunks(int[,] terrain) {
		for (int y = 0; y < terrain.GetUpperBound(1); y+=chunkSize) {
			for (int x = 0; x < terrain.GetUpperBound(0); x+=chunkSize) {
				
				for (int j = y; j < y+chunkSize; j++) {
					for (int i = x; i < x+chunkSize; i++) {
						terrain[i,j] = 1 + (GetChunk(i,j,terrain.GetUpperBound(0)/chunkSize)%2);
					}
				}

			}
		}

		tgen.RenderMap(currentTerrain);
	}

	void ClearChunk(int[,] newTerrain, int chunk) {
		int chunkX = (chunk % chunkSize) * chunkSize;
		int chunkY = (chunk / chunkSize) * chunkSize;

		//Debug.Log("Showing chunk " + chunk + " starting at (" + chunkX + ", " + chunkY + ")");

		for (int x = chunkX; x < chunkX + chunkSize; x++) {
			for (int y = chunkY; y < chunkY + chunkSize; y++) {
				if (y > newTerrain.GetUpperBound(1) || x > newTerrain.GetUpperBound(0)) {
					continue;
				}

				if (y < newTerrain.GetLowerBound(1) || x < newTerrain.GetLowerBound(0)) {
					continue;
				}
				newTerrain[x, y] = 0;
			}
		}
	}

	void ShowChunk(int[,] oldTerrain, int[,] newTerrain, int chunk) {
		int chunkX = (chunk % chunkSize) * chunkSize;
		int chunkY = (chunk / chunkSize) * chunkSize;

		//Debug.Log("Showing chunk " + chunk + " starting at (" + chunkX + ", " + chunkY + ")");

		for (int x = chunkX; x < chunkX + chunkSize; x++) {
			for (int y = chunkY; y < chunkY + chunkSize; y++) {
				if (y > newTerrain.GetUpperBound(1) || x > newTerrain.GetUpperBound(0)) {
					continue;
				}

				if (y < newTerrain.GetLowerBound(1) || x < newTerrain.GetLowerBound(0)) {
					continue;
				}
				newTerrain[x, y] = oldTerrain[x, y];
			}
		}
	}
}
