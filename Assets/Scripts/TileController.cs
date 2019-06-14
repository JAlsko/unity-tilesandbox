using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public enum Direction {
    up = 0,
    left = 1,
    right = 2,
    down = 3,
    back = 4
}

public class SupportGiver {
	public bool[] supportedSides;
	public SupportGiver(bool[] sidesToSupport) {
		supportedSides = sidesToSupport;
	}
    public SupportGiver(SupportGiver toCopy) {
        this.supportedSides = toCopy.supportedSides;
    }
	public SupportGiver(bool defaultInitialization) {
		supportedSides = new bool[5];	
	}
    public void UpdateSupportGiver(SupportGiver toCopy) {
        this.supportedSides = toCopy.supportedSides;
    }
}

public class SupportPoint {
	public bool takesAnySupport;
	public bool[] neededSupport;
	public SupportPoint(bool _takesAnySupport, bool[] _neededSupport) {
		takesAnySupport = _takesAnySupport;
		neededSupport = _neededSupport;
	}
    public SupportPoint(SupportPoint toCopy) {
        this.takesAnySupport = toCopy.takesAnySupport;
        this.neededSupport = toCopy.neededSupport;
    }
	public SupportPoint(bool defaultInitialization) {
		takesAnySupport = false;
		neededSupport = new bool[5];
	}
    public void UpdateSupportPoint(SupportPoint toCopy) {
        this.takesAnySupport = toCopy.takesAnySupport;
        this.neededSupport = toCopy.neededSupport;
    }
}

[System.Serializable]
public class SingleTileObject {
    public new string name;
    public TileBase tileBase;
    public float lightStrength;
    public Color lightColor = Color.white;
    public float maxTileHealth = 10f;
    public int digToolTier = 0;
    public Item dropItem;

    public SupportGiver supportGiver;
    public SupportPoint supportPoint;

    public void InitializeSingleTile(SingleTile sTile) {
        name = sTile.name;
        tileBase = sTile.tileBase;
        lightStrength = sTile.lightStrength;
        lightColor = sTile.lightColor;
        maxTileHealth = sTile.maxTileHealth;
        digToolTier = sTile.digToolTier;
        dropItem = sTile.dropItem;

        bool[] neededSupport = new bool[5];
        bool[] givenSupport = new bool[5];
        
        neededSupport[(int)Direction.up] = sTile.needsTopSupport;
        neededSupport[(int)Direction.left] = sTile.needsLeftSupport;
        neededSupport[(int)Direction.right] = sTile.needsRightSupport;
        neededSupport[(int)Direction.down] = sTile.needsBottomSupport;
        neededSupport[(int)Direction.back] = sTile.needsBackSupport;

        givenSupport[(int)Direction.up] = sTile.givesTopSupport;
        givenSupport[(int)Direction.left] = sTile.givesLeftSupport;
        givenSupport[(int)Direction.right] = sTile.givesRightSupport;
        givenSupport[(int)Direction.down] = sTile.givesBottomSupport;

        supportGiver = new SupportGiver(givenSupport);
        supportPoint = new SupportPoint(sTile.takesAnySupport, neededSupport);
    }
}

[System.Serializable]
public class MultiTileObject {
    public string name = "New MultiTile";
    public List<Sprite> spriteSections = new List<Sprite>();
    public Tile[,] tileBlock;
    public int tileWidth = 1;
    public int tileHeight = 1;

    public SupportGiver supportGiver;
    public SupportPoint supportPoint;

    public void InitializeMultiTile(MultiTile mTile) {
        name = mTile.tileName;
        tileWidth = mTile.numTilesWidth;
        tileHeight = mTile.numTilesHeight;

        spriteSections = new List<Sprite>();

        //string assetPath = AssetDatabase.GetAssetPath(mTile.tileSprite);
        //Object[] loadedAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        /* foreach (Object asset in loadedAssets) {
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
        } */

        tileBlock = new Tile[tileWidth, tileHeight];

        int spriteIndex = 0;
        for (int y = tileHeight-1; y >= 0; y--) {
            for (int x = 0; x < tileWidth; x++) {
                int index = (x * tileHeight) + y;
                Tile newTile = (Tile)ScriptableObject.CreateInstance("Tile");
                newTile.sprite = spriteSections[spriteIndex];
                newTile.colliderType = Tile.ColliderType.Sprite;
                tileBlock[x, y] = newTile;
                spriteIndex++;
            }
        }

