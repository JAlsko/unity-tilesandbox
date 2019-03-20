using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : Singleton<UIController>
{
    public PlayerInventory playerInv;
    public GameObject itemUIPrefab;
    public Transform inventoryUIParent;
    public Transform hotbarUIParent;
    public InventorySlotObject[] invSlotObjs;

    public GameObject recipeUIPrefab;
    public Transform recipeListParent;
    public Dictionary<CraftRecipe.CraftingType, List<GameObject>> recipesByType = new Dictionary<CraftRecipe.CraftingType, List<GameObject>>();

    public TextMeshProUGUI recipeOutputName;
    public Image recipeOutputIcon;
    public TextMeshProUGUI recipeOutputFlavorText;
    public GameObject recipeOutputButton;
    public Image recipeOutputButtonIcon;
    public TextMeshProUGUI recipeOutputButtonCount;

    public Sprite emptySprite;

    void Start()
    {
        InitializeInventoryItemUI();
        //InitializeRecipeUI();
        playerInv.LinkUI(this, invSlotObjs);
    }

    public void InitializeInventoryItemUI()
    {
        invSlotObjs = new InventorySlotObject[playerInv.backpackSize + playerInv.hotbarSize];
        for (int i = 0; i < playerInv.backpackSize + playerInv.hotbarSize; i++) {
            GameObject newItemUI;
            if (i < playerInv.hotbarSize) {
                newItemUI = Instantiate(itemUIPrefab, hotbarUIParent);
            } else {
                newItemUI = Instantiate(itemUIPrefab, inventoryUIParent);
            }
            invSlotObjs[i] = newItemUI.GetComponentInChildren<InventorySlotObject>();
            invSlotObjs[i].UnhighlightSlot();
        }

        HighlightInvSlot(0);
    }

    public void HighlightInvSlot(int index) {
        invSlotObjs[index].HighlightSlot();
    }

    public void UnhighlightInvSlot(int index) {
        invSlotObjs[index].UnhighlightSlot();
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
        RecipePanel recipePanel = recipePanelObject.GetComponent<RecipePanel>();
        recipePanel.InitializeRecipe(recipe);
        recipePanelObject.transform.parent = recipeListParent;
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
    }

    void ShowRecipeOutput(CraftRecipe recipe) {
        recipeOutputName.text = recipe.outputItem.name;
        recipeOutputIcon.sprite = recipe.outputItem.icon;
        recipeOutputFlavorText.text = "";
        recipeOutputButton.SetActive(true);
        recipeOutputButtonIcon.sprite = recipe.outputItem.icon;
        recipeOutputButtonCount.text = recipe.outputCount + "";
    }

    void HideRecipeOutput() {
        recipeOutputName.text = "";
        recipeOutputIcon.sprite = emptySprite;
        recipeOutputFlavorText.text = "";
        recipeOutputButton.SetActive(false);
        recipeOutputButtonIcon.sprite = emptySprite;
        recipeOutputButtonCount.text = "";
    }
}
