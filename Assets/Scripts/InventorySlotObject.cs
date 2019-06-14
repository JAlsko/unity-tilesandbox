using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotObject : MonoBehaviour
{
    public Image itemIcon;
    public Text itemCount;

    public Sprite defaultSprite;

    public Image highlightPanel;

    private Inventory linkedInv;
    private int linkedSlot;

    void Start()
    {
        if (defaultSprite == null) {
            defaultSprite = itemIcon.sprite;
        }
        //UnhighlightSlot();
    }

    void DebugSlotItem() {
        ItemObject containedItem = GetContainedItem();
        if (containedItem == null) {
            return;
        }
        //Debug.Log("Item: " + ItemManager.GetItem(containedItem.id).name + " - Count: " + containedItem.currentStack);
    }

    public void LinkInventory(Inventory inv, int thisSlot) {
        linkedInv = inv;
        linkedSlot = thisSlot;

        UpdateItemVisuals(linkedInv.inventory[linkedSlot]);
    }

    public void UnlinkInventory() {
        linkedInv = null;
        linkedSlot = -1;

        HideItemIcon();
    }

    public ItemObject GetContainedItem() {
        return linkedInv.GetContainedItem(linkedSlot);
    }

    public ItemObject InsertItemObject(ItemObject newItem, int amountToAdd = -1) {
        ItemObject leftoverItem = linkedInv.InsertItemObject(newItem, linkedSlot, amountToAdd);
        
        UpdateItemVisuals(GetContainedItem());

        DebugSlotItem();

        return leftoverItem;
    }

    public ItemObject TakeWholeItem() {
        HideItemIcon();
        ItemObject takenItem = linkedInv.TakeWholeItem(linkedSlot);
        DebugSlotItem();
        return takenItem; 
    }

    public ItemObject TakePartialItem(int stackSize = 1, int currentStackSize = 0) {
        ItemObject itemToGive = linkedInv.TakePartialItem(linkedSlot, stackSize, currentStackSize);

        ItemObject newSlotItem = GetContainedItem();
        if (newSlotItem == null)
            HideItemIcon();
        else 
            UpdateItemVisuals(newSlotItem);

        DebugSlotItem();

        return itemToGive;
    }

    void RemoveItem() {
        linkedInv.RemoveSlotItem(linkedSlot);
        HideItemIcon();
    }

    public void UpdateItemVisuals(ItemObject newItem) {
        if (newItem == null) {
            HideItemIcon();
        } 
        
        else if (newItem.currentStack <= 0) {
            RemoveItem();
            HideItemIcon();
        }

        else {
            itemIcon.sprite = ItemManager.GetItem(newItem.name).icon;
            itemCount.text = newItem.currentStack > 1 ? newItem.currentStack + "" : "";
            itemIcon.enabled = true;
        }
    }

    public void HideItemIcon() {
        itemIcon.sprite = defaultSprite;
        itemCount.text = "";
    }

    public void HighlightSlot() {
        highlightPanel.enabled = true;
    }

    public void UnhighlightSlot() {
        highlightPanel.enabled = false;
    }
}
