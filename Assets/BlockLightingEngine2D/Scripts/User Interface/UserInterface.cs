using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInterface : Singleton<UserInterface>
{
    public GameObject tileSelectRoot, messageBox,
        gridToolRoot, gridTileRoot;
    public Sprite lightSpriteAdd, lightSpriteRemove;
    public ToolDetails SelectedTool { get; set; }
    public TileDetails SelectedTile { get; set; }
    public List<ToolDetails> Tools { get; private set; }
    public List<TileDetails> Tiles { get; private set; }

    private int selectedToolIndex, selectedTileIndex;


    private void Start()
    {
        // Add all tools and tiles to lists
        Tools = new List<ToolDetails>();
        Tiles = new List<TileDetails>();
        foreach (Transform transform in gridToolRoot.transform)
        {
            ToolDetails tool = transform.GetComponent<ToolDetails>();
            if (tool != null)
                Tools.Add(tool);
        }
        foreach (Transform transform in gridTileRoot.transform)
        {
            TileDetails tile = transform.GetComponent<TileDetails>();
            if (tile != null)
                Tiles.Add(tile);
        }
        SelectedTool = Tools[0];
        SelectedTile = Tiles[0];
    }

    private void Update()
    {
        float scrollwheelInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollwheelInput != 0f)
        {
            if (!Input.GetKey(KeyCode.LeftShift))
                ScrollToTool(scrollwheelInput < 0f ? selectedToolIndex + 1 : selectedToolIndex - 1);
            else if (tileSelectRoot.activeSelf)
                ScrollToTile(scrollwheelInput < 0f ? selectedTileIndex + 1 : selectedTileIndex - 1);
        }
    }

    /// <summary>
    /// Deselects the current tool and selects the next tool.
    /// </summary>
    /// <param name="toolIndex"></param>
    public void ScrollToTool(int toolIndex)
    {
        if (toolIndex < 0)
            toolIndex = Tools.Count - 1;
        else if (toolIndex > Tools.Count - 1)
            toolIndex = 0;
        selectedToolIndex = toolIndex;
        Tools[selectedToolIndex].SelectTool();
    }

    /// <summary>
    /// Deselects the current tile and selects the next tile.
    /// </summary>
    /// <param name="tileIndex"></param>
    private void ScrollToTile(int tileIndex)
    {
        if (tileIndex < 0)
            tileIndex = Tiles.Count - 1;
        else if (tileIndex > Tiles.Count - 1)
            tileIndex = 0;
        selectedTileIndex = tileIndex;
        Tiles[selectedTileIndex].SelectTile();
    }

    /// <summary>
    /// Clears the entire world from tiles, LightSources, everything.
    /// Also resets data structures to be ready for the next generation.
    /// </summary>
    public void ButtonClear()
    {
        LightingManager.Instance.Clear();
        GenerationManager.Instance.Clear();        
    }

    /// <summary>
    /// Does the same as ButtonClear, but waits a frame after execution.
    /// 
    /// Only used combined with ButtonGenerate, since artifacts can be seen
    /// if clearing and generating is done in the same frame (I suspect Unity's
    /// tile system updates).
    /// 
    /// ButtonClear and ButtonGenerate can't be coroutines since they are linked
    /// with the standard Unity button events for simplicity. That's why we use
    /// an Action callback in here to "fake" a frame pause in the ButtonGenerate
    /// void function.
    /// </summary>
    /// <param name="done"></param>
    /// <returns></returns>
    public IEnumerator ButtonClearSmooth(Action<bool> done)
    {
        LightingManager.Instance.Clear();
        GenerationManager.Instance.Clear();

        /* Let's use the frame pause to notify you that we're about to generate
         * because why not? */
        messageBox.SetActive(true);

        yield return null;
        done(true);
    }

    /// <summary>
    /// Generates some simple terrain to test lighting with.
    /// </summary>
    public void ButtonGenerate()
    {
        // Clear all remaining data first
        StartCoroutine(ButtonClearSmooth(done =>
        {
            GenerationManager.Instance.GenerateTerrain();
            messageBox.SetActive(false);
        }));        
    }

    /// <summary>
    /// Randomize the seed in the game.
    /// </summary>
    public void ButtonSeed()
    {
        GenerationManager.Instance.SetSeed();
    }
}