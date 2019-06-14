using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "New Liquid Item", menuName = "Items/Liquid")]
public class LiquidItem : Item {
    public string liquidID;
    public float liquidAmount;

    override public string Use() {
        LiquidController.Instance.SpawnLiquidBlock(liquidAmount);
        return "nullitem";
    }
}
