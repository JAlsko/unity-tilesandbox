using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldController))]
public class WorldModifier : Singleton<WorldModifier>
{
    private WorldController wCon;

    void Start()
    {
        wCon = GetComponent<WorldController>();
    }

    //Public tile remover function
    public int RemoveTile(int x, int y) {
        int removedBlockID = wCon.RemoveTile(x, y);
        if (removedBlockID != -1) {
            ItemManager.SpawnDroppedBlock(removedBlockID, x, y, Vector2.one*300f);
        }

        return removedBlockID;
    }

    //Public tile addition function
    public int AddTile(int x, int y, int newTile) {
        int addTileResult = wCon.AddTile(x, y, newTile);
        return addTileResult;
    }

    public int AddTile(int newTile) {
        Vector2 mousePos = PlayerHandler.Instance.GetMainPlayerMousePos();
        return AddTile((int)mousePos.x, (int)mousePos.y, newTile);
    }

    public int RemoveTile() {
        Vector2 mousePos = PlayerHandler.Instance.GetMainPlayerMousePos();
        return RemoveTile((int)mousePos.x, (int)mousePos.y);
    }
}
