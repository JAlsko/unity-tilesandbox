using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Item : ScriptableObject {
    public int id = 999;
    public string name = "Leftover code";
    public Sprite icon;
    public float colliderSize;
    public int maxStackSize = 999;
    public bool consumeOnUse = true;
    public float lightStrength = 0f;
    public Color lightColor;

    public int Use() {
        return 0;
    }
}

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
        return WorldModifier.Instance.AddTile(blockID);
    }
}