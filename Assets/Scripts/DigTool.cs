﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "New Dig Tool", menuName = "Items/Dig Tool")]
public class DigTool : Item {
    public float digAmount = 3.5f;
    public int digToolTier = 0;
    public float fireRate = .5f;
    public bool fullAuto = true;

    override public string Use() {
        return TileController.Instance.DigTile(this.digAmount);
    }
}