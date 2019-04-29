using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingController : Singleton<CraftingController>
{
    public PlayerInventory linkedInv;

    void Start()
    {
        
    }

    public void LinkInventory(PlayerInventory pInv) {
        linkedInv = pInv;
    }

    public ItemObject TryCraft(CraftRecipe recipe) {
        for (int i = 0; i < recipe.ingredientItems.Count; i++) {
            if (linkedInv.GetItemCount(recipe.ingredientItems[i]) < recipe.ingredientCounts[i]) {
                return null;
            }
        }

        for (int i = 0; i < recipe.ingredientItems.Count; i++) {
            linkedInv.RemoveItemStack(recipe.ingredientItems[i], recipe.ingredientCounts[i]);
        }

        return new ItemObject(recipe.outputItem.name, recipe.outputCount);
    }
}
