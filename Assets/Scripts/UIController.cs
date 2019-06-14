using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIController : Singleton<UIController>
{
    public PlayerInventory playerInv;
    public GameObject itemUIPrefab;
    public Transform playerInventoryUIParent;
    public Transform externalInventoryListUIParent;
    public Transform externalInventoryUIParent;
    public Text externalInventoryUIName;
    public Transform hotbarUIParent;
    public InventorySlotObject[] playerInvSlotObjs;
    public InventorySlotObject[] externalInvSlotObjs;

    [Header("Maximum Displayable Inventory Size")]
    public int maxExternalInvSize = 64;
    private int cachedExternalInventoryID = -1;

    public Transform craftUIParent;
    public GameObject recipeUIPrefab;
    public Transform recipeListParent;
    public Dictionary<CraftRecipe.CraftingType, List<GameObject>> recipesByType = new Dictionary<CraftRecipe.CraftingType, List<GameObject>>();

    public CraftingOutput recipeOutput;
    public Text recipeOutputName;
    public Image recipeOutputIcon;
    public Text recipeOutputFlavorText;
    public GameObject recipeOutputButton;

    public Sprite emptySprite;

    public bool moveUIContinuing = false;
    public GameObject moveUI;
    public Vector3 moveUIOffset;

    void Start()
    {
        //InitializeInventoryItemUI();
        //InitializeRecipeUI();
        ToggleCrafting();
        TogglePlayerInventory();
        ToggleExternalInventory(-1);
    }

    public GraphicRaycaster canvasRaycaster;
    public PointerEventData pointerEventData;
    public EventSystem eventSystem;
    
    GameObject CheckHoveredObject(string tag) {
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

    public bool HoveringOverUI() {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        canvasRaycaster.Raycast(pointerEventData, raycastResults);

        return raycastResults.Count > 0;
    }

    void Update() {
        if (Input.GetKey(KeyCode.Mouse0)) {
            GameObject hoveredObject = CheckHoveredObject("UIHandle");
            if (hoveredObject) {
                if (hoveredObject.GetComponent<MoveableUI>()) {
                    if (!moveUIContinuing) {
                        moveUIContinuing = true;
                        GameObject mainUIObject = hoveredObject.GetComponent<MoveableUI>().mainUIObject;
                        moveUI = mainUIObject;
                        moveUIOffset = mainUIObject.transform.position - Input.mousePosition;
                    }
                }
            }
        }

        if (moveUIContinuing) {
            moveUI.transform.position = (Input.mousePosition + moveUIOffset);
        }

        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            moveUIContinuing = false;
            moveUI = null;
        }
    }

    public void InitializePlayerInventoryUI()
    {
        playerInvSlotObjs = new InventorySlotObject[playerInv.backpackSize + playerInv.hotbarSize];
        for (int i = 0; i < playerInv.backpackSize + playerInv.hotbarSize; i++) {
            GameObject newItemUI;
            if (i < playerInv.hotbarSize) {
                newItemUI = Instantiate(itemUIPrefab, hotbarUIParent);
            } else {
                newItemUI = Instantiate(itemUIPrefab, playerInventoryUIParent);
            }
            playerInvSlotObjs[i] = newItemUI.GetComponentInChildren<InventorySlotObject>();
            playerInvSlotObjs[i].UnhighlightSlot();
        }

        HighlightInvSlot(0);
        playerInv.LinkUI(this, playerInvSlotObjs);
    }

    public void InitializeExternalInventoryUI() {
        externalInvSlotObjs = new InventorySlotObject[maxExternalInvSize];

        for (int i = 0; i < maxExternalInvSize; i++) {
            GameObject newItemUI;
            newItemUI = Instantiate(itemUIPrefab, externalInventoryListUIParent);
            externalInvSlotObjs[i] = newItemUI.GetComponentInChildren<InventorySlotObject>();
            externalInvSlotObjs[i].UnhighlightSlot();
        }
    }

    public void OpenExternalInventoryUI(int invID) {
        if (invID != cachedExternalInventoryID) {
            cachedExternalInventoryID = invID;
            for (int i = 0; i < maxExternalInvSize; i++) {
                externalInvSlotObjs[i].gameObject.SetActive(false);
                externalInvSlotObjs[i].UnlinkInventory();
            }

            Inventory openedInventory = InventoryManager.Instance.GetInventory(invID);

            if (openedInventory == null) {
                Debug.Log("Opened inventory is null!");
                externalInventoryUIParent.gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < openedInventory.inventorySize; i++) {
                externalInvSlotObjs[i].gameObject.SetActive(true);
                externalInvSlotObjs[i].LinkInventory(openedInventory, i);
            }

            externalInventoryUIName.text = openedInventory.inventoryName.ToUpper();
        }

        externalInventoryUIParent.gameObject.SetActive(true);
    }

    public void CloseExternalInventoryUI() {
        externalInventoryUIParent.gameObject.SetActive(false);
    }

    public void HighlightInvSlot(int index) {
        playerInvSlotObjs[index].HighlightSlot();
    }

    public void UnhighlightInvSlot(int index) {
        playerInvSlotObjs[index].UnhighlightSlot();
    }

    public void TogglePlayerInventory() {
        playerInventoryUIParent.gameObject.SetActive(!playerInventoryUIParent.gameObject.activeInHierarchy);
    }

    public void ToggleExternalInventory(int toggleOp = 0, int invID = -1) {
        bool open = toggleOp != 0 ? (toggleOp == 1 ? true : false) : !externalInventoryUIParent.gameObject.activeInHierarchy;
        
        if (open) {
            OpenExternalInventoryUI(invID);
        } else if (!open) {
            CloseExternalInventoryUI();
        }
    }

    public void ToggleCrafting() {
        craftUIParent.gameObject.SetActive(!craftUIParent.gameObject.activeInHierarchy);
    }

    public void InitializeRecipeUI() {
        List<CraftRecipe> allRecipes = ItemManager.Instance.allRecipes;
        for (int i = 0; i < allRecipes.Count; i++) {
            if (allRecipes[i] == null) {
                continue;
            }

            CraftRecipe curRecipe = allRecipes[i];
            GameObject recipePanel = CreateRecipePanel(curRecipe);
            if (!recipesByType.ContainsKey(curRecipe.craftingType)) {
                recipesByType[curRecipe.craftingType] = new List<GameObject>();
            }
            recipesByType[curRecipe.craftingType].Add(recipePanel);
            //recipePanel.SetActive(false);
        }

        HideRecipeOutput();
    }

    GameObject CreateRecipePanel(CraftRecipe recipe) {
        GameObject recipePanelObject = Instantiate(recipeUIPrefab);
        RecipePanel recipePanel = recipePanelObject.GetComponentInChildren<RecipePanel>();
        recipePanel.InitializeRecipe(recipe);
        recipePanelObject.transform.SetParent(recipeListParent);
        return recipePanelObject;
    }

    public void HideCraftingTypeRecipes(CraftRecipe.CraftingType cType) {
        List<GameObject> recipesToHide = recipesByType[cType];
        for (int i = 0; i < recipesToHide.Count; i++) {
            recipesToHide[i].SetActive(false);
        }
    }

    public void ShowCraftingTypeRecipes(CraftRecipe.CraftingType cType) {
        List<GameObject> recipesToHide = recipesByType[cType];
        for (int i = 0; i < recipesToHide.Count; i++) {
            recipesToHide[i].SetActive(true);
        }
    }

    public void FocusCraftingRecipe(RecipePanel recipePanel) {
        CraftRecipe recipe = recipePanel.recipe;
        ShowRecipeOutput(recipe);
        recipeOutput.UpdateCraftRecipe(recipe);
    }

    void ShowRecipeOutput(CraftRecipe recipe) {
        recipeOutputName.text = Helpers.AdjustItemName(recipe.outputItem.name);
        recipeOutputIcon.sprite = recipe.outputItem.icon;
        recipeOutputFlavorText.text = recipe.outputItem.flavorText;
        recipeOutputButton.SetActive(true);
    }

    void HideRecipeOutput() {
        recipeOutputName.text = "";
        recipeOutputIcon.sprite = emptySprite;
        recipeOutputFlavorText.text = "";
        recipeOutputButton.SetActive(false);
        recipeOutput.UpdateCraftRecipe(null);
    }
}
