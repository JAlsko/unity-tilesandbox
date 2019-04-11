using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[System.Serializable]
public class MultiTileObject {
    public string name = "New MultiTile";
    public List<Sprite> spriteSections = new List<Sprite>();
    public Tile[] tileBlock;
    public int tileWidth = 1;
    public int tileHeight = 1;

    public void InitializeMultiTile(MultiTile mTile) {
        name = mTile.tileName;
        tileWidth = mTile.numTilesWidth;
        tileHeight = mTile.numTilesHeight;

        spriteSections = new List<Sprite>();

        string assetPath = AssetDatabase.GetAssetPath(mTile.tileSprite);
        Object[] loadedAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in loadedAssets) {
            //Debug.Log("Trying to load " + asset.name);
            var spriteCast = asset as Sprite;

            if (spriteCast != null)
            {
                spriteSections.Add(spriteCast);
            }
            else
            {
                continue;
            }
            //Debug.Log("Successfully loaded " + spriteSections[spriteSections.Count-1].name);
        }

        tileBlock = new Tile[spriteSections.Count];

        for (int i = 0; i < spriteSections.Count; i++) {
            Tile newTile = (Tile)ScriptableObject.CreateInstance("Tile");
            newTile.sprite = spriteSections[i];
            tileBlock[i] = newTile;
        }
    }
}

public class MultiTileManager : Singleton<MultiTileManager>
{   
    public List<MultiTileObject> allmts = new List<MultiTileObject>();
    public Dictionary<string, MultiTileObject> allMultiTiles = new Dictionary<string, MultiTileObject>();

    public int curMultiTileID = 1;
    public int[,] multiTileWorld;

    public void InitializeAllMultiTiles() {
        Resources.LoadAll("Tiles/");
        MultiTile[] foundTiles = (MultiTile[]) Resources.FindObjectsOfTypeAll(typeof(MultiTile));
        foreach (MultiTile mt in foundTiles) {
            MultiTileObject mto = new MultiTileObject();
            mto.InitializeMultiTile(mt);
            allmts.Add(mto);
            allMultiTiles[mt.name] = mto;
            //Debug.Log("Loaded item " + i.name);
        }

        multiTileWorld = new int[WorldController.GetWorldWidth(), WorldController.GetWorldHeight()];
    }

    public void PlaceMultiTile(string tileToPlace, int x, int y) {
        MultiTileObject mto = GetMultiTileObject(tileToPlace);
        int width = mto.tileWidth;
        int height = mto.tileHeight;

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (i + x >= WorldController.GetWorldWidth() || j + y >= WorldController.GetWorldHeight())
                    continue;
                
                multiTileWorld[i + x, j + y] = curMultiTileID;
                WorldController.Instance.AddTile(i + x, j + y, 1);
            }
        }

        curMultiTileID++;
    }

    public void RemoveMultiTile(int x, int y) {
        if (multiTileWorld[x, y] == 0)
            return;

        int removingID = multiTileWorld[x, y];

        Vector2Int startPos = FindMultiTileStart(x, y);

        bool reachedEndX = false;
        bool reachedEndY = false;
        for (int i = 0; !reachedEndX; i++) {
            int realX = i + x;
            if (realX > multiTileWorld.GetUpperBound(0)) {
                reachedEndX = true;
            } 

            for (int j = 0; !reachedEndY; j++) {
                int realY = j + y;
                if (realX > multiTileWorld.GetUpperBound(0)-1) {
                    if (multiTileWorld[realX+1, realY] != removingID)
                        reachedEndX = true;
                }
                if (realY > multiTileWorld.GetUpperBound(1)-1) {
                    if (multiTileWorld[realX, realY+1] != removingID)
                        reachedEndY = true;
                }
                if (realY > multiTileWorld.GetUpperBound(1)) {
                    reachedEndY = true;
                }

                multiTileWorld[realX, realY] = 0;
                WorldController.Instance.RemoveTile(realX, realY);
            }
        }
    }

    Vector2Int FindMultiTileStart(int x, int y) {
        int removingID = multiTileWorld[x, y];
        
        Vector2Int startPos = new Vector2Int();
        bool foundStartX = false; //if we get to world coordinates (1, 1) and still haven't found the start of the multiblock, default to 0
        bool foundStartY = false;
        for (int i = x; i > 1; i--) {
            if (multiTileWorld[i-1, y] != removingID) {
                startPos.x = i;
                foundStartX = true;
            }
        } 
        
        if (!foundStartX)
            startPos.x = 0;
        
        for (int j = y; j > 1; j--) {
             if (multiTileWorld[x, j-1] != removingID) {
                startPos.x = j;
                foundStartY = true;
            }   
        }

        if (!foundStartY)
            startPos.y = 0;

        return startPos;
    }

    public MultiTileObject GetMultiTileObject(string multiTileName) {
        return allMultiTiles[multiTileName];
    }
}
