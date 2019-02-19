using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System;


[RequireComponent(typeof(WorldGenerator))]
[RequireComponent(typeof(WorldRenderer))]
[RequireComponent(typeof(WorldCollider))]
[RequireComponent(typeof(ColliderManager))]
[RequireComponent(typeof(TileManager))]
[RequireComponent(typeof(LightController))]
[RequireComponent(typeof(ItemManager))]
public class WorldController : Singleton<WorldController> {

    private static WorldController instance;

	static string WORLD_SAVE_NAME = "worldSave.dat";
	static int NULL_TILE = 0;

	public int[,] world_fg;
	public int[,] world_bg;

	//Other world scripts
	WorldGenerator wGen;
	WorldRenderer wRend;
	WorldCollider wCol;
	TileManager tMgr;
	LightController lCon;
	ItemManager iMgr;

	public Transform player;

	//Dimensions for world generation
	public int worldWidth = 0;
	public int worldHeight = 0;

	//Width/height of each chunk
	public const int chunkSize = 64;

	//Total number of chunks in world
	public int totalChunks;

	//Lists to track which chunks are currently showing/hidden
	public List<int> hiddenChunks;
	public List<int> showingChunks;

	//Temporary array to populate for functions that require array of tiles in a chunk
	private int[,] chunkTiles;

	//Tracks player chunk movement
	[SerializeField] private int prevChunk;
	[SerializeField] private int curChunk;

	//Number of chunks in each direction from player to render
	public int chunkRenderRadius = 1;
	int chunkRenderDiameter;

	static int worldChunkWidth = 1;
	static int worldChunkHeight = 1;

	//Switch once world initialized
	bool worldInitialized = false;

	void Start () {
		wGen = GetComponent<WorldGenerator>();
		wRend = GetComponent<WorldRenderer>();
		wCol = GetComponent<WorldCollider>();
		tMgr = GetComponent<TileManager>();
		lCon = GetComponent<LightController>();
		iMgr = GetComponent<ItemManager>();

		if (instance == null) {
			instance = this;
		} else if (instance != this) {
			Destroy(this.gameObject);
		}

		//Checks to make sure world generation size matches with chunk size
		if (worldWidth % chunkSize != 0 || worldHeight % chunkSize != 0) {
			Debug.LogError("World cannot be divided evenly into chunks!! Make sure world width and height are evenly divisible by chunk size!");
			worldWidth -= worldWidth%chunkSize;
			worldHeight -= worldHeight%chunkSize;
		} else if (worldWidth < chunkSize || worldHeight < chunkSize) {
			Debug.LogError("World too small for chunk size!");
			worldWidth = chunkSize;
			worldHeight = chunkSize;
		}

		totalChunks = (worldWidth/chunkSize) * (worldHeight/chunkSize);

		//Load world on start
		//LoadWorld();

		//Initialize chunkTiles temp array
		chunkTiles = new int[chunkSize, chunkSize];

		//Variable to avoid doing radius->diameter calculation repeatedly
		chunkRenderDiameter = (chunkRenderRadius*2)+1;

		//Generate Colliders -> Evaluate Rule Tiles -> Render Tiles
		StartupWorld();
	}
	
	void FixedUpdate () {
		if (!worldInitialized) {
			return;
		}

		GetCurrentChunk();
	}

    public static WorldController Instance {
        get {
            return instance;
        }
    }

	//World Initialization Functions
	//-------------------------------------------------------------------------------
		void StartupWorld() {
			CheckIfWorldExists();
			InitializeWorldBounds();
			PlacePlayer();
			PackTextures();
			GenerateColliders();
			RenderWorld();
			GenerateLightMap();
			InitializeItems();
			worldInitialized = true;
		}

		[ContextMenu("CheckIfWorldExists")]
		public void CheckIfWorldExists() {
			if (world_fg == null) {
				Debug.Log("Null world! Generating new one!");
				world_fg = wGen.GetNewFractalWorld(worldWidth, worldHeight);
				world_bg = WorldGenerator.Get2DArrayCopy(world_fg);
			}
		}

		void InitializeWorldBounds() {
			worldChunkWidth = (world_fg.GetUpperBound(0)+1)/chunkSize;
			worldChunkHeight = (world_fg.GetUpperBound(1)+1)/chunkSize;

			//Once world bounds are set, we need to initialize the chunk objects (before collider generation)
			wRend.InitializeChunkObjects(world_fg);
		}

		void PlacePlayer() {
			player.position = (Vector3.up * (worldHeight + chunkSize/2));
		}

