using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public GameObject defaultDroppedItemPrefab;
    static GameObject s_defaultDroppedItemPrefab;
    public Transform droppedItemParent;
    static Transform s_droppedItemParent;

    public List<BlockItem> blockItems = new List<BlockItem>();
    public List<LightItem> lightItems = new List<LightItem>();
    private static Dictionary<int, Item> items;

    void Start()
    {
        FillItemsArray();
        s_defaultDroppedItemPrefab = defaultDroppedItemPrefab;
        s_droppedItemParent = droppedItemParent;
    }

    void FillItemsArray() {
        items = new Dictionary<int, Item>();
        for (int i = 0; i < blockItems.Count; i++) {
            Item newItem = blockItems[i];
            items[newItem.id] = newItem;
        }
        for (int i = 0; i < lightItems.Count; i++) {
            Item newItem = lightItems[i];
            items[newItem.id] = newItem;
        }
    }

    public static Item GetItem(int id) {
        if (!items.ContainsKey(id)) {
            id = 999; //Default item
        }
        return items[id];
    }

    public static void SpawnDroppedItem(ItemObject itemObj, int x, int y, Vector2 spawnForce) {
        GameObject newDroppedItem = Instantiate(s_defaultDroppedItemPrefab, new Vector3(x, y, 0), Quaternion.identity, s_droppedItemParent) as GameObject;
        DroppedItem di = newDroppedItem.GetComponentInChildren<DroppedItem>();
        di.InitializeItem(itemObj);
        di.AddForceToItem(spawnForce);
    }

    public static void SpawnDroppedItem(int id, int x, int y, Vector2 spawnForce, int startStack = 1) {
        ItemObject itemObj = new ItemObject(id, startStack);
        SpawnDroppedItem(itemObj, x, y, spawnForce);
    }
}
