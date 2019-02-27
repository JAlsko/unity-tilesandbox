using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int inventorySize = 32;
    private int firstOpenSlot = -1;
    private ItemObject[] inventory;
    private InventorySlotObject[] invSlots;
    private UIController uic;

    private List<int> openSlots = new List<int>();

    void Start() {
        inventory = new ItemObject[inventorySize];
        Cursor.visible = false;

        for (int i = 0; i < inventory.Length; i++) {
            openSlots.Add(i);
        }
    }

    public void LinkUI(UIController newUIC, InventorySlotObject[] invSlotObjs) {
        uic = newUIC;

        invSlots = new InventorySlotObject[inventorySize];
        for (int i = 0; i < inventorySize; i++) {
            invSlots[i] = invSlotObjs[i];
            invSlots[i].InitializeInventory(this, i);
        }

        //AddItem(0, new ItemObject(1, 10));
        //AddItem(1, new ItemObject(2, 1));
        //AddItem(2, new ItemObject(1, 999));
    }

    bool IsValidInvSlot(int invSlot) {
        return (invSlot < inventorySize && invSlot >= 0);
    }

    int GetFirstOpenSlot() {
        if (openSlots.Count <= 0) {
            return -1;
        }

        return openSlots[0];
    }

    int DistributeNewAddedItem(int id, int currentStack, int index) {
        if (index >= inventorySize) {
            return currentStack;
        }

        if (index < 0) {
            return -1;
        }

        if (currentStack <= 0) {
            return 0;
        }

        for (int i = index; i < inventorySize; i++) {
            if (inventory[i] == null) {
                continue;
            }
            if (inventory[i].id == id) {
                int addableAmount = Mathf.Min(ItemManager.GetItem(id).maxStackSize - inventory[i].currentStack, currentStack);
                inventory[i].currentStack += addableAmount;
                invSlots[i].UpdateItemIcon(inventory[i]);
                return DistributeNewAddedItem(id, currentStack - addableAmount, i+1);
            }
        }

        return currentStack;
    }

    public ItemObject TryAddItem(ItemObject newItem) {
        if (newItem == null) {
            Debug.Log("Trying to add null item!");
            return null;
        }
        ItemObject leftOverItem = new ItemObject(newItem.id, newItem.currentStack);
        int leftOverAmount = DistributeNewAddedItem(newItem.id, newItem.currentStack, 0);
        if (leftOverAmount <= 0) {
            return null;
        }
        int firstOpenSlot = GetFirstOpenSlot();
        if (firstOpenSlot == -1) {
            return leftOverItem;
        } else {
            AddItem(firstOpenSlot, leftOverItem);
            return null;
        }
    }

    public void AddItem(int invSlot, ItemObject newItem) {
        if (!IsValidInvSlot(invSlot)) {
            Debug.Log(invSlot + " is not a valid inventory slot!");
            return;
        }

        if (newItem == null) {
            Debug.Log("Trying to add null item!");
            return;
        }
            
        inventory[invSlot] = newItem;
        invSlots[invSlot].UpdateItemIcon(newItem);

        openSlots.Remove(invSlot);
    }

    public int AddItem(DroppedItem droppedItem) {
        int openSlot = GetFirstOpenSlot();
        if (openSlot == -1) {
            return -1;
        }

        ItemObject itemToAdd = droppedItem.GetDroppedItem();
        droppedItem.HideDroppedItem();
        AddItem(openSlot, itemToAdd);
        return 1;
    }

    public void RemoveItem(int invSlot) {
        if (!IsValidInvSlot(invSlot)) {
            return;
        }

        inventory[invSlot] = null;
        invSlots[invSlot].UpdateItemIcon(null);

        openSlots.Add(invSlot);
        openSlots.Sort();
    }

    public ItemObject GetContainedItem(int invSlot) {
        return inventory[invSlot];
    }

    public ItemObject InsertItemObject(ItemObject newItem, int invSlot, int amountToAdd = -1) {
        if (!IsValidInvSlot(invSlot)) {
            return null;
        }
        
        //If method is called without explicit amount of items to add, use whole stack
        if (amountToAdd == -1) {
            amountToAdd = newItem.currentStack;
        }

        ItemObject currentItem = inventory[invSlot];
        if (currentItem == null) {
            inventory[invSlot] = newItem;
            return null;
        }
        else if (newItem.id == currentItem.id) {
            int addableAmount = ItemManager.GetItem(newItem.id).maxStackSize - currentItem.currentStack;
            if (newItem.currentStack < addableAmount) {
                addableAmount = newItem.currentStack;
            }
            currentItem.currentStack += addableAmount;
            newItem.currentStack -= addableAmount;

            if (newItem.currentStack <= 0) {
                return null;
            }
            else {
                return newItem;
            }
        } 
        else {
            ItemObject oldItem = inventory[invSlot];
            inventory[invSlot] = newItem;
            return oldItem;
        }
    }

    public ItemObject TakeWholeItem(int invSlot) {
        if (!IsValidInvSlot(invSlot)) {
            return null;
        }

        ItemObject itemToGive = inventory[invSlot];
        RemoveItem(invSlot);
        return itemToGive;
    }

    public ItemObject TakePartialItem(int invSlot, int stackSize = 1, int currentStackSize = 0) {
        if (!IsValidInvSlot(invSlot)) {
            return null;
        }

        if (inventory[invSlot] == null) {
            return null;
        }
        
        int takeableAmount = inventory[invSlot].currentStack >= stackSize ? stackSize : inventory[invSlot].currentStack;
        ItemObject itemToGive = new ItemObject(inventory[invSlot].id, takeableAmount + currentStackSize);
        inventory[invSlot].currentStack -= takeableAmount;
        if (inventory[invSlot].currentStack <= 0) {
            RemoveItem(invSlot);
        }
        return itemToGive;
    }
}
