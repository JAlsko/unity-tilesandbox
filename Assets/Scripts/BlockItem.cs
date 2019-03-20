using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "New Block Item", menuName = "Items/Block")]
public class BlockItem : Item {
    public int blockID;

    override public int Use() {
        return WorldModifier.Instance.PlaceTile(blockID);
    }
}
