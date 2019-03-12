using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "New Block Item", menuName = "Items/Block")]
public class BlockItem : Item {
    public int blockID;

    new public int Use() {
        return WorldModifier.Instance.AddTile(blockID);
    }
}
