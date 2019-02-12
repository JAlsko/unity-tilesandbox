using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple tool grid item in the UserInterface
/// </summary>
public class ToolDetails : MonoBehaviour
{
    public ToolType tool;
    public enum ToolType
    {
        PLACE_TILE,
        REMOVE_TILE,
        PLACE_LIGHT,        
        REMOVE_LIGHT
    }

    private Button button;


	private void Awake ()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => { SelectTool(); });
    }

    /// <summary>
    /// Clears the selection and ghosting of this tool.
    /// </summary>
    public void DeselectTool()
    {
        GetComponent<Image>().color = Color.white;

        if (ObjectManager.Instance.isGhosting)
            ObjectManager.Instance.Unghost();
    }

    /// <summary>
    /// Selects this tool and enable ghosting.
    /// </summary>
    public void SelectTool()
    {
        if (UserInterface.Instance.SelectedTool != null)
            UserInterface.Instance.SelectedTool.DeselectTool();
        UserInterface.Instance.SelectedTool = this;

        GetComponent<Image>().color = Color.green;
        UserInterface.Instance.tileSelectRoot.SetActive(false);        
        switch (tool)
        {
            case ToolType.PLACE_TILE:
                UserInterface.Instance.tileSelectRoot.SetActive(true);
                if (UserInterface.Instance.SelectedTile == null)
                    UserInterface.Instance.SelectedTile = 
                        UserInterface.Instance.tileSelectRoot.GetComponentInChildren<TileDetails>();
                UserInterface.Instance.SelectedTile.SelectTile();
                break;
            case ToolType.REMOVE_TILE:
                break;
            case ToolType.PLACE_LIGHT:
                ObjectManager.Instance.GhostTile(UserInterface.Instance.lightSpriteAdd);
                break;
            case ToolType.REMOVE_LIGHT:
                ObjectManager.Instance.GhostTile(UserInterface.Instance.lightSpriteRemove);
                break;
            default:
                break;
        }
    }
}
