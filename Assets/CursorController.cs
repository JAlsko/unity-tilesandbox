using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CursorController : MonoBehaviour
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

    public SpriteRenderer playerHeldItem;
    public DynamicLightSource playerHeldItemLight;
    public ItemObject heldItem;

    public Vector2 defaultFlingForce;

    Vector2 cursorPos;

    public Camera mainCam;

    public Sprite cursorSprite;
    public Sprite nullSprite;
    public Transform cursor;
    public Image cursorIcon;
    public Image heldItemIcon;
    public TextMeshProUGUI heldItemCount;

    public GraphicRaycaster canvasRaycaster;
    public PointerEventData pointerEventData;
    public EventSystem eventSystem;

    private ClickAction currentAction = ClickAction.Any;
    public float elapsedTime = 0;
    public float lastTime = 0;
    public float clickActionSpeed = 0.1f;
    public int consecutiveClicks = 0;
    public int accelerationClickThreshold = 2;
    public float clickActionAcceleration = 0.1f;
    public float clickActionMinSpeed = 0.1f;
    public float clickActionMaxSpeed = 0.01f;

    void Start()
    {
        playerHeldItemLight.enabled = false;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateCursorPos();
        
        if (Input.GetKey(KeyCode.Mouse0)) {
            if (!CanPerformClickAction()) {
                return;
            }

            GameObject invSlotObj = CheckHoveredObject();
            
            if (invSlotObj == null) {

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.UseHotbarItem)) { //Input 0
                    UseHotbarItem();
                }

                else if (heldItem != null && (currentAction == ClickAction.Any || currentAction == ClickAction.UseHeldItem)) { //Input 1
                    UseHeldItem();
                }

            } 
            
            else if (invSlotObj != null) {
                InventorySlotObject invSlot = CheckHoveredObject().GetComponent<InventorySlotObject>();

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.TakeWholeItem)) { //Input 2
                    TakeWholeItem(invSlot);                    
                }

                else if (heldItem != null && (currentAction == ClickAction.Any || currentAction == ClickAction.PlaceWholeItem)) { //Input 3
                    PlaceWholeItem(invSlot);
                }

            }
        }

        else if (Input.GetKey(KeyCode.Mouse1)) {
            if (!CanPerformClickAction()) {
                return;
            }

            GameObject invSlotObj = CheckHoveredObject();

            if (invSlotObj == null) {

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.DropHotbarItem)) { //Input 4
                    DropHotbarItem();
                }

                else if (heldItem != null && (currentAction == ClickAction.Any || currentAction == ClickAction.DropHeldItem)) { //Input 5
                    DropHeldItem();
                }

            } 
            
            else if (invSlotObj != null) {
                InventorySlotObject invSlot = CheckHoveredObject().GetComponent<InventorySlotObject>();

                if (heldItem == null && (currentAction == ClickAction.Any || currentAction == ClickAction.TakeSingleItem)) { //Input 6
                    TakeSingleItem(invSlot);                    
                }

                else if (heldItem != null) {
                    ItemObject invItem = invSlot.GetContainedItem();
                    if (invItem == null) { //Input 7
                        if (currentAction == ClickAction.Any || currentAction == ClickAction.PlaceSingleItem) {
                            PlaceSingleItem(invSlot);
                        }
                    }
                    else if (invItem.id == heldItem.id) {
                        if (heldItem.currentStack <= ItemManager.GetItem(heldItem.id).maxStackSize-1 && (currentAction == ClickAction.Any || currentAction == ClickAction.TakeSingleItem)) { //Input 8
                            TakeSingleItem(invSlot);
                        }
                    }
                }
            }
        }

        if ((Input.GetKeyUp(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1)) || (Input.GetKeyUp(KeyCode.Mouse1) && !Input.GetKey(KeyCode.Mouse0))) {
            currentAction = ClickAction.Any;
            ResetClickSpeed();
        }
    }

    void UseHotbarItem() {
        WorldModifier.Instance.RemoveTile();
        IncrementClicks();
        currentAction = ClickAction.UseHotbarItem;
    }

    void UseHeldItem() {
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

    void TakeSingleItem(InventorySlotObject invSlot) {
        IncrementClicks();
        if (heldItem == null) {
            heldItem = invSlot.TakePartialItem(1);
        } else {
            heldItem = invSlot.TakePartialItem(1, heldItem.currentStack);
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
    }

    GameObject CheckHoveredObject() {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        canvasRaycaster.Raycast(pointerEventData, raycastResults);

        foreach (RaycastResult result in raycastResults) {
            if (result.gameObject.tag == "InventorySlot") {
                return result.gameObject;
            }
        }

        return null;
    }

    void UpdateHeldItemVisuals() {
        if (heldItem == null) {
            HideHeldItemVisuals();
        } else {
            Sprite newIcon = ItemManager.GetItem(heldItem.id).icon;
            playerHeldItem.sprite = newIcon;
            heldItemIcon.sprite = newIcon;
            heldItemCount.text = heldItem.currentStack > 1 ? heldItem.currentStack + "" : "";

            float lightVal = ItemManager.GetItem(heldItem.id).lightStrength;
            if (lightVal > 0) {
                Color lightColor = ItemManager.GetItem(heldItem.id).lightColor;
                playerHeldItemLight.startLightStrength = lightVal;
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

        playerHeldItemLight.DisableLight();
    }

    bool CanPerformClickAction() {
        if (elapsedTime >= lastTime + clickActionSpeed) {
            lastTime = elapsedTime;
            return true;
        }
        return false;
    }

    void ResetClickSpeed() {
        consecutiveClicks = 0;
        clickActionSpeed = clickActionMinSpeed;
        lastTime = 0;
    }

    void IncrementClicks() {
        consecutiveClicks++;
        if (consecutiveClicks >= accelerationClickThreshold && clickActionSpeed > clickActionMaxSpeed) {
            clickActionSpeed *= clickActionAcceleration;
        }
    }
}
