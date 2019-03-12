using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotObject : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemCount;

    public Sprite defaultSprite;

    private PlayerInventory linkedInv;
    private int linkedSlot;

    void Start()
    {
        if (defaultSprite == null) {
            defaultSprite = itemIcon.sprite;
        }
    }

    void DebugSlotItem() {
        ItemObject containedItem = GetContainedItem();
        if (containedItem == null) {
            return;
        }
        //Debug.Log("Item: " + ItemManager.GetItem(containedItem.id).name + " - Count: " + containedItem.currentStack);
    }

    public void InitializeInventory(PlayerInventory pInv, int thisSlot) {
        linkedInv = pInv;
        linkedSlot = thisSlot;
    }

    public ItemObject GetContainedItem() {
        return linkedInv.GetContainedItem(linkedSlot);
    }

    public ItemObject InsertItemObject(ItemObject newItem, int amountToAdd = -1) {
        ItemObject leftoverItem = linkedInv.InsertItemObject(newItem, linkedSlot, amountToAdd);
        
        UpdateItemIcon(GetContainedItem());

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
            UpdateItemIcon(newSlotItem);

        DebugSlotItem();

        return itemToGive;
    }

    void RemoveItem() {
        linkedInv.RemoveItem(linkedSlot);
        HideItemIcon();
    }

    public void UpdateItemIcon(ItemObject newItem) {
        if (newItem == null) {
            HideItemIcon();
        } else {
            itemIcon.sprite = ItemManager.GetItem(newItem.id).icon;
            itemCount.text = newItem.currentStack > 1 ? newItem.currentStack + "" : "";
            itemIcon.enabled = true;
        }
    }

    public void HideItemIcon() {
        itemIcon.sprite = defaultSprite;
        itemCount.text = "";
    }
}