		void PackTextures() {
			tMgr.PackRuleTileTextures();
		}

		void GenerateColliders() {
			for (int chunk = 0; chunk < totalChunks; chunk++) {
				Vector2Int chunkPos = GetChunkPosition(chunk);
				GameObject chunkObj = wRend.GetChunkObject(chunk);
				wCol.GenerateChunkColliders(chunkObj, world_fg, chunkPos.x, chunkPos.y);
			}
		}

		void RenderWorld() {
			wRend.RenderAllChunks();
			UpdateCulledChunks();
		}

		void GenerateLightMap() {
			lCon.InitializeWorld(world_fg, world_bg);
		}

		void InitializeItems() {
			iMgr.InitializeItemManager();
		}
	//

    //World Save/Load Functions
    //-------------------------------------------------------------------------------

		[ContextMenu("Save World")]
		public void SaveWorld() {
			Debug.Log("Saving World...");
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file1 = File.Open (Application.persistentDataPath + "/" + WORLD_SAVE_NAME, FileMode.OpenOrCreate);

			bf.Serialize (file1, world_fg);
			file1.Close ();
		}

		[ContextMenu("Load World")]
		public void LoadWorld() {
			if (File.Exists (Application.persistentDataPath + "/" + WORLD_SAVE_NAME)) {
				BinaryFormatter bf = new BinaryFormatter ();
				FileStream file1 = File.Open (Application.persistentDataPath + "/" + WORLD_SAVE_NAME, FileMode.Open);
				int[,] loadedWorld = (int[,])bf.Deserialize (file1);
				file1.Close ();

				Debug.Log("Loading stats...");
				LoadSavedWorld(loadedWorld);
			} else {
				Debug.Log("No stats file found.");
			}
		}

		void LoadSavedWorld(int[,] loadedWorld) {
			world_fg = loadedWorld;
			RenderWorld();
		}

		[ContextMenu("Delete World")]
		void DeleteWorld() {
			if (File.Exists(Application.persistentDataPath + "/" + WORLD_SAVE_NAME)) {
				File.Delete(Application.persistentDataPath + "/" + WORLD_SAVE_NAME);
			}
			RenderWorld();
		}

		public int[,] GetWorld(int worldLayer = 0) {
			CheckIfWorldExists();
			return worldLayer == 0 ? world_fg : world_bg;
		}
	//

	//Chunk Handling
	//-------------------------------------------------------------------------------
		//Function to get number of chunks in an entire world row
		public static int GetWorldChunkWidth() {
			return worldChunkWidth;
		}

		//Function to get number of chunks in an entire world column
		public static int GetWorldChunkHeight() {
			return worldChunkHeight;
		}

		//Public function to get total number of chunks
		public static int GetChunkCount() {
			return worldChunkWidth * worldChunkHeight;
		}

		//Update function to keep track of player's current chunk
		int GetCurrentChunk() {
			prevChunk = curChunk;
			int currentChunk = 0;

			int adjustedX = (int)(((int)player.position.x)/chunkSize);
			int adjustedY = (int)(((int)player.position.y)/chunkSize);
			currentChunk = (adjustedY * worldChunkWidth) + adjustedX;

			curChunk = currentChunk;

			if (curChunk != prevChunk) {
				UpdateCulledChunks();
			}
			return currentChunk;
		}

		//Public function to get appropriate chunk from x-y position
		public static int GetChunk(int x, int y) {
			int chunk = 0;

			int adjustedX = (int)(x/chunkSize);
			int adjustedY = (int)(y/chunkSize);
			chunk = (adjustedY * worldChunkWidth) + adjustedX;

			return chunk;
		}

		public Transform GetChunkParent(int chunk) {
			return wRend.GetChunkObject(chunk).transform;
		}

		public List<Transform> GetAllChunkParents() {
			return wRend.GetAllChunkParents();
		}

		//Public function to get world tiles from one chunk
		public int[,] GetChunkTiles(int chunk, int worldLayer = 0) {
			CheckIfWorldExists();
			Vector2Int chunkPos = GetChunkPosition(chunk);

			chunkTiles = new int[chunkSize, chunkSize];

			int adjustedX = chunkPos.x;
			int adjustedY = chunkPos.y;
			for (int y = 0; y < chunkSize; y++) {
				for (int x = 0; x < chunkSize; x++) {
					int newTile = worldLayer == 0 ? world_fg[adjustedX, adjustedY] : world_bg[adjustedX, adjustedY];
					chunkTiles[x, y] = newTile;

					adjustedX++;
				}
				adjustedY++;
				adjustedX = chunkPos.x;
			}

			return chunkTiles;
		}

