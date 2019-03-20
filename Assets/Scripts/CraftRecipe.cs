using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Craft Recipe")]
public class CraftRecipe : ScriptableObject {
	public enum CraftingType {
		basic = 0,
		workbench = 1,
		smelting = 2,
		anvil = 3
	}
	
	public List<Item> ingredientItems;
	public List<int> ingredientCounts;
	public Item outputItem;
    public int outputCount;
	public CraftingType craftingType;
	public int craftingTier;

    public int initialized = 0;
	
    public void Awake() {
        if (initialized == 0) {
            initialized = 1;
            ingredientItems = new List<Item>();
            ingredientCounts = new List<int>();
        }
    }

	public CraftRecipe(List<Item> ingredientItems = null, List<int> ingredientCounts = null, Item outputItem = null, int outputCount = 1, CraftingType craftingType = CraftingType.basic, int craftingTier = 0) {
		this.ingredientItems = ingredientItems;
		this.ingredientCounts = ingredientCounts;
		this.outputItem = outputItem;
		this.outputCount = outputCount;
		this.craftingType = craftingType;
		this.craftingTier = craftingTier;
	}
}