        bool[] givenSupport = new bool[5];
        bool[] neededSupport = new bool[5];
        
        neededSupport[(int)Direction.up] = mTile.needsTopSupport;
        neededSupport[(int)Direction.left] = mTile.needsLeftSupport;
        neededSupport[(int)Direction.right] = mTile.needsRightSupport;
        neededSupport[(int)Direction.down] = mTile.needsBottomSupport;
        neededSupport[(int)Direction.back] = mTile.needsBackSupport;

        givenSupport[(int)Direction.up] = mTile.givesTopSupport;
        givenSupport[(int)Direction.left] = mTile.givesLeftSupport;
        givenSupport[(int)Direction.right] = mTile.givesRightSupport;
        givenSupport[(int)Direction.down] = mTile.givesBottomSupport;

        supportGiver = new SupportGiver(givenSupport);
        supportPoint = new SupportPoint(mTile.takesAnySupport, neededSupport);
    }
}

[RequireComponent(typeof(WorldController))]
public class TileController : Singleton<TileController>
{
    private WorldController wCon;

    private float[,] tileHealth;                    //Array of current tile health for every tile
    private float[,] maxTileHealth;                 //Array of each tile's undamaged health amount

    private SupportGiver[,] supportGivers;          //Details of support given by each tile
    private SupportPoint[,] supportPoints;          //Details of support needed by each tile

    private static SupportGiver nullSupportGiver;   //Template for 'air'/'empty' block support details
    private static SupportPoint nullSupportPoint;   //Template for 'air'/'empty' block support details

    private int[,] tilesToHeal;                     //Final array of tiles set to be healed back up
    private int[,] tilesMarkedToHeal;               //First array of tiles that need to be healed on next full cycle

    private bool needUpdate = true;                 //Determines whether ANY tiles need to be healed still

    void Start()
    {
        wCon = GetComponent<WorldController>();

        nullSupportGiver = new SupportGiver(true);
        nullSupportPoint = new SupportPoint(true);
    }

    public void InitializeTileStructures() {
        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();

        tilesToHeal = new int[worldWidth, worldHeight];
        tilesMarkedToHeal = new int[worldWidth, worldHeight];

        tileHealth = new float[worldWidth, worldHeight];
        maxTileHealth = new float[worldWidth, worldHeight];

        InitializeSupportArrays(worldWidth, worldHeight);
    }

    private void InitializeSupportArrays(int worldWidth, int worldHeight) {
        supportGivers = new SupportGiver[worldWidth, worldHeight];
        supportPoints = new SupportPoint[worldWidth, worldHeight];
        for (int x = 0; x < worldWidth; x++) {
            for (int y = 0; y < worldHeight; y++) {
                supportGivers[x, y] = new SupportGiver(true);
                supportPoints[x, y] = new SupportPoint(true);
            }
        }
    }

