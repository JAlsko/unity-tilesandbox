using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple tile grid item in the UserInterface
/// </summary>
public class TileDetails : MonoBehaviour
{
    public TileType tile;
    public enum TileType
    {
        DIRT,
        STONE
    }

    private Button button;


    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => { SelectTile(); });
    }

    /// <summary>
    /// Clears the selection and ghosting of this tile.
    /// </summary>
    public void DeselectTile()
    {
        GetComponent<Image>().color = Color.white;

        if (ObjectManager.Instance.isGhosting)
            ObjectManager.Instance.Unghost();
    }

    /// <summary>
    /// Selects this tile and enable ghosting.
    /// </summary>
    public void SelectTile()
    {
        if (UserInterface.Instance.SelectedTile != null)
            UserInterface.Instance.SelectedTile.DeselectTile();
        UserInterface.Instance.SelectedTile = this;

        GetComponent<Image>().color = Color.green;
        ObjectManager.Instance.GhostTile(ObjectManager.Instance.scriptableObjects[(int)tile] as ItemData);
    }
}
