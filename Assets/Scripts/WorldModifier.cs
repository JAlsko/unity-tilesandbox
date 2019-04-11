using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldController))]
public class WorldModifier : Singleton<WorldModifier>
{
    private WorldController wCon;

    public float maxTileHealth = 10f;
    public float baseDigAmount = 3.5f;
    private float[,] tileHealth;
    private SupportTile[,] supportedTiles;

    private int[,] tilesToHeal;
    private int[,] tilesMarkedToHeal;

    private bool needUpdate = true;

    void Start()
    {
        wCon = GetComponent<WorldController>();
    }

    public void InitializeTileHealth() {
        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();

        tilesToHeal = new int[worldWidth, worldHeight];
        tilesMarkedToHeal = new int[worldWidth, worldHeight];

        tileHealth = new float[worldWidth, worldHeight];

        supportedTiles = new SupportTile[worldWidth, worldHeight];
    }

    bool HealBlocks() {
        bool stillNeedUpdate = false;

        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();
        for (int x = 0; x < worldWidth; x++) {
            for (int y = 0; y < worldHeight; y++) {
                if (tilesToHeal[x, y] == 1) {
                    tilesToHeal[x, y] = 0;
                    tileHealth[x, y] = maxTileHealth;
                }

                if (tilesMarkedToHeal[x, y] == 1) {
                    tilesMarkedToHeal[x, y] = 0;
                    tilesToHeal[x, y] = 1;
                    stillNeedUpdate = true;
                }
            }
        }

        return stillNeedUpdate;
    }

    public int DigTile(int x, int y, float digAmount) {
        float newHealth = tileHealth[x, y] - digAmount;
        if (newHealth <= 0) {
            tileHealth[x, y] = 0;
            return RemoveTile(x, y);
        } else {
            tileHealth[x, y] = newHealth;
            tilesMarkedToHeal[x, y] = 1;
            needUpdate = true;
            return -1;
        }
    }

    private void CheckSupportBlocks(int x, int y, bool removingTile) {
        int direction = -1;
        for (int i = 1; i > -1; i--) {
            for (int j = -1; j < 1; j++) {
                direction++;
                if (Mathf.Abs(i) == Mathf.Abs(j))
                    continue;

                int newX = x + j;
                int newY = y + i;
                if (newX < 0 || newX > supportedTiles.GetUpperBound(0) || newY < 0 || newY > supportedTiles.GetUpperBound(1))
                    continue;

                if (!supportedTiles[newX, newY].active)
                    continue;

                if (removingTile) {
                    if (!supportedTiles[newX, newY].RemoveSide(direction)) {
                        RemoveTile(newX, newY);
                    }
                } else {
                    supportedTiles[newX, newY].AddSide(direction);
                }
            }
        }
    }

    //Public tile remover function
    public int RemoveTile(int x, int y) {
        int removedBlockID = wCon.RemoveTile(x, y);
        if (removedBlockID != -1) {
            string dropItem = TileManager.Instance.allTiles[removedBlockID].dropItem.name;
            ItemManager.SpawnDroppedItem(dropItem, x, y, Vector2.one*300f);
            if (supportedTiles[x, y] != null)
                supportedTiles[x, y].active = false;
            else {
                supportedTiles[x, y] = new SupportTile();
                supportedTiles[x, y].active = false;
            }
            //CheckSupportBlocks(x, y, true);
        }

        return removedBlockID;
    }

    //Public tile addition function
    public int PlaceTile(int x, int y, int newTile) {
        int addTileResult = wCon.AddTile(x, y, newTile);

        if (addTileResult != -1) {
            tileHealth[x, y] = maxTileHealth;
            TileInfo newTileInfo = TileManager.Instance.GetTile(newTile);
            supportedTiles[x, y] = newTileInfo.supportTile;
            //CheckSupportBlocks(x, y, false);
        }

        return addTileResult;
    }

    public void InitializeNewTile(int x, int y, int newTile) {
        if (newTile == 0) {
            supportedTiles[x, y] = new SupportTile();
            supportedTiles[x, y].active = false;
            return;
        }
        tileHealth[x, y] = maxTileHealth;
        TileInfo newTileInfo = TileManager.Instance.GetTile(newTile);
        supportedTiles[x, y] = newTileInfo.supportTile;
        //CheckSupportBlocks(x, y, false);
    }

    public int PlaceTile(int newTile) {
        Vector2 mousePos = PlayerHandler.Instance.GetMainPlayerMousePos();
        return PlaceTile((int)mousePos.x, (int)mousePos.y, newTile);
    }

    public int DigTile(float digAmount) {
        Vector2 mousePos = PlayerHandler.Instance.GetMainPlayerMousePos();
        return DigTile((int)mousePos.x, (int)mousePos.y, digAmount);
    }
}