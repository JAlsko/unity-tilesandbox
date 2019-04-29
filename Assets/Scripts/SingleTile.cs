using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
[CreateAssetMenu(fileName = "New Tile", menuName = "Tiles/Single Tile")]
public class SingleTile: ScriptableObject {
    public new string name;
    public TileBase tileBase;
    public float lightStrength;
    public Color lightColor = Color.white;
    public float maxTileHealth = 10f;
    public int digToolTier = 0;
    public Item dropItem;

    public bool requiresSupport = false;
    public bool takesAnySupport = false;
    public bool needsTopSupport, needsLeftSupport, needsRightSupport, needsBottomSupport, needsBackSupport = false;
    public bool givesTopSupport, givesLeftSupport, givesRightSupport, givesBottomSupport = false;
}