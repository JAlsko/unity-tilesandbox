﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System;


[RequireComponent(typeof(WorldGenerator))]
[RequireComponent(typeof(TileRenderer))]
[RequireComponent(typeof(WorldCollider))]
[RequireComponent(typeof(TileController))]
[RequireComponent(typeof(LightController))]
[RequireComponent(typeof(ItemManager))]
[RequireComponent(typeof(TerrainGenerator))]
[RequireComponent(typeof(ChunkObjectsHolder))]
[RequireComponent(typeof(LiquidController))]
[RequireComponent(typeof(UIController))]
[RequireComponent(typeof(CursorController))]
[RequireComponent(typeof(NavGrid))]
[RequireComponent(typeof(EntityManager))]
[RequireComponent(typeof(InventoryManager))]
public class WorldController : Singleton<WorldController> {

	static string WORLD_SAVE_NAME = "worldSave.dat";
	static string WORLDBG_SAVE_NAME = "worldBGSave.dat";

	//Primary tile arrays
	public string[,] world_fg;
	public string[,] world_bg;

	//Other world scripts
	TerrainGenerator tGen;
	TileRenderer wRend;
	WorldCollider wCol;
	TileController tMgr;
	LightController ltCon;
	ItemManager iMgr;
	ChunkObjectsHolder cObjs;
	LiquidController lqCon;
	TileController wMod;
	UIController uic;
	CursorController cCon;
	NavGrid navGrid;
	EntityManager eMgr;
	InventoryManager invMgr;

	public Transform player;
	public PlayerInventory pInv;

	//Dimensions for world generation (statics set on Start())
	[SerializeField] private int worldWidth = 0;
	static int s_worldWidth = 0;
	[SerializeField] private int worldHeight = 0;
	static int s_worldHeight = 0;

	//Width/height of each chunk
	public const int chunkSize = 64;

	//Total number of chunks in world
	private int totalChunks;

	//Lists to track which chunks are currently showing/hidden
	private List<int> hiddenChunks;
	public List<int> showingChunks;

	//Temporary array to populate for functions that require array of tiles in a chunk
	private string[,] chunkTiles;

	//Tracks player chunk movement
	private int prevChunk;
	private int curChunk;

	//Number of chunks in each direction from player to render
	public int chunkRenderRadius = 1;
	private int chunkRenderDiameter;

	static int worldChunkWidth = 1;
	static int worldChunkHeight = 1;

	//World initialization flag
	bool worldInitialized = false;

	bool started = false;

	void Start () {
		tGen = GetComponent<TerrainGenerator>();
		wRend = GetComponent<TileRenderer>();
		wCol = GetComponent<WorldCollider>();
		tMgr = GetComponent<TileController>();
		ltCon = GetComponent<LightController>();
		iMgr = GetComponent<ItemManager>();
		cObjs = GetComponent<ChunkObjectsHolder>();
		lqCon = GetComponent<LiquidController>();
		wMod = GetComponent<TileController>();
		uic = GetComponent<UIController>();
		cCon = GetComponent<CursorController>();
		navGrid = GetComponent<NavGrid>();
		eMgr = GetComponent<EntityManager>();
		invMgr = GetComponent<InventoryManager>();

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

		s_worldWidth = worldWidth;
		s_worldHeight = worldHeight;

		totalChunks = (worldWidth/chunkSize) * (worldHeight/chunkSize);

		chunkTiles = new string[chunkSize, chunkSize];

		chunkRenderDiameter = (chunkRenderRadius*2)+1;

		StartupWorld();
	}

	void Update() {
		if (!started && Input.GetKeyDown(KeyCode.Space)) {
			//DoStart();
			started = true;
		}
	}
	
	void FixedUpdate () {
		if (!worldInitialized) {
			return;
		}

		GetCurrentChunk();
	}

	//World Initialization Functions
	//-------------------------------------------------------------------------------
		/// <summary>
        /// Initialization function for all world scripts.
		/// Standardizes script initialization order instead of relying on 'Start()'.
        /// </summary>
        /// <returns></returns>
		void StartupWorld(string[,] newWorld = null, string[,] newWorldBG = null) {
			//world_fg = new int[s_worldWidth, s_worldHeight];
			//world_bg = new int[s_worldWidth, s_worldHeight];
			
			InitializeTileStructure();
			InitializeTiles();
			/*if (newWorld == null) {
				NewWorld();
			} else {
				world_fg = newWorld;
				world_bg = newWorldBG;
			}*/
			LoadWorld();
			SaveWorld();
			InitializeChunks();
			PlacePlayer();
			InitializeLiquids();
			InitializeItems();
			InitializeMultiTiles();
			InitializeInventories();
			InitializeUI();
			InitializeEntities();
			InitializeCursor();
			RenderWorld();
			GenerateLightMap();
			InitializeNavGrid();
			worldInitialized = true;
		}

