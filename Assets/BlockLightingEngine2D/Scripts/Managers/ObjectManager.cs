using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles ghosting and frame execution of the selected tool.
/// </summary>
public class ObjectManager : Singleton<ObjectManager>
{
    public bool isGhosting = false;
    public bool toolIsActive = false;
    public Color ghostColor, ghostColorBlocked;
    public SpriteRenderer ghost;
    public List<ScriptableObject> scriptableObjects;

    private ItemData itemData;


    /// <summary>
    /// Enable ghost dragging for the given item.
    /// </summary>
    /// <param name="data"></param>
    public void GhostTile(ItemData data)
    {
        if (data == null)
            return;

        Unghost();
        itemData = data;
        ghost.sprite = itemData.itemSprite;
        ghost.color = ghostColor;
        isGhosting = true;
    }

    /// <summary>
    /// Enable ghost dragging for the given item.
    /// </summary>
    /// <param name="data"></param>
    public void GhostTile(Sprite sprite)
    {
        if (sprite == null)
            return;

        Unghost();
        itemData = null;
        ghost.sprite = sprite;
        ghost.color = Color.white;
        isGhosting = true;
    }

    /// <summary>
    /// Stops ghosting.
    /// </summary>
    public void Unghost()
    {
        ghost.sprite = null;
        isGhosting = false;
    }

