using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingOutput : MonoBehaviour
{
    public Image itemIcon;
    public Text itemCount;

    public GameObject blocker;

    public Sprite defaultSprite;

    private CraftRecipe outputRecipe;

    void Start()
    {
        if (defaultSprite == null) {
            defaultSprite = itemIcon.sprite;
        }
    }

    public ItemObject TakeCraftedItem(ItemObject currentItem) {
        if (currentItem != null) {
            if (currentItem.name != outputRecipe.outputItem.name) {
                return currentItem;
            }
            if (currentItem.currentStack + outputRecipe.outputCount > outputRecipe.outputItem.maxStackSize) {
                return currentItem;
            }
        }
        ItemObject takenItem = CraftingController.Instance.TryCraft(outputRecipe);

        if (takenItem == null) {
            return currentItem;
        }

        if (currentItem != null) {
            takenItem.currentStack += currentItem.currentStack;
        }

        return takenItem; 
    }

    public void UpdateCraftRecipe(CraftRecipe newRecipe) {
        if (newRecipe == null) {
            HideRecipeVisuals();
            return;
        }

        outputRecipe = newRecipe;
        UpdateItemIcon(outputRecipe.outputItem, outputRecipe.outputCount);
    }

    public void UpdateItemIcon(Item newItem, int newCount) {
        if (newItem == null) {
            HideRecipeVisuals();
        } 
        
        else if (newCount <= 0) {
            HideRecipeVisuals();
        }

        else {
            itemIcon.sprite = newItem.icon;
            itemCount.text = Helpers.AdjustCount(newCount);
            itemIcon.enabled = true;
        }
    }

    public void HideRecipeVisuals() {
        itemIcon.sprite = defaultSprite;
        itemCount.text = "";
    }

    public void BlockCrafting() {
        blocker.SetActive(true);
    }

    public void EnableCrafting() {
        blocker.SetActive(false);
    }
}