		/// <summary>
        /// Gets a new world from TerrainGenerator.
        /// </summary>
        /// <returns></returns>
		public void NewWorld() {
			//if (world_fg == null) {
				Debug.Log("Generating new world!");
				
				world_fg = new string[s_worldWidth, s_worldHeight];
				world_bg = new string[s_worldWidth, s_worldHeight];
				
				world_fg = tGen.GenerateNewWorld();
				world_bg = TerrainGenerator.Get2DArrayCopy(world_fg);
				world_fg = tGen.DigCaves(world_fg);
			//}
		}

		/// <summary>
        /// Initializes chunk GameObjects in ChunkObjectHolder
        /// </summary>
        /// <returns></returns>
		void InitializeChunks() {
			worldChunkWidth = (world_fg.GetUpperBound(0)+1)/chunkSize;
			worldChunkHeight = (world_fg.GetUpperBound(1)+1)/chunkSize;

			//Once world bounds are set, we need to initialize the chunk objects (before collider generation)
			cObjs.InitializeChunkObjects();
		}

		//Placeholder player spawn
		void PlacePlayer() {
			player.position = (Vector3.up * (worldHeight));
		}

		/// <summary>
        /// Packs TileManager's textures (before rendering tiles).
        /// </summary>
        /// <returns></returns>
		void InitializeTiles() {
			tMgr.InitializeTiles();
		}

		/// <summary>
        /// Generates all initial colliders for new world.
        /// </summary>
        /// <returns></returns>
		void GenerateColliders() {
			for (int chunk = 0; chunk < totalChunks; chunk++) {
				wCol.GenerateChunkColliders(chunk);
			}
		}

		/// <summary>
		/// Initializes tile health for existing tiles
		/// </summary>
		/// <returns></returns>
		void InitializeTileStructure() {
			wMod.InitializeTileStructures();
		}

		/// <summary>
        /// Handles initial render of all chunk tiles.
        /// </summary>
        /// <returns></returns>
		void RenderWorld() {
			wRend.RenderAllChunks();
			UpdateCulledChunks();
		}

		/// <summary>
        /// Initializes lightmaps for all chunks.
        /// </summary>
        /// <returns></returns>
		void GenerateLightMap() {
			ltCon.InitializeWorld();
		}

		/// <summary>
        /// Initializes liquid map.
        /// </summary>
        /// <returns></returns>
		void InitializeLiquids() {
			lqCon.InitializeLiquids();
		}

		/// <summary>
        /// Initializes ItemManager's item collection.
        /// </summary>
        /// <returns></returns>
		void InitializeItems() {
			iMgr.InitializeItemManager();
			iMgr.InitializeAllItems();
		}

		/// <summary>
        /// Initializes MultiTileManagers's MultiTiles collection.
        /// </summary>
        /// <returns></returns>
		void InitializeMultiTiles() {
			//TileController.Instance.InitializeAllMultiTiles();
		}

		/// <summary>
        /// Initializes ItemManager's item collection.
        /// </summary>
        /// <returns></returns>
		void InitializeUI() {
			//uic.InitializeInventoryItemUI();
			uic.InitializeRecipeUI();
			uic.InitializeExternalInventoryUI();
			uic.InitializePlayerInventoryUI();
		}

		void InitializeCursor() {
			cCon.InitializeCursorControls();
		}

		void InitializeInventories() {
			invMgr.InitializeInventories();
			pInv.InitializePlayerInventory();
		}

		void InitializeNavGrid() {
			navGrid.InitializeNavGrid();
		}

		void InitializeEntities() {
			eMgr.InitializeEntityAttributes();
			eMgr.InitializeExistingEntities();
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

			FileStream file2 = File.Open (Application.persistentDataPath + "/" + WORLDBG_SAVE_NAME, FileMode.OpenOrCreate);

			bf.Serialize (file2, world_bg);
			file2.Close ();
		}

		[ContextMenu("Load World")]
		public void LoadWorld() {
			Debug.Log(Application.persistentDataPath);
			if (File.Exists (Application.persistentDataPath + "/" + WORLD_SAVE_NAME)) {
				BinaryFormatter bf = new BinaryFormatter ();
				FileStream file1 = File.Open (Application.persistentDataPath + "/" + WORLD_SAVE_NAME, FileMode.Open);
				string[,] loadedWorld = (string[,])bf.Deserialize (file1);
				file1.Close ();

				FileStream file2 = File.Open (Application.persistentDataPath + "/" + WORLDBG_SAVE_NAME, FileMode.Open);
				string[,] loadedWorldBG = (string[,])bf.Deserialize (file2);
				file2.Close ();

				Debug.Log("Loading stats...");
				LoadSavedWorld(loadedWorld, loadedWorldBG);
			} else {
				Debug.Log("No stats file found.");
				NewWorld();
			}
		}