    private bool HealBlocks() {
        bool stillNeedUpdate = false;

        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();
        for (int x = 0; x < worldWidth; x++) {
            for (int y = 0; y < worldHeight; y++) {
                if (tilesToHeal[x, y] == 1) {
                    tilesToHeal[x, y] = 0;
                    tileHealth[x, y] = maxTileHealth[x, y];
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

    public string DigTile(int x, int y, float digAmount) {
        if (!WorldController.InWorldBounds(x, y)) {
            return "nulltile";
        }
        float newHealth = tileHealth[x, y] - digAmount;
        if (newHealth <= 0) {
            tileHealth[x, y] = 0;
            return RemoveTile(x, y);
        } else {
            tileHealth[x, y] = newHealth;
            tilesMarkedToHeal[x, y] = 1;
            needUpdate = true;
            return "nulltile";
        }
    }

    public bool CanPlace(int x, int y, string newTile) {
        if (IsMultiTile(newTile)) {
            Debug.Log(newTile + " is multitile ");
            MultiTileObject mto = allMultiTiles[GetMultiTileName(newTile)];
            return CanPlace(x, y, mto);
        }

        if (wCon.GetTile(x, y, 0) != "air") { return false; }

        SupportPoint supportPoint;
        SupportGiver supportGiver;
        SingleTileObject sto = GetTile(newTile);
        supportPoint = sto.supportPoint;
        supportGiver = sto.supportGiver;

		bool[] neededSupport = supportPoint.neededSupport;
		bool anySupport = supportPoint.takesAnySupport;

        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();
		
		if (neededSupport[(int)Direction.up]) {
			if (anySupport) {
				if (y >= worldHeight-1) { return true; }
				if (supportGivers[x, y+1].supportedSides[(int)Direction.down]) {
					return true;	
				}
			} else {
				if (y < worldHeight-1) {
					if (!supportGivers[x, y+1].supportedSides[(int)Direction.down]) {
						return false;
					}
				}
			}
		}
		
		if (neededSupport[(int)Direction.down]) {
			if (anySupport) {
				if (y <= 0) { return true; }
				if (supportGivers[x, y-1].supportedSides[(int)Direction.up]) {
					return true;	
				}
			} else {
				if (y > 0) {
					if (!supportGivers[x, y+1].supportedSides[(int)Direction.up]) {
						return false;
					}
				}
			}
		}
		
		if (neededSupport[(int)Direction.left]) {
			if (anySupport) {
				if (x <= 0) { return true; }
				if (supportGivers[x-1, y].supportedSides[(int)Direction.right]) {
					return true;	
				}
			} else {
				if (y > 0) {
					if (!supportGivers[x-1, y].supportedSides[(int)Direction.right]) {
						return false;
					}
				}
			}
		}
		
		if (neededSupport[(int)Direction.right]) {
			if (anySupport) {
				if (x >= worldWidth-1) { return true; }
				if (supportGivers[x+1, y].supportedSides[(int)Direction.left]) {
					return true;	
				}
			} else {
				if (x < worldWidth-1) {
					if (!supportGivers[x+1, y].supportedSides[(int)Direction.left]) {
						return false;
					}
				}
			}
		}
		
		return !anySupport;
	}

    private bool CanPlace(int x, int y, MultiTileObject mto) {
        SupportPoint supportPoint;
        SupportGiver supportGiver;

        supportPoint = mto.supportPoint;
        supportGiver = mto.supportGiver;
        bool[] neededSupport = supportPoint.neededSupport;
        bool anySupport = supportPoint.takesAnySupport;

        int width = mto.tileWidth;
        int height = mto.tileHeight;

        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();

        bool[] supportedSides = new bool[5];
        for (int side = 0; side < 5; side++) {
            supportedSides[side] = true;
        }

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (wCon.GetTile(x+i, y+j, 0) != "air") {
                    return false;
                }
                
                if (i == 0) { //Left side support
                    if (neededSupport[(int)Direction.left]) {
                        if (!supportGivers[x+i-1, y+j].supportedSides[(int)Direction.right]) {
                            supportedSides[(int)Direction.left] = false;
                        }
                    }
                }

                if (i == width-1) { //Right side support
                    if (neededSupport[(int)Direction.right]) {
                        if (!supportGivers[x+i+1, y+j].supportedSides[(int)Direction.left]) {
                            supportedSides[(int)Direction.right] = false;
                        }
                    }
                }

                if (j == 0) { //Bottom side support
                    if (neededSupport[(int)Direction.down]) {
                        if (!supportGivers[x+i, y+j-1].supportedSides[(int)Direction.up]) {
                            supportedSides[(int)Direction.down] = false;
                        }
                    }
                }

                if (j == height-1) { //Top side support
                    if (neededSupport[(int)Direction.up]) {
                        if (!supportGivers[x+i, y+j+1].supportedSides[(int)Direction.down]) {
                            supportedSides[(int)Direction.up] = false;
                        }
                    }
                }
            }
        }

        if (neededSupport[(int)Direction.up]) {
            if (anySupport) {
                if (supportedSides[(int)Direction.up]) {
                    return true;
                }
            }

            if (!anySupport) {
                if (!supportedSides[(int)Direction.up]) {
                    return false;
                }
            }
        }

        if (neededSupport[(int)Direction.down]) {
            if (anySupport) {
                if (supportedSides[(int)Direction.down]) {
                    return true;
                }
            }

            if (!anySupport) {
                if (!supportedSides[(int)Direction.down]) {
                    return false;
                }
            }
        }

        if (neededSupport[(int)Direction.left]) {
            if (anySupport) {
                if (supportedSides[(int)Direction.left]) {
                    return true;
                }
            }

            if (!anySupport) {
                if (!supportedSides[(int)Direction.left]) {
                    return false;
                }
            }
        }

        if (neededSupport[(int)Direction.right]) {
            if (anySupport) {
                if (supportedSides[(int)Direction.right]) {
                    return true;
                }
            }

            if (!anySupport) {
                if (!supportedSides[(int)Direction.right]) {
                    return false;
                }
            }
        }

        return !anySupport;     //If we make it through the checks and this tile accepts any support, we've found none and CAN'T place the tile
                                //If we make it through the checks and this tile needs specific support, we've found them all and CAN place the tile
    }

    public string PlaceTile(int x, int y, string newTile, bool isMultiTile = false) {
        if (!isMultiTile) {
            SingleTileObject newTileInfo = GetTile(newTile);
            bool tileCanBePlaced = CanPlace(x, y, newTile);

            if (tileCanBePlaced) {
                string addTileResult = wCon.AddTile(x, y, newTile);

                supportGivers[x, y].UpdateSupportGiver(newTileInfo.supportGiver);
                supportPoints[x, y].UpdateSupportPoint(newTileInfo.supportPoint);
                //supportGivers[x, y].supportedSides[0] = newTileInfo.supportGiver.supportedSides[0];
                tileHealth[x, y] = newTileInfo.maxTileHealth;
                maxTileHealth[x, y] = newTileInfo.maxTileHealth;
                return addTileResult;
            } else {
                return "nulltile"; //New tile is NOT sufficiently supported and can't be placed
            }
        } 
        
        else if (isMultiTile) {
            bool tileCanBePlaced = CanPlace(x, y, newTile);

            if (tileCanBePlaced) {
                PlaceMultiTile(newTile, x, y);
                return newTile;
            }

            else {
                return "nulltile"; //New tile is NOT sufficiently supported and can't be placed
            }
        }

        else {
            return "nulltile";
        }
    }

    public void PlaceMultiTile(string tileToPlace, int x, int y) {
        MultiTileObject mto = GetMultiTileObject(GetMultiTileName(tileToPlace));
        int width = mto.tileWidth;
        int height = mto.tileHeight;

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (i + x >= WorldController.GetWorldWidth() || j + y >= WorldController.GetWorldHeight())
                    continue;
                
                multiTileWorld[i + x, j + y] = curMultiTileID;
                TileRenderer.Instance.RenderTile(i + x, j + y, mto.tileBlock[i, j]);
                WorldController.Instance.ModifyTile(i + x, j + y, "_" + mto.name, false);

                SupportGiver tempSupportGiver = new SupportGiver(true);
                SupportPoint tempSupportPoint = new SupportPoint(true);

                //Adjust support tiles on the outer edges of the multitile
                if (i == 0) {
                    tempSupportGiver.supportedSides[(int)Direction.left] = mto.supportGiver.supportedSides[(int)Direction.left];
                    tempSupportPoint.neededSupport[(int)Direction.left] = mto.supportPoint.neededSupport[(int)Direction.left];
                }

                if (i == width-1) {
                    tempSupportGiver.supportedSides[(int)Direction.right] = mto.supportGiver.supportedSides[(int)Direction.right];
                    tempSupportPoint.neededSupport[(int)Direction.right] = mto.supportPoint.neededSupport[(int)Direction.right];
                }

                if (j == 0) {
                    tempSupportGiver.supportedSides[(int)Direction.down] = mto.supportGiver.supportedSides[(int)Direction.down];
                    tempSupportPoint.neededSupport[(int)Direction.down] = mto.supportPoint.neededSupport[(int)Direction.down];
                }

                if (j == height-1) {
                    tempSupportGiver.supportedSides[(int)Direction.up] = mto.supportGiver.supportedSides[(int)Direction.up];
                    tempSupportPoint.neededSupport[(int)Direction.up] = mto.supportPoint.neededSupport[(int)Direction.up];
                }

                supportGivers[x+i, y+j].UpdateSupportGiver(tempSupportGiver);
                supportPoints[x+i, y+j].UpdateSupportPoint(tempSupportPoint);
            }
        }

        curMultiTileID++;
    }

