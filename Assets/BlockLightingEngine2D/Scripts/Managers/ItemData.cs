using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles the data of items as scriptable objects.
/// </summary>
[CreateAssetMenu(menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    public Sprite itemSprite;  
    public Tile itemTile;
}