		void LoadSavedWorld(string[,] loadedWorld, string[,] loadedWorldBG) {
			for (int x = 0; x <= loadedWorld.GetUpperBound(0); x++) {
				for (int y = 0; y <= loadedWorld.GetUpperBound(1); y++) {
					TileController.Instance.InitializeNewTile(x, y, loadedWorld[x, y]);
				}
			}
			world_fg = loadedWorld;
			world_bg = loadedWorldBG;
			//RenderWorld();
		}

		[ContextMenu("Delete World")]
		void DeleteWorld() {
			if (File.Exists(Application.persistentDataPath + "/" + WORLD_SAVE_NAME)) {
				File.Delete(Application.persistentDataPath + "/" + WORLD_SAVE_NAME);
			}
			RenderWorld();
		}

		public string[,] GetWorld(int worldLayer = 0) {
			//NewWorld();
			return worldLayer == 0 ? world_fg : world_bg;
		}

		public string GetTile(int x, int y, int worldLayer = 0, int chunk = 0) {
			if (x > world_fg.GetUpperBound(0) || y > world_fg.GetUpperBound(1) || x < 0 || y < 0) {
				return "air";
			}
			return worldLayer == 0 ? world_fg[x, y] : world_bg[x, y];
		}
	//

	//Chunk Handling
	//-------------------------------------------------------------------------------
		public static Vector2Int GetWorldSize() {
			return new Vector2Int(s_worldWidth, s_worldHeight);
		}

		public static int GetWorldWidth() {
			return s_worldWidth;
		}

		public static int GetWorldHeight() {
			return s_worldHeight;
		}

		public static int GetWorldChunkWidth() {
			return worldChunkWidth;
		}

		public static int GetWorldChunkHeight() {
			return worldChunkHeight;
		}

		public static int GetChunkCount() {
			return worldChunkWidth * worldChunkHeight;
		}

		public static bool InWorldBounds(int x, int y) {
			if (x < 0 || x >= s_worldWidth || y < 0 || y >= s_worldHeight) {
				return false;
			} else {
				return true;
			}
		}

		/// <summary>
        /// Uses player's position to get the current chunk the player is in.null
		/// If the new chunk is actually different, updates the visible chunks.
        /// </summary>
        /// <returns></returns>
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

		/// <summary>
        /// Uses tile position to get the appropriate containing chunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
		public static int GetChunk(int x, int y) {
			int chunk = 0;

			if (x < 0 || x > s_worldWidth || y < 0 || y > s_worldHeight) {
				return -1;
			}

			int adjustedX = (int)(x/chunkSize);
			int adjustedY = (int)(y/chunkSize);
			chunk = (adjustedY * worldChunkWidth) + adjustedX;

			return chunk;
		}

		public Transform GetChunkParent(int chunk) {
			return cObjs.GetChunkObject(chunk).transform;
		}

		public List<Transform> GetAllChunkParents() {
			return cObjs.GetAllChunkParents();
		}

		/// <summary>
        /// Returns a 2D array of a chunk's individual tiles.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="worldLayer"></param>
        /// <returns></returns>
		public string[,] GetChunkTiles(int chunk, int worldLayer = 0) {
			//NewWorld();
			Vector2Int chunkPos = GetChunkPosition(chunk);

			chunkTiles = new string[chunkSize, chunkSize];

			int adjustedX = chunkPos.x;
			int adjustedY = chunkPos.y;
			for (int y = 0; y < chunkSize; y++) {
				for (int x = 0; x < chunkSize; x++) {
					string newTile = worldLayer == 0 ? world_fg[adjustedX, adjustedY] : world_bg[adjustedX, adjustedY];
					chunkTiles[x, y] = newTile;

					adjustedX++;
				}
				adjustedY++;
				adjustedX = chunkPos.x;
			}

			return chunkTiles;
		}

		/// <summary>
        /// Gets bottom-left tile coordinates of a given chunk.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
		public static Vector2Int GetChunkPosition(int chunk) {
			int adjustedX = chunk % worldChunkWidth;
			int adjustedY = chunk / worldChunkWidth;
			return new Vector2Int(adjustedX * chunkSize, adjustedY * chunkSize);
		}

