using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public abstract class Item : ScriptableObject {
    public string name = "Leftover code";
    public Sprite icon;
    public string flavorText = "Oops! How'd you find this?";
    public float colliderSize = 1;
    public int maxStackSize = 999;
    public bool consumeOnUse = true;
    public float lightStrength = 0f;
    public Color lightColor;

    public abstract int Use();
}

public class ItemObject {
    public ItemObject(string name, int currentStack) {
        this.name = name;
        this.currentStack = currentStack;
    }

    T CastItem<T> (object input) {
        return (T) input;
    }

    public void Use() {
        Item thisItem = ItemManager.GetItem(this.name);
        //Type itemType = thisItem.GetType();
        if (this.currentStack <= 0) {
            return;
        }
        if (thisItem.Use() == -1) {
            return;
        }
        if (thisItem.consumeOnUse) {
            this.currentStack--;
        }
    }
    
    public string name;
    public int currentStack;
}

[Serializable] 
[CreateAssetMenu(fileName = "New IOItem", menuName = "Items/IOItem")]
public class IOItem : ScriptableObject {
    public IOItem(Item item, int count) {
        this.item = item;
        this.count = count;
    }

    public Item item;
    public int count;
}

[Serializable]
public class LightItem : BlockItem {
    public float lightStrength = 1f;
    
    new public int Use() {
        return WorldModifier.Instance.PlaceTile(blockID);
    }
}