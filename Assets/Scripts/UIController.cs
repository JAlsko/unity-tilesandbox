using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public PlayerInventory playerInv;
    public GameObject itemUIPrefab;
    public Transform itemUIParent;
    public InventorySlotObject[] invSlotObjs;

    void Start()
    {
        InitializeInventoryItemUI();
        playerInv.LinkUI(this, invSlotObjs);
    }

    void InitializeInventoryItemUI()
    {
        invSlotObjs = new InventorySlotObject[playerInv.inventorySize];
        for (int i = 0; i < playerInv.inventorySize; i++) {
            GameObject newItemUI = Instantiate(itemUIPrefab, itemUIParent);
            invSlotObjs[i] = newItemUI.GetComponent<InventorySlotObject>();
        }
    }
}