    /// <summary>
    /// Determines whether a ghost can be placed or not, depending on its position
    /// within world bounds or whether the location is blocked with other objects or
    /// tiles etcetera.
    /// </summary>
    /// <returns></returns>
    private bool GhostCanBePlaced(bool shiftIsPressed = false, bool checkForObjects = true)
    {
        if (ghost.sprite == null)
            return false;

        Vector3Int ghostPosition = new Vector3Int(
            (int)ghost.transform.position.x, 
            (int)ghost.transform.position.y, 0);

        // Loop through sprite bounds positions
        for (int v = 0; v < ghost.sprite.bounds.max.y; v++)
        {
            for (int h = 0; h < ghost.sprite.bounds.max.x; h++)
            {
                Vector3Int ghostRelativePosition = new Vector3Int(
                    ghostPosition.x + h, ghostPosition.y + v, 0);

                if ((ghostRelativePosition.x < 1 ||
                    ghostRelativePosition.x >= GenerationManager.Instance.worldWidth - 1) ||
                    (ghostRelativePosition.y < 1 ||
                    ghostRelativePosition.y >= GenerationManager.Instance.worldHeight - 1))
                    return false;

                if (GenerationManager.Instance.tileMapBlocksBack.GetTile(ghostRelativePosition))
                {
                    if (shiftIsPressed)
                        return false;
                    else
                    {
                        if (checkForObjects)
                        {
                            if (LightingManager.Instance.GetLightSource(ghostRelativePosition) != null)
                                return false;
                        }
                        if (GenerationManager.Instance.tileMapBlocksFront.GetTile(ghostRelativePosition))
                            return false;
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Places a tile at the current ghosting position.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlaceTile(Tilemap map)
    {
        // Cancel if out of bounds
        Vector3Int tilePosition = new Vector3Int((int)ghost.transform.position.x, (int)ghost.transform.position.y, 0);
        if ((tilePosition.x < 0 || tilePosition.x >= GenerationManager.Instance.worldWidth) ||
            (tilePosition.y < 0 || tilePosition.y >= GenerationManager.Instance.worldHeight))
            yield break;

        // Add tile
        map.SetTile(tilePosition, itemData.itemTile);
        map.SetColor(tilePosition, Color.black);
        if (map == GenerationManager.Instance.tileMapBlocksFront)
        {
            GenerationManager.Instance.tileMapBlocksBack.SetTile(tilePosition, itemData.itemTile);
            GenerationManager.Instance.tileMapBlocksBack.SetColor(tilePosition, Color.black);
        }

        // Mark the possible air blocks around it as light sources
        if (GenerationManager.Instance.IsAirBlock(tilePosition + Vector3Int.right))
            if (!LightingManager.Instance.GetLightSource(tilePosition + Vector3Int.right))
                LightingManager.Instance.CreateLightSource(tilePosition + Vector3Int.right,
                    LightingManager.Instance.ambientLightColor, 1f);
        if (GenerationManager.Instance.IsAirBlock(tilePosition + Vector3Int.up))
            if (!LightingManager.Instance.GetLightSource(tilePosition + Vector3Int.up))
                LightingManager.Instance.CreateLightSource(tilePosition + Vector3Int.up,
                    LightingManager.Instance.ambientLightColor, 1f);
        if (GenerationManager.Instance.IsAirBlock(tilePosition + Vector3Int.left))
            if (!LightingManager.Instance.GetLightSource(tilePosition + Vector3Int.left))
                LightingManager.Instance.CreateLightSource(tilePosition + Vector3Int.left,
                    LightingManager.Instance.ambientLightColor, 1f);
        if (GenerationManager.Instance.IsAirBlock(tilePosition + Vector3Int.down))
            if (!LightingManager.Instance.GetLightSource(tilePosition + Vector3Int.down))
                LightingManager.Instance.CreateLightSource(tilePosition + Vector3Int.down,
                    LightingManager.Instance.ambientLightColor, 1f);

        /* By placing a tile we want to darken its surroundings realistically. Get/place a LightSource
         * at the current position and simulate light removal as if it was a torch that we took away. */
        LightSource existingLight = LightingManager.Instance.GetLightSource(tilePosition);
        if (existingLight == null)
            existingLight = LightingManager.Instance.CreateLightSource(
                tilePosition, LightingManager.Instance.LightColors[tilePosition.x, tilePosition.y], 1f, false);

        LightingManager.Instance.RemoveLight(existingLight);
        existingLight.gameObject.SetActive(false);
        Destroy(existingLight.gameObject);
    }


    /// <summary>
    /// Removes a tile at the current mouse position.
    /// </summary>
    /// <param name="map"></param>
    public void RemoveTile(Tilemap map)
    {
        Vector3Int tilePosition = GetTilePositionAtMouse();
        if ((tilePosition.x < 0 || tilePosition.x >= GenerationManager.Instance.worldWidth) ||
            (tilePosition.y < 0 || tilePosition.y >= GenerationManager.Instance.worldHeight))
            return;
        if ((map == GenerationManager.Instance.tileMapBlocksBack &&
            GenerationManager.Instance.tileMapBlocksFront.GetTile(tilePosition)) || !map.GetTile(tilePosition))
            return;

        map.SetTile(tilePosition, null);

        // If this became an air block, create a new ambient light source
        if (!GenerationManager.Instance.tileMapBlocksBack.GetTile(tilePosition))
        {
            if (!LightingManager.Instance.GetLightSource(tilePosition))
            {
                LightingManager.Instance.CreateLightSource(
                    tilePosition, LightingManager.Instance.ambientLightColor, 1f);
            }
        }
        // Otherwise I removed a front tile. Make it brighter with a temporary light source and remove the light's object
        else
        {
            Color currentColor = LightingManager.Instance.LightColors[tilePosition.x, tilePosition.y];
            if (currentColor != Color.black && currentColor != Color.clear)
            {
                // The amount of brightness to add is the difference between falloffs!
                float colorIncrement = LightingManager.Instance.LightFalloff - LightingManager.Instance.LightFalloffBack;
                currentColor = new Color(
                    Mathf.Clamp(currentColor.r + colorIncrement, 0f, 1f),
                    Mathf.Clamp(currentColor.g + colorIncrement, 0f, 1f),
                    Mathf.Clamp(currentColor.b + colorIncrement, 0f, 1f));

                GenerationManager.Instance.tileMapBlocksFront.SetColor(tilePosition, currentColor);
                GenerationManager.Instance.tileMapBlocksBack.SetColor(tilePosition, new Color(
                    Mathf.Clamp(currentColor.r * LightingManager.Instance.backLayerShadowFactor, 0f, 1f),
                    Mathf.Clamp(currentColor.g * LightingManager.Instance.backLayerShadowFactor, 0f, 1f),
                    Mathf.Clamp(currentColor.b * LightingManager.Instance.backLayerShadowFactor, 0f, 1f)));

                // Create a light source to update light values, then remove it
                LightSource source = LightingManager.Instance.CreateLightSource(
                    tilePosition, currentColor, 1f);
                source.gameObject.SetActive(false);
                Destroy(source.gameObject);
            }
        }
    }

    /// <summary>
    /// Converts mouse position to tile position and returns it.
    /// </summary>
    /// <returns></returns>
    public Vector3Int GetTilePositionAtMouse()
    {
        Vector3 rawPosition = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 50));
        return new Vector3Int((int)rawPosition.x, (int)rawPosition.y, 0);
    }


    /// <summary>
    /// Execute frame code for the currently active tool.
    /// </summary>
    /// <param name="tool"></param>
    public void ExecuteTool(ToolDetails.ToolType tool)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift);
        bool mouseLeft = Input.GetMouseButton(0);
        switch (tool)
        {
            case ToolDetails.ToolType.PLACE_TILE:                
                if (GhostCanBePlaced(shift, false))
                {
                    ghost.color = ghostColor;
                    if (mouseLeft)
                        StartCoroutine(PlaceTile(shift ? GenerationManager.Instance.tileMapBlocksBack
                            : GenerationManager.Instance.tileMapBlocksFront));
                }
                else
                    ghost.color = ghostColorBlocked;
                break;
            case ToolDetails.ToolType.REMOVE_TILE:
                if (mouseLeft)
                    RemoveTile(shift ? GenerationManager.Instance.tileMapBlocksBack
                        : GenerationManager.Instance.tileMapBlocksFront);
                break;
            case ToolDetails.ToolType.PLACE_LIGHT:
                if (GenerationManager.Instance.tileMapBlocksBack.GetTile(GetTilePositionAtMouse()) && GhostCanBePlaced())
                {
                    ghost.color = ghostColor;
                    if (mouseLeft)
                    {
                        // Use a random color for the lights
                        Color lightColor = new Color(
                                UnityEngine.Random.Range(0f, 1f),
                                UnityEngine.Random.Range(0f, 1f),
                                UnityEngine.Random.Range(0f, 1f));
                        LightSource newSource = LightingManager.Instance.CreateLightSource(
                            GetTilePositionAtMouse(), lightColor, 1f);

                        // Make the light's sprite visible (by default invisible for ambient lights)
                        newSource.GetComponent<SpriteRenderer>().enabled = true;
                    }
                }
                else
                    ghost.color = ghostColorBlocked;
                break;
            case ToolDetails.ToolType.REMOVE_LIGHT:
                if (mouseLeft)
                {
                    LightSource lightSource = LightingManager.Instance.GetLightSource(
                        GetTilePositionAtMouse());

                    /* Only target lights that were placed by the player. Ambient lights can only be removed
                     * by placing a block on top of them and is handled in PlaceTile();. */
                    if (lightSource != null && lightSource.lightColor != LightingManager.Instance.ambientLightColor)
                    {
                        LightingManager.Instance.RemoveLight(lightSource);
                        lightSource.gameObject.SetActive(false);
                        Destroy(lightSource.gameObject);
                    }
                }
                break;
            default:
                break;
        }
    }


    private void Update()
    {
        // Move ghost with the mouse (also compensate for sprite origin)
        Vector3Int mousePosition = GetTilePositionAtMouse();
        ghost.transform.position = new Vector3(
            mousePosition.x + (ghost.sprite != null ? ghost.sprite.pivot.x / ghost.sprite.pixelsPerUnit : 0f),
            mousePosition.y + (ghost.sprite != null ? ghost.sprite.pivot.y / ghost.sprite.pixelsPerUnit : 0f),
            mousePosition.z);

        // Toggle tool on / off depending on UI mouse over
        if (EventSystem.current.IsPointerOverGameObject())
        {
            toolIsActive = false;
            if (isGhosting)
                Unghost();
        }
        else if (!toolIsActive && UserInterface.Instance.SelectedTool != null)
        {
            UserInterface.Instance.SelectedTool.SelectTool();
            toolIsActive = true;
        }

        // Run specific tool
        if (toolIsActive)
            ExecuteTool(UserInterface.Instance.SelectedTool.tool);
    }
}