using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipePanel : MonoBehaviour
{
    [HideInInspector] public CraftRecipe recipe;

    public Image outputIcon;
    public TextMeshProUGUI outputCount;

    public Transform ingredientsParent;
    private List<IngredientPanel> ingredientsPanels = new List<IngredientPanel>();

    public GameObject recipeBlocker;
    
    void Start()
    {
    
    }

    public void InitializeRecipe(CraftRecipe newRecipe) {
        recipe = newRecipe;

        if (newRecipe == null) {
            Debug.Log("Initializing null recipe!");
        }

        int i = 0;
        foreach (Transform child in ingredientsParent) {
            ingredientsPanels.Add(child.GetComponent<IngredientPanel>());
            ingredientsPanels[i].HideIngredientPanel();
            i++;
        }

        outputIcon.sprite = recipe.outputItem.icon;
        outputCount.text = Helpers.AdjustCount(newRecipe.outputCount);

        for (i = 0; i < recipe.ingredientItems.Count && i < ingredientsPanels.Count; i++) {
            ingredientsPanels[i].ingredientIcon.sprite = recipe.ingredientItems[i].icon;
            ingredientsPanels[i].ingredientCount.text = Helpers.AdjustCount(recipe.ingredientCounts[i]);
            ingredientsPanels[i].ShowIngredientPanel();
        }
    }

    public void FocusRecipe() {
        UIController.Instance.FocusCraftingRecipe(this);
    }
}
