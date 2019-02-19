using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
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
    public List<LightItem> lightItems = new List<LightItem>();
    private static Dictionary<int, Item> items;
    private static Dictionary<int, BlockItem> blocks;

    public void InitializeItemManager()
    {
        wCon = GetComponent<WorldController>();

        FillItemsArray();
        
        s_defaultDroppedItemPrefab = defaultDroppedItemPrefab;
        s_droppedItemPooler = droppedItemPooler;

        GetChunkObjects();
        s_chunkParents = chunkParents;

        initialized = true;
    }

    void FillItemsArray() {
        items = new Dictionary<int, Item>();
        blocks = new Dictionary<int, BlockItem>();
        for (int i = 0; i < blockItems.Count; i++) {
            BlockItem newBlock = blockItems[i];
            Item newItem = newBlock;
            items[newItem.id] = newItem;
            blocks[newBlock.blockID] = newBlock;
        }
        for (int i = 0; i < lightItems.Count; i++) {
            Item newItem = lightItems[i];
            items[newItem.id] = newItem;
        }
    }

    void GetChunkObjects() {
        chunkParents = wCon.GetAllChunkParents();
    }

    public static Item GetItem(int id) {
        if (!initialized) {
            return null;
        }
        if (!items.ContainsKey(id)) {
            id = 999; //Default item
        }
        return items[id];
    }

    public static BlockItem GetBlockItem(int blockID) {
        if (!initialized) {
            return null;
        }
        if (!blocks.ContainsKey(blockID)) {
            Debug.Log("Block of id " + blockID + " doesn't exist...");
            blockID = 999;
        }
        return blocks[blockID];
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

    public static void SpawnDroppedItem(int id, float x, float y, Vector2 spawnForce, int startStack = 1) {
        ItemObject itemObj = new ItemObject(id, startStack);
        SpawnDroppedItem(itemObj, x, y, spawnForce);
    }

    public static void SpawnDroppedBlock(int blockID, float x, float y, Vector2 spawnForce, int startStack = 1) {
        ItemObject itemObj = new ItemObject(GetBlockItem(blockID).id, startStack);
        SpawnDroppedItem(itemObj, x, y, spawnForce);
    }
}
