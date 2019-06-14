using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[System.Serializable]
[CreateAssetMenu(fileName = "New ObjectTile", menuName = "Tiles/ObjectTile")]
public class ObjectTile : ScriptableObject
{
    public string tileName;
    public Sprite tileSprite;
    public int numTilesWidth = 1;
    public int numTilesHeight = 1;

    public bool requiresSupport = false;
    public bool takesAnySupport = false;
    public bool needsTopSupport, needsLeftSupport, needsRightSupport, needsBottomSupport, needsBackSupport = false;
    public bool givesTopSupport, givesLeftSupport, givesRightSupport, givesBottomSupport = false;
}