		/// <summary>
        /// Called when entering a new chunk. Determines which new chunks must be shown.
        /// </summary>
        /// <param name="currentChunk"></param>
        /// <returns></returns>
		int[] GetChunksToShow(int currentChunk) {
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

		/// <summary>
        /// Shows and hides chunks based on player entering new chunks.
        /// </summary>
        /// <returns></returns>
		void UpdateCulledChunks() {
			int[] chunksShowing = Helpers.GetArrFromList(showingChunks);
			int[] chunksToShow = GetChunksToShow(curChunk);
			int[] oldChunksToShow = new int[chunksToShow.Length];
			chunksToShow.CopyTo(oldChunksToShow, 0);

			int[] chunksToHide;
			if (chunksShowing != null) {
				chunksToHide = (chunksShowing.Except(chunksToShow)).ToArray();
				chunksToShow = (chunksToShow.Except(chunksShowing)).ToArray();
			} else {
				chunksToHide = new int[0];
			}
			//Debug.Log("Chunks to hide: " + ArrToString(chunksToHide));
			//Debug.Log("Chunks to show: " + ArrToString(chunksToShow));

			cObjs.UpdateShownChunks(chunksToShow, chunksToHide);
			lqCon.SetChunksToRender(oldChunksToShow);

			showingChunks = Helpers.GetListFromArr(oldChunksToShow);
		}

		public List<int> GetChunksShowing() {
			return showingChunks;
		}

	//

	//Tile Manipulator Functions
	//-------------------------------------------------------------------------------
		/// <summary>
        /// Changes a single tile to a specified value.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="newTile"></param>
        /// <returns></returns>
		public void ModifyTile(int x, int y, string newTile, bool updateWorld = true) {
			world_fg[x, y] = newTile;

			if (!updateWorld)
				return;

			int chunkToModify = GetChunk(x, y);

			lqCon.StartChunkLiquidSimulation(chunkToModify);

			//Re-render tile's chunk
			//wRend.RenderChunkTiles(chunkToModify);
			wRend.RenderTile(x, y, newTile);

			//Re-render tile's lightmap
			ltCon.HandleNewTile(x, y, newTile, world_bg[x, y]);

			//Remove water from position if placing new block
			if (newTile != "air")
				lqCon.EmptyLiquidBlock(x, y);
			
			navGrid.UpdateNavNode(x, y, newTile == "air");
		}

		/// <summary>
        /// Sets a specified tile to 0. Public function for world modification.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
		public string RemoveTile(int x, int y) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				return "nulltile";
			}
			if (world_fg[x,y] == "air") {
				return "nulltile";
			}

			string blockID = world_fg[x, y];
			ModifyTile(x, y, "air");
			return blockID;
		}

		/// <summary>
        /// Adds a specified tile value. Public function for world modification.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="newTile"></param>
        /// <returns></returns>
		public string AddTile(int x, int y, string newTile) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				return "nulltile";
			}
			if (world_fg[x,y] != "air") {
				return "nulltile";
			}

			ModifyTile(x, y, newTile);
			return "air";
		}

		/// <summary>
        /// Checks if a tile exists in the specified world layer (FG/BG)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="worldLayer"></param>
        /// <returns></returns>
		public bool isTile(int x, int y, int worldLayer = 0) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				Debug.Log("Out of map tile " + x + ", " + y);
				return false;
			}
			bool tileExists = (worldLayer == 0 ? world_fg[x, y] != "air" : world_bg[x, y] != "air");
			return tileExists;
		}

		/// <summary>
        /// Checks if a position is empty of both background and foreground tiles.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
		public bool isSky(int x, int y) {
			if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
				//Debug.Log("Out of map tile " + x + ", " + y);
				return false;
			}
			bool skyTile = world_fg[x, y] == "air" && world_bg[x, y] == "air";
			return skyTile;
		}

		/// <summary>
        /// Checks if a tile has any empty neighbors (and should therefore have a collider)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool isTileOpen(int x, int y) {
            if (x > world_fg.GetUpperBound(0) || x < world_fg.GetLowerBound(0) || y > world_fg.GetUpperBound(1) || y < world_fg.GetLowerBound(1)) {
                return false;
            }
            if (world_fg[x,y] == "air") {
                return false;
            }

            for (int i = x-1; i < x+2; i++) {
                for (int j = y-1; j < y+2; j++) {
                    if (i > world_fg.GetUpperBound(0) || i < world_fg.GetLowerBound(0) || j > world_fg.GetUpperBound(1) || j < world_fg.GetLowerBound(1)) {
                        return true;
                    }       

                    if (world_fg[i,j] == "air") {
                        return true;
                    }
                }
            }

            return false;
        }

	//
}
