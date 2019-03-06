using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public abstract class Item {
    public int id = 999;
    public string name = "Leftover code";
    public Sprite icon;
    public float colliderSize;
    public int maxStackSize = 999;
    public bool consumeOnUse = true;
    public float lightStrength = 0f;
    public Color lightColor;

    public abstract int Use();
}

[Serializable]
public class ItemObject {
    public ItemObject(int id, int currentStack) {
        this.id = id;
        this.currentStack = currentStack;
    }

    public void Use() {
        Item thisItem = ItemManager.GetItem(this.id);
        if (thisItem.Use() == -1) {
            return;
        }
        if (thisItem.consumeOnUse) {
            this.currentStack--;
        }
        if (this.currentStack <= 0) {
            
        }
    }
    
    public int id;
    public int currentStack;
}

[Serializable]
public class CraftRecipe {
	public List<ItemObject> ingredients;
	public ItemObject output;
	public int craftingTier;
	
	public CraftRecipe(List<ItemObject> ingredients, ItemObject output, int craftingTier = 0) {
		this.ingredients = ingredients;
		this.output = output;
		this.craftingTier = craftingTier;
	}
}

[Serializable]
public class BlockItem : Item {
    public int blockID;

    override public int Use() {
        return WorldModifier.Instance.AddTile(blockID);
    }
}

[Serializable]
public class LightItem : BlockItem {
    public float lightStrength = 1f;
    
    override public int Use() {
        return WorldModifier.Instance.AddTile(blockID);
    }
}