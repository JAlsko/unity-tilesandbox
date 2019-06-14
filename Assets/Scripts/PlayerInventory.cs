using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerInventory : MonoBehaviour
{
    public int backpackSize = 32;
    public int hotbarSize = 8;
    private int inventorySize;
    private int firstOpenSlot = -1;
    private InventorySlotObject[] invSlots;
    public Inventory inventory;
    private UIController uic;
    private List<int> openSlots = new List<int>();

    public void InitializePlayerInventory() {
        inventorySize = backpackSize + hotbarSize;
        inventory = new Inventory(inventorySize);
        Cursor.visible = false;

        for (int i = 0; i < inventorySize; i++) {
            openSlots.Add(i);
        }

        InventoryManager.Instance.AddNewInventory(0, inventory);
    }

    public void LinkUI(UIController newUIC, InventorySlotObject[] invSlotObjs) {
        uic = newUIC;

        invSlots = new InventorySlotObject[inventorySize];
        for (int i = 0; i < inventorySize; i++) {
            invSlots[i] = invSlotObjs[i];
            invSlots[i].LinkInventory(inventory, i);
        }

        CraftingController.Instance.LinkInventory(this);

        AddItem(0, new ItemObject("pickaxe", 1));
        AddItem(1, new ItemObject("water_bucket", 1));
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

    public int GetItemCount(string itemToCount) {
        int total = 0;

        for (int i = 0; i < inventory.inventory.Length; i++) {
            if (inventory.inventory[i] == null) {
                continue;
            }
            total += (inventory.inventory[i].name == itemToCount ? inventory.inventory[i].currentStack : 0);
        }

        return total;
    }

    public int GetItemCount(Item itemToCount) {
        return GetItemCount(itemToCount.name);
    }

    public bool RemoveItemStack(string itemToRemove, int countToRemove, bool removeBackwards = true) {
        if (GetItemCount(itemToRemove) < countToRemove) {
            return false;
        }

        int amountStillNeeded = countToRemove;

        if (removeBackwards) {
            for (int i = inventory.inventory.Length-1; i >= 0; i--) {
                if (inventory.inventory[i] == null) {
                    continue;
                }

                if (inventory.inventory[i].name != itemToRemove) {
                    continue;
                }

                amountStillNeeded = RemoveSlotItemPartial(i, amountStillNeeded);

                if (amountStillNeeded <= 0) {
                    break;
                }
            }
        } 
        
        else if (!removeBackwards) {
            for (int i = 0; i < inventory.inventory.Length; i++) {
                if (inventory.inventory[i].name != itemToRemove) {
                    continue;
                }

                amountStillNeeded = RemoveSlotItemPartial(i, amountStillNeeded);

                if (amountStillNeeded <= 0) {
                    break;
                }
            }
        }

        return true;
    }

    public bool RemoveItemStack(Item itemToRemove, int countToRemove, bool removeBackwards = true) {
        return RemoveItemStack(itemToRemove.name, countToRemove, removeBackwards);
    }

    int DistributeNewAddedItem(string name, int currentStack, int index) {
        //Debug.Log("Distributing item with stack " + currentStack);
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
            if (inventory.inventory[i] == null) {
                continue;
            }
            if (inventory.inventory[i].name == name) {
                int addableAmount = Mathf.Min(ItemManager.GetItem(name).maxStackSize - inventory.inventory[i].currentStack, currentStack);
                inventory.inventory[i].currentStack += addableAmount;
                //Debug.Log("Adding " + addableAmount + " to slot " + i);
                invSlots[i].UpdateItemVisuals(inventory.inventory[i]);
                return DistributeNewAddedItem(name, currentStack - addableAmount, i+1);
            }
        }

        return currentStack;
    }

    public ItemObject TryAddItem(ItemObject newItem) {
        if (newItem == null) {
            Debug.Log("Trying to add null item!");
            return null;
        }
        ItemObject leftOverItem = new ItemObject(newItem.name, newItem.currentStack);
        int leftOverAmount = DistributeNewAddedItem(newItem.name, newItem.currentStack, 0);
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
            
        inventory.inventory[invSlot] = newItem;
        invSlots[invSlot].UpdateItemVisuals(newItem);

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

    public void RemoveSlotItem(int invSlot) {
        if (!IsValidInvSlot(invSlot)) {
            return;
        }

        inventory.inventory[invSlot] = null;
        invSlots[invSlot].UpdateItemVisuals(null);

        openSlots.Add(invSlot);
        openSlots.Sort();
    }

    public int RemoveSlotItemPartial(int invSlot, int countToRemove) {
        int leftOverAmount = countToRemove;

        if (!IsValidInvSlot(invSlot)) {
            return leftOverAmount;
        }

        if (countToRemove >= inventory.inventory[invSlot].currentStack) {
            leftOverAmount = 0;
            RemoveSlotItem(invSlot);
        } else {
            leftOverAmount -= inventory.inventory[invSlot].currentStack;
            inventory.inventory[invSlot].currentStack -= countToRemove;
            invSlots[invSlot].UpdateItemVisuals(inventory.inventory[invSlot]);
        }
        
        return leftOverAmount;
    }

    public ItemObject GetContainedItem(int invSlot) {
        return inventory.inventory[invSlot];
    }

    public ItemObject InsertItemObject(ItemObject newItem, int invSlot, int amountToAdd = -1) {
        if (!IsValidInvSlot(invSlot)) {
            return null;
        }
        
        //If method is called without explicit amount of items to add, use whole stack
        if (amountToAdd == -1) {
            amountToAdd = newItem.currentStack;
        }

        ItemObject currentItem = inventory.inventory[invSlot];
        if (currentItem == null) {
            inventory.inventory[invSlot] = newItem;
            openSlots.Remove(invSlot);
            return null;
        }
        else if (newItem.name == currentItem.name) {
            int addableAmount = ItemManager.GetItem(newItem.name).maxStackSize - currentItem.currentStack;
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
            ItemObject oldItem = inventory.inventory[invSlot];
            inventory.inventory[invSlot] = newItem;
            return oldItem;
        }
    }

    public ItemObject TakeWholeItem(int invSlot) {
        if (!IsValidInvSlot(invSlot)) {
            return null;
        }

        ItemObject itemToGive = inventory.inventory[invSlot];
        RemoveSlotItem(invSlot);
        return itemToGive;
    }

    public ItemObject TakePartialItem(int invSlot, int stackSize = 1, int currentStackSize = 0) {
        if (!IsValidInvSlot(invSlot)) {
            return null;
        }

        if (inventory.inventory[invSlot] == null) {
            return null;
        }
        
        int takeableAmount = inventory.inventory[invSlot].currentStack >= stackSize ? stackSize : inventory.inventory[invSlot].currentStack;
        ItemObject itemToGive = new ItemObject(inventory.inventory[invSlot].name, takeableAmount + currentStackSize);
        inventory.inventory[invSlot].currentStack -= takeableAmount;
        if (inventory.inventory[invSlot].currentStack <= 0) {
            RemoveSlotItem(invSlot);
        }
        return itemToGive;
    }
}
