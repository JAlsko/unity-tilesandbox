using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class ItemManager : Singleton<ItemManager>
{
    private WorldController wCon;
    static bool initialized = false;

    public GameObject defaultDroppedItemPrefab;
    static GameObject s_defaultDroppedItemPrefab;

    public Pooler droppedItemPooler;
    static Pooler s_droppedItemPooler;

    private List<Transform> chunkParents;
    static List<Transform> s_chunkParents;

    public List<BlockItem> blockItems = new List<BlockItem>();
    private static Dictionary<int, Item> items;
    private static Dictionary<string, Item> allItems = new Dictionary<string, Item>();
    private static Dictionary<int, BlockItem> allBlockItems = new Dictionary<int, BlockItem>();
    private static Dictionary<int, BlockItem> blocks;

    public List<CraftRecipe> allRecipes = new List<CraftRecipe>();

    void Start() {
        //InitializeAllItems();
    }

    public void InitializeAllItems() {
        Resources.LoadAll("Items/");
        Item[] foundItems = (Item[]) Resources.FindObjectsOfTypeAll(typeof(Item));
        foreach (Item i in foundItems) {
            allItems[i.name] = i;
            //Debug.Log("Loaded item " + i.name);
        }

        BlockItem[] foundBlockItems = (BlockItem[]) Resources.FindObjectsOfTypeAll(typeof(BlockItem));
        foreach (BlockItem i in foundBlockItems) {
            allBlockItems[i.blockID] = i;
            //Debug.Log("Loaded item " + i.name);
        }
        
        Resources.LoadAll("Recipes/");
        CraftRecipe[] foundRecipes = (CraftRecipe[]) Resources.FindObjectsOfTypeAll(typeof(CraftRecipe));
        foreach (CraftRecipe i in foundRecipes) {
            allRecipes.Add(i);
            //Debug.Log("Loaded recipe " + i.outputItem.name);
        }
    }

    public string GetResourcePath(string filePath) {
        if (!filePath.Contains("/Items/")) {
            return "";
        }

        int index = filePath.IndexOf("/Items/")+1;
        string resourcePath = filePath.Substring(index);
        return resourcePath;
    }

    public List<FileInfo> GetAllAssetFiles(string directory) {
        List<FileInfo> itemFiles = new List<FileInfo>();
        DirectoryInfo dir = new DirectoryInfo(directory);

        FileInfo[] fInfo = dir.GetFiles("*.asset", SearchOption.AllDirectories);
        fInfo.Select(f => f.FullName).ToArray();
        foreach (FileInfo f in fInfo) {
            itemFiles.Add(f);
        }

        return itemFiles;
    }

    public void InitializeItemManager()
    {
        wCon = GetComponent<WorldController>();
        
        s_defaultDroppedItemPrefab = defaultDroppedItemPrefab;
        s_droppedItemPooler = droppedItemPooler;

        GetChunkObjects();
        s_chunkParents = chunkParents;

        initialized = true;
    }

    void GetChunkObjects() {
        chunkParents = wCon.GetAllChunkParents();
    }

    public static Item GetItem(string name) {
        if (!initialized) {
            return null;
        }
        if (!allItems.ContainsKey(name)) {
            Debug.Log("No item called " + name);
            name = "DefaultItem"; //Default item
        }
        return allItems[name];
    }

    public static BlockItem GetBlockItem(int blockID) {
        if (!initialized) {
            return null;
        }
        if (!allBlockItems.ContainsKey(blockID)) {
            Debug.Log("Block of id " + blockID + " doesn't exist...");
            blockID = 999;
        }
        return allBlockItems[blockID];
    }

    public static void SpawnDroppedItem(ItemObject itemObj, float x, float y, Vector2 spawnForce) {
        //GameObject newDroppedItem = Instantiate(s_defaultDroppedItemPrefab, new Vector3(x+.5f, y+.5f, 0), Quaternion.identity, s_droppedItemParent) as GameObject;
        GameObject newDroppedItem = s_droppedItemPooler.GetPooledObject();
        if (!newDroppedItem.GetComponentInChildren<DroppedItem>()) {
            Debug.LogError("New Dropped Item Object has no Dropped Item script!");
            return;
        }
        newDroppedItem.transform.parent = s_chunkParents[WorldController.GetChunk((int)x, (int)y)];
        newDroppedItem.transform.position = new Vector3(x+.5f, y+.5f, 0);
        DroppedItem di = newDroppedItem.GetComponentInChildren<DroppedItem>();
        di.InitializeItem(itemObj);
        di.AddForceToItem(spawnForce);
    }

    public static void SpawnDroppedItem(string name, float x, float y, Vector2 spawnForce, int startStack = 1) {
        ItemObject itemObj = new ItemObject(name, startStack);
        SpawnDroppedItem(itemObj, x, y, spawnForce);
    }

    public static void SpawnDroppedBlock(int blockID, float x, float y, Vector2 spawnForce, int startStack = 1) {
        ItemObject itemObj = new ItemObject(GetBlockItem(blockID).name, startStack);
        SpawnDroppedItem(itemObj, x, y, spawnForce);
    }
}