    public string RemoveTile(int x, int y) {
        string removedBlockID = wCon.GetTile(x, y);
        if (removedBlockID == "nulltile") {
            return removedBlockID;
        }
        
        if (supportGivers[x, y] != null) {
            supportGivers[x, y].UpdateSupportGiver(nullSupportGiver);
        } else {
            supportGivers[x, y] = new SupportGiver(nullSupportGiver);
        }

        if (supportPoints[x, y] != null) {
            supportPoints[x, y].UpdateSupportPoint(nullSupportPoint);
        } else {
            supportPoints[x, y] = new SupportPoint(nullSupportPoint);
        }

        if (IsMultiTile(removedBlockID)) {
            RemoveMultiTile(x, y);
            removedBlockID = GetMultiTileName(removedBlockID);
        } else {
            wCon.RemoveTile(x, y);
            RemovedSupportCheck(x, y);
        }

        ItemManager.SpawnDroppedItem(removedBlockID, x, y, Vector2.one*300f);

        return removedBlockID;
    }

    public void RemoveMultiTile(int x, int y) {
        if (multiTileWorld[x, y] == 0)
            return;

        int removingID = multiTileWorld[x, y];

        Vector2Int startPos = FindMultiTileStart(x, y);

        bool reachedEndX = false;
        bool reachedEndY = false;
        for (int i = 0; !reachedEndX; i++) {
            int realX = i + startPos.x;
            if (realX > multiTileWorld.GetUpperBound(0)) {
                reachedEndX = true;
                continue;
            } 

            reachedEndY = false;

            for (int j = 0; !reachedEndY; j++) {
                int realY = j + startPos.y;
                if (realX <= multiTileWorld.GetUpperBound(0)-1) {
                    if (multiTileWorld[realX+1, realY] != removingID) {
                        reachedEndX = true;
                    }
                }

                if (realY > multiTileWorld.GetUpperBound(1)) {
                    reachedEndY = true;
                    continue;
                }

                if (realY <= multiTileWorld.GetUpperBound(1)-1) {
                    if (multiTileWorld[realX, realY+1] != removingID) {
                        reachedEndY = true;
                    }
                }

                multiTileWorld[realX, realY] = 0;
                supportGivers[realX, realY].UpdateSupportGiver(nullSupportGiver);
                supportPoints[realX, realY].UpdateSupportPoint(nullSupportPoint);
                RemovedSupportCheck(realX, realY);
                WorldController.Instance.ModifyTile(realX, realY, "air", true);
            }
        }
    }

