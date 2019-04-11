using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[System.Serializable]
[CreateAssetMenu(fileName = "New MultiTile", menuName = "Tiles/MultiTile")]
public class MultiTile : ScriptableObject
{
    public string tileName;
    public Sprite tileSprite;
    public int numTilesWidth = 1;
    public int numTilesHeight = 1;
}
