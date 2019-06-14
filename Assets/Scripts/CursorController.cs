using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CursorController : Singleton<CursorController>
{
    public enum ClickAction {
        Any = -1,
        UseHotbarItem = 0,
        UseHeldItem = 1,
        TakeWholeItem = 2,
        PlaceWholeItem = 3,
        DropHotbarItem = 4,
        DropHeldItem = 5,
        TakeSingleItem = 6,
        PlaceSingleItem = 7,
    }

    public PlayerInventory playerInv;

    public SpriteRenderer playerHeldItem;
    public DynamicLightSource playerHeldItemLight;
    public ItemObject heldItem = null;

    public Vector2 defaultFlingForce;

    Vector2 cursorPos;

    public Camera mainCam;

    public Sprite cursorSprite;
    public Sprite nullSprite;
    public Transform cursor;
    public Image cursorIcon;
    public Image heldItemIcon;
    public Text heldItemCount;

    public GraphicRaycaster canvasRaycaster;
    public PointerEventData pointerEventData;
    public EventSystem eventSystem;

    private ClickAction currentAction = ClickAction.Any;
    public float elapsedTime = 0;
    public float lastTime = 0;
    public float inventoryActionSpeed = 0.1f;
    public int consecutiveClicks = 0;
    public int accelerationClickThreshold = 2;
    public float inventoryActionAcceleration = 0.1f;
    public float inventoryActionMinSpeed = 0.1f;
    public float inventoryActionMaxSpeed = 0.01f;
    public int itemsPerAction = 1;
    public float minItemsPerAction = 1;
    public float maxItemsPerAction = 5;

    public float itemActionSpeed = 0.1f;

    private float heldLightVal = 0;

    private int selectedHotbarSlot = 0;
    public float minScrollDelta = 0.1f;
    public float scrollSpeed = 2;

    private InventorySlotObject selectedInvSlot;

	Vector3 curMousePos;
    public Transform tileSelectionBox;
    public int tileSelectionDiameter = 1;
    public Entity playerEntity;
    Vector3 adjustedMousePos = Vector3.zero;

    public void InitializeCursorControls()
    {
        playerHeldItemLight.enabled = false;
        selectedInvSlot = UIController.Instance.playerInvSlotObjs[0];

        tileSelectionDiameter = (int)playerEntity.GetFloat("digRadius");
        tileSelectionBox.localScale = new Vector3(tileSelectionDiameter * 1.125f, tileSelectionDiameter * 1.125f, 1f);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateCursorPos();

        if (Mathf.Abs(Input.mouseScrollDelta.y) > minScrollDelta) {
            int oldSelectedHotbarSlot = selectedHotbarSlot;
            selectedHotbarSlot = (selectedHotbarSlot - (int)(scrollSpeed * Input.mouseScrollDelta.y)) % playerInv.hotbarSize;
            if (selectedHotbarSlot < 0)
                selectedHotbarSlot = UIController.Instance.playerInv.hotbarSize-1;

            selectedInvSlot = UIController.Instance.playerInvSlotObjs[selectedHotbarSlot];

            UIController.Instance.UnhighlightInvSlot(oldSelectedHotbarSlot);
            UIController.Instance.HighlightInvSlot(selectedHotbarSlot);
        }

        if (Input.GetKey(KeyCode.Mouse0)) {
            if (!CanPerformClickAction(currentAction)) {
                return;
            }

            GameObject invSlotObj = CheckHoveredObject();
            GameObject craftSlotObj = CheckHoveredObject("CraftingOutput");
            
            if (invSlotObj == null && craftSlotObj == null) {

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.UseHotbarItem)) { //Input 0
                    UseHotbarItem(selectedInvSlot);
                }

                else if (heldItem != null && (currentAction == ClickAction.Any || currentAction == ClickAction.UseHeldItem)) { //Input 1
                    UseHeldItem();
                }

            } 
            
            else if (invSlotObj != null) {
                InventorySlotObject invSlot = invSlotObj.GetComponent<InventorySlotObject>();

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.TakeWholeItem)) { //Input 2
                    TakeWholeItem(invSlot);                    
                }

                else if (heldItem != null && (currentAction == ClickAction.Any || currentAction == ClickAction.PlaceWholeItem)) { //Input 3
                    PlaceWholeItem(invSlot);
                }

            }

            else if (craftSlotObj != null) {
                if (currentAction == ClickAction.Any || currentAction == ClickAction.TakeWholeItem) {
                    CraftingOutput cOut = craftSlotObj.GetComponent<CraftingOutput>();
                    TakeCraftingOutput(cOut);
                }
            }
        }

        else if (Input.GetKey(KeyCode.Mouse1)) {
            if (!CanPerformClickAction(currentAction)) {
                return;
            }

            GameObject invSlotObj = CheckHoveredObject();
            GameObject craftSlotObj = CheckHoveredObject("CraftingOutput");

            if (invSlotObj == null && craftSlotObj == null) {

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.DropHotbarItem)) { //Input 4
                    //Right click action with only hotbar item in hand
                }

                else if (heldItem != null && (currentAction == ClickAction.Any || currentAction == ClickAction.DropHeldItem)) { //Input 5
                    DropHeldItem();
                }

            } 
            
            else if (invSlotObj != null) {
                InventorySlotObject invSlot = invSlotObj.GetComponent<InventorySlotObject>();

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.TakeSingleItem)) { //Input 6
                    TakeSingleItem(invSlot);                    
                }

                else if (heldItem != null) {
                    ItemObject invItem = invSlot.GetContainedItem();
                    if (invItem == null) {
                        if (currentAction == ClickAction.Any || currentAction == ClickAction.PlaceSingleItem) {
                            PlaceSingleItem(invSlot);
                        }
                    }
                    else if (invItem.name == heldItem.name) {
                        if (heldItem.currentStack <= ItemManager.GetItem(heldItem.name).maxStackSize-1 && (currentAction == ClickAction.Any || currentAction == ClickAction.TakeSingleItem)) { //Input 8
                            TakeSingleItem(invSlot);
                        }
                    }
                }
            }

            else if (craftSlotObj != null) {
                if (currentAction == ClickAction.Any || currentAction == ClickAction.TakeSingleItem) {
                    CraftingOutput cOut = craftSlotObj.GetComponent<CraftingOutput>();
                    TakeCraftingOutput(cOut);
                }
            }
        }

        if ((Input.GetKeyUp(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1)) || (Input.GetKeyUp(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Mouse0))) {
            currentAction = ClickAction.Any;
            ResetClickSpeed();
        }
    }

    void UseHotbarItem(InventorySlotObject invSlot) {
        if (UIController.Instance.HoveringOverUI()) {
            return;
        }

        ItemObject hotbarItem = invSlot.GetContainedItem();
        if (hotbarItem == null) {
            return;
        }
        hotbarItem.Use();
        invSlot.UpdateItemVisuals(hotbarItem);
        
        IncrementClicks();
        currentAction = ClickAction.UseHotbarItem;
    }

    void UseHeldItem() {
        if (UIController.Instance.HoveringOverUI()) {
            return;
        }

        heldItem.Use();
        if (heldItem.currentStack <= 0) {
            heldItem = null;
        }
        UpdateHeldItemVisuals();
        IncrementClicks();
        currentAction = ClickAction.UseHeldItem;
    }

    void TakeWholeItem(InventorySlotObject invSlot) {
        IncrementClicks();
        heldItem = invSlot.TakeWholeItem();
        UpdateHeldItemVisuals();
        currentAction = ClickAction.TakeWholeItem;
    }

    void PlaceWholeItem(InventorySlotObject invSlot) {
        IncrementClicks();
        heldItem = invSlot.InsertItemObject(heldItem);
        UpdateHeldItemVisuals();
        currentAction = ClickAction.PlaceWholeItem;
    }

    void DropHotbarItem() {
        IncrementClicks();
        currentAction = ClickAction.DropHotbarItem;
    }

    void DropHeldItem() {
        IncrementClicks();
        ItemManager.SpawnDroppedItem(heldItem, (int)cursorPos.x, (int)cursorPos.y, defaultFlingForce);
        heldItem = null;
        UpdateHeldItemVisuals();
        currentAction = ClickAction.DropHeldItem;
    }

    void TakeCraftingOutput(CraftingOutput cOut) {
        IncrementClicks();
        heldItem = cOut.TakeCraftedItem(heldItem);
        UpdateHeldItemVisuals();
        currentAction = ClickAction.TakeSingleItem;
    }

    void TakeSingleItem(InventorySlotObject invSlot) {
        IncrementClicks();
        if (heldItem == null) {
            heldItem = invSlot.TakePartialItem(5);
        } else {
            heldItem = invSlot.TakePartialItem(5, heldItem.currentStack);
        }
        UpdateHeldItemVisuals();
        currentAction = ClickAction.TakeSingleItem;
    }

    void PlaceSingleItem(InventorySlotObject invSlot) {
        heldItem = invSlot.InsertItemObject(heldItem, 1);
        UpdateHeldItemVisuals();
        currentAction = ClickAction.PlaceSingleItem;
    }

    void UpdateCursorPos() {
        cursorPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        cursor.position = Input.mousePosition;

        bool oddSelectionSize = tileSelectionDiameter % 2 != 0;

        curMousePos = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCam.transform.position.z));

        if (oddSelectionSize) {
            curMousePos = SimplifyMousePos(curMousePos);
            adjustedMousePos = AdjustMousePos(curMousePos, .5f);
        } else {
            curMousePos = SimplifyMousePos(curMousePos, .5f);
            adjustedMousePos = AdjustMousePos(curMousePos, 0);
        }
        tileSelectionBox.position = adjustedMousePos;
    }

    GameObject CheckHoveredObject(string tag = "InventorySlot") {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        canvasRaycaster.Raycast(pointerEventData, raycastResults);

        foreach (RaycastResult result in raycastResults) {
            if (result.gameObject.tag == tag) {
                return result.gameObject;
            }
        }

        return null;
    }

    void UpdateHeldItemVisuals() {
        if (heldItem == null) {
            HideHeldItemVisuals();
        } else {
            Sprite newIcon = ItemManager.GetItem(heldItem.name).icon;
            playerHeldItem.sprite = newIcon;
            heldItemIcon.sprite = newIcon;
            heldItemCount.text = heldItem.currentStack > 1 ? heldItem.currentStack + "" : "";

            heldLightVal = ItemManager.GetItem(heldItem.name).lightStrength;
            if (heldLightVal > 0) {
                Color lightColor = ItemManager.GetItem(heldItem.name).lightColor;
                playerHeldItemLight.startLightStrength = heldLightVal;
                playerHeldItemLight.lightColor = lightColor;
                playerHeldItemLight.enabled = true;
                playerHeldItemLight.EnableLight();
            }
        }
    }

    void HideHeldItemVisuals() {
        playerHeldItem.sprite = nullSprite;
        heldItemIcon.sprite = nullSprite;
        heldItemCount.text = "";

        if (heldLightVal > 0) {
            playerHeldItemLight.DisableLight();
            heldLightVal = 0;
        }
    }

    bool CanPerformClickAction(ClickAction action) {
        if (action == ClickAction.UseHeldItem || action == ClickAction.UseHotbarItem) {
            if (elapsedTime >= lastTime + itemActionSpeed) {
                lastTime = elapsedTime;
                return true;
            }
        } else {
            if (elapsedTime >= lastTime + inventoryActionSpeed) {
                lastTime = elapsedTime;
                return true;
            }
        }
        return false;
    }

    void ResetClickSpeed() {
        consecutiveClicks = 0;
        inventoryActionSpeed = inventoryActionMinSpeed;
        itemsPerAction = (int)minItemsPerAction;
        lastTime = 0;
    }

    void IncrementClicks() {
        consecutiveClicks++;
        if (consecutiveClicks >= accelerationClickThreshold && inventoryActionSpeed > inventoryActionMaxSpeed) {
            inventoryActionSpeed *= inventoryActionAcceleration;
            float ipaIncrease = (inventoryActionMinSpeed / inventoryActionMaxSpeed) / (inventoryActionSpeed / inventoryActionMinSpeed);
        }
    }

    Vector3 SimplifyMousePos(Vector3 mousePos, float offset = 0) {
        mousePos.x = (int)(mousePos.x + offset);
        mousePos.y = (int)(mousePos.y + offset);
        return mousePos;
    }

    Vector3 AdjustMousePos(Vector3 mousePos, float offset) { 
        mousePos.x += offset;
        mousePos.y += offset;
        return mousePos;
    }

    public Vector3 GetMousePos() {
        return curMousePos;
    }

    public Vector3 GetTileSelectionPos() {
        return adjustedMousePos;
    }
}