    public void InitializeNewTile(int x, int y, string newTile) {
        if (newTile == "air" || newTile == "") {
            supportGivers[x, y] = new SupportGiver(nullSupportGiver);
            supportPoints[x, y] = new SupportPoint(nullSupportPoint);
            return;
        }

        SingleTileObject newTileInfo = GetTile(newTile);
        if (supportGivers[x, y] == null) {
            supportGivers[x, y] = new SupportGiver(newTileInfo.supportGiver);
        } else {
            supportGivers[x, y].UpdateSupportGiver(newTileInfo.supportGiver);
        }

        if (supportPoints[x, y] == null) {
            supportPoints[x, y] = new SupportPoint(newTileInfo.supportPoint);
        } else {
            supportPoints[x, y].UpdateSupportPoint(newTileInfo.supportPoint);
        }

        tileHealth[x, y] = newTileInfo.maxTileHealth;
        maxTileHealth[x, y] = newTileInfo.maxTileHealth;
    }

    private void RemovedSupportCheck(int x, int y) {
        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();

        if (x > 0) { //left neighbor
            if (!CheckTileSupport(x-1, y)) {
                RemoveTile(x-1, y);
            }
        }

        if (x < worldWidth-1) { //right neighbor 
            if (!CheckTileSupport(x+1, y)) {
                RemoveTile(x+1, y);
            }
        }

        if (y > 0) { //bottom neighbor
            if (!CheckTileSupport(x, y-1)) {
                RemoveTile(x, y-1);
            }
        }

        if (y < worldHeight-1) { //top neighbor
            if (!CheckTileSupport(x, y+1)) {
                RemoveTile(x, y+1);
            }
        }
    }

    private bool CheckTileSupport(int x, int y) {
        int worldWidth = WorldController.GetWorldWidth();
        int worldHeight = WorldController.GetWorldHeight();

        bool[] neededSupport = supportPoints[x, y].neededSupport;
        bool anySupport = supportPoints[x, y].takesAnySupport;

        if (x > 0) { //left neighbor
            if (neededSupport[(int)Direction.left]) {
                if (anySupport) {
                    if (supportGivers[x-1, y].supportedSides[(int)Direction.right]) {
                        return true;
                    }
                } if (!anySupport) {
                    if (!supportGivers[x-1, y].supportedSides[(int)Direction.right]) {
                        return false;
                    }
                }
            }
        }

        if (x < worldWidth-1) { //right neighbor 
            if (neededSupport[(int)Direction.right]) {
                if (anySupport) {
                    if (supportGivers[x+1, y].supportedSides[(int)Direction.left]) {
                        return true;
                    }
                } if (!anySupport) {
                    if (!supportGivers[x+1, y].supportedSides[(int)Direction.left]) {
                        return false;
                    }
                }
            }
        }

        if (y > 0) { //bottom neighbor
            if (neededSupport[(int)Direction.down]) {
                if (anySupport) {
                    if (supportGivers[x, y-1].supportedSides[(int)Direction.up]) {
                        return true;
                    }
                } if (!anySupport) {
                    if (!supportGivers[x, y-1].supportedSides[(int)Direction.up]) {
                        return false;
                    }
                }
            }
        }

        if (y < worldHeight-1) { //top neighbor
            if (neededSupport[(int)Direction.up]) {
                if (anySupport) {
                    if (supportGivers[x, y+1].supportedSides[(int)Direction.down]) {
                        return true;
                    }
                } if (!anySupport) {
                    if (!supportGivers[x, y+1].supportedSides[(int)Direction.down]) {
                        return false;
                    }
                }
            }
        }

        return !anySupport;
    }