		//Public function to get bottom-left coordinates of one chunk
		public Vector2Int GetChunkPosition(int chunk) {
			int adjustedX = chunk % worldChunkWidth;
			int adjustedY = chunk / worldChunkWidth;
			return new Vector2Int(adjustedX * chunkSize, adjustedY * chunkSize);
		}

		//Used when player enters a new chunk; returns only the new chunks that have to be shown
		int[] GetChunksToRender(int currentChunk) {
			CheckIfWorldExists();

			int numChunks = chunkRenderDiameter*chunkRenderDiameter;
			int[] chunksToShow = new int[numChunks];

			int currentAddedChunk = 0;
			for (int i = -chunkRenderDiameter/2; i <= chunkRenderDiameter/2; i++) {
				for (int j = -chunkRenderDiameter/2; j <= chunkRenderDiameter/2; j++) {

					//If in a chunk on the right edge of the world, don't show chunks to the right
					if ((currentChunk+1) % worldChunkWidth == 0) {
						if (j > 0) {
							chunksToShow[currentAddedChunk] = -1;
							currentAddedChunk++;
							continue;
						}
					} 
					
					//If in a chunk on the left edge of the world, don't show chunks to the left
					else if (currentChunk % worldChunkWidth == 0) {
						if (j < 0) {
							chunksToShow[currentAddedChunk] = -1;
							currentAddedChunk++;
							continue;
						}
					}
					chunksToShow[currentAddedChunk] = currentChunk + worldChunkWidth*i + j;
					currentAddedChunk++;
				}
			}

			return chunksToShow;
		}

		//If entering a new chunk, determine which chunks must be shown and which must be hidden
		void UpdateCulledChunks() {
			CheckIfWorldExists();

			int[] chunksShowing = GetArrFromList(showingChunks);
			int[] chunksToShow = GetChunksToRender(curChunk);
			int[] oldChunksToShow = new int[chunksToShow.Length];
			chunksToShow.CopyTo(oldChunksToShow, 0);

			int[] chunksToHide = (chunksShowing.Except(chunksToShow)).ToArray();
			chunksToShow = (chunksToShow.Except(chunksShowing)).ToArray();

			//Debug.Log("Chunks to hide: " + ArrToString(chunksToHide));
			//Debug.Log("Chunks to show: " + ArrToString(chunksToShow));

			wRend.RenderChunks(chunksToShow, chunksToHide);

			showingChunks = GetListFromArr(oldChunksToShow);
		}

	//

	//Tile Manipulator Functions
	//-------------------------------------------------------------------------------
		//Base tile manipulator
		private void ModifyTile(int x, int y, int newTile) {
			world_fg[x, y] = newTile;

			int chunkToModify = GetChunk(x, y);

			//Re-render tile's chunk
			wRend.RenderChunk(chunkToModify);

			//Re-render tile's lightmap
			lCon.HandleNewBlock(x, y, newTile, world_bg[x, y]);

			//Update tile's chunk's collider (doesn't update relevant colliders in other chunks)
			//wCol.GenerateChunkColliders(chunkToModify, world);
			
			//Update colliders of tiles surrounding modified tile (updates other chunks' colliders if necessary)
			wCol.GenerateTileColliders(world_fg, x, y);
		}

		//Public tile remover function
		public int RemoveTile(int x, int y) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				return -1;
			}
			if (world_fg[x,y] == 0) {
				return -1;
			}

			int blockID = world_fg[x, y];
			ModifyTile(x, y, 0);
			return blockID;
		}

		//Public tile addition function
		public int AddTile(int x, int y, int newTile) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				return -1;
			}
			if (world_fg[x,y] != 0) {
				return -1;
			}

			ModifyTile(x, y, newTile);
			return 1;
		}

		//Public tile/nontile check-er method
		public bool isTile(int x, int y, int worldLayer = 0) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				Debug.Log("Out of map tile " + x + ", " + y);
				return false;
			}
			bool tileExists = (worldLayer == 0 ? world_fg[x, y] != 0 : world_bg[x, y] != 0);
			return tileExists;
		}

		public bool isSky(int x, int y) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				return false;
			}
			bool skyTile = world_fg[x, y] == 0 && world_bg[x, y] == 0;
			return skyTile;
		}

	//
	
	//Helper Methods
	//-------------------------------------------------------------------------------
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
	//
}
