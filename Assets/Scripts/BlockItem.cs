using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "New Block Item", menuName = "Items/Block")]
public class BlockItem : Item {
    public string blockID;
    public bool isMultiTile = false;

    override public string Use() {
        string adjustedBlockID = isMultiTile ? "_" + blockID : blockID;
        return TileController.Instance.PlaceTile(adjustedBlockID, isMultiTile);
    }
}