    public string PlaceTile(string newTile, bool isMultiTile = false) {
        Vector2 mousePos = CursorController.Instance.GetMousePos();
        return PlaceTile((int)mousePos.x, (int)mousePos.y, newTile, isMultiTile);
    }

    public string DigTile(float digAmount) {
        Vector2 mousePos = CursorController.Instance.GetTileSelectionPos();
        int tileSelectionDiameter = CursorController.Instance.tileSelectionDiameter;
        float tileSelectionRadius = ((float)(tileSelectionDiameter)/2f);
        Vector2 digStartPos = new Vector2(mousePos.x - tileSelectionRadius, mousePos.y - tileSelectionRadius);
        
        for (int x = 0; x < tileSelectionDiameter; x++) {
            for (int y = 0; y < tileSelectionDiameter; y++) {
                DigTile((int)digStartPos.x + x, (int)digStartPos.y + y, digAmount);
            }
        }
        
        return "nulltile";//DigTile((int)mousePos.x, (int)mousePos.y, digAmount);
    }

    public int ReverseDirection(int direction) {
		switch (direction) {
			case 0:
				return 3;
			case 1:
				return 2;
			case 2:
				return 1;
			case 3:
				return 0;
			default:
				return 0;
		}
	}

    //Single Tile Management ----------------------------------------------------------------------------------------

    public List<SingleTile> allTiles = new List<SingleTile>();
    public Dictionary<string, SingleTileObject> allTileObjects = new Dictionary<string, SingleTileObject>();

    public void InitializeTiles() {
            Resources.LoadAll("Tiles/");
            SingleTile[] foundTiles = (SingleTile[]) Resources.FindObjectsOfTypeAll(typeof(SingleTile));
            foreach (SingleTile tile in foundTiles) {
                SingleTileObject sto = new SingleTileObject();
                sto.InitializeSingleTile(tile);
                allTileObjects[sto.name] = sto;
                Debug.Log("Loaded tile " + sto.name);
            }
        }

    public SingleTileObject GetTile(string tileName) {
            if (allTileObjects.ContainsKey(tileName))
                return allTileObjects[tileName];
            else if (tileName[0] == '_') {
                return allTileObjects["air"];
            }
            else {
                Debug.Log("Couldn't find tile " + tileName + "!");
                return null;
            }
        }
    
    //Multi Tile Management ----------------------------------------------------------------------------------------
    
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
            allMultiTiles[mt.tileName] = mto;
            //Debug.Log("Loaded item " + i.name);
        }

        multiTileWorld = new int[WorldController.GetWorldWidth(), WorldController.GetWorldHeight()];
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
                break;
            }
        } 
        
        if (!foundStartX)
            startPos.x = 0;
        
        for (int j = y; j > 1; j--) {
            if (multiTileWorld[x, j-1] != removingID) {
                startPos.y = j;
                foundStartY = true;
                break;
            }   
        }

        if (!foundStartY)
            startPos.y = 0;

        return startPos;
    }

    public MultiTileObject GetMultiTileObject(string multiTileName) {
        if (!allMultiTiles.ContainsKey(multiTileName)) {
            Debug.Log("Couldn't find multitile " + multiTileName + "!");
            return null;
        }
        return allMultiTiles[multiTileName];
    }

    public bool IsMultiTile(string tileName) {
        if (tileName == "") {
            return false;
        }

        //Check for underscore at beginning of tile name
        return tileName[0] == '_';
    }

    public string GetMultiTileName(string tileName) {
        return tileName.Substring(1); //Skip underscore at beginning of multitile name
    }
    //----------------------------------------------------------------------------------------
}