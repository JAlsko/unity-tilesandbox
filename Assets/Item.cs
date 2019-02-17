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
}

public class ItemObject: Item {
    public ItemObject(int id, int currentStack) {
        this.id = id;
        this.currentStack = currentStack;
    }
    
    public int currentStack;
}

[Serializable]
public class BlockItem : Item {
    public int tileID;
}

[Serializable]
public class LightItem : BlockItem {
    public float lightStrength = 1f;
}