using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

public enum RuleTileType {
    none = -1,
    innerBlock = 0,
    openTop = 1,
    openLeft = 2,
    openRight = 3,
    openBottom = 4,
    cornerTopLeft = 5,
    cornerBottomLeft = 6,
    cornerTopRight = 7,
    cornerBottomRight = 8,
    verticalPipe = 9,
    horizontalPipe = 10,
    leftPipeEnd = 11,
    rightPipeEnd = 12,
    topPipeEnd = 13,
    bottomPipeEnd = 14,
    exposedBlock = 15,
}

public class TilesetCreatorPopup : EditorWindow {

    private static RuleTileType[] defaultRuleOrder = new RuleTileType[16];
    RuleTileType[] ruleOrder = new RuleTileType[16];
    int tileVariants = 1;
    Sprite tileSetSprite;
    UnityEngine.Tilemaps.Tile.ColliderType colliderType;
    string saveName = "New Rule Tile";

    const float labelPadding = 15f;

    public void Awake() {
        for (int i = 0; i < 16; i++) {
            defaultRuleOrder[i] = (RuleTileType)i;
            ruleOrder[i] = defaultRuleOrder[i];
        }
    }

    [MenuItem("Tilesets/Create Tileset")]
    static void Init()
    {
        TilesetCreatorPopup window = ScriptableObject.CreateInstance<TilesetCreatorPopup>();
        window.Awake();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 1000, 1000);
        window.minSize = new Vector2(500, 500);
        window.maxSize = new Vector2(500, 500);
        window.Show();
    }

    public static int IntField(string label, int text)
    {
        var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));
        EditorGUIUtility.labelWidth = textDimensions.x + labelPadding;
        return EditorGUILayout.IntField(label, text);
    }

    public static UnityEngine.Object ObjectField(string label, UnityEngine.Object obj, Type t)
    {
        var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));
        EditorGUIUtility.labelWidth = textDimensions.x + labelPadding;
        return EditorGUILayout.ObjectField(label, obj, t, false);
    }

    public static Enum EnumPopup(string label, Enum enm)
    {
        var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));
        EditorGUIUtility.labelWidth = textDimensions.x + labelPadding;
        return EditorGUILayout.EnumPopup(selected:enm, label:label, options:GUILayout.MinWidth(200));
    }
    
    void DefaultRuleOrder() {
        ruleOrder = new RuleTileType[defaultRuleOrder.Length];
        for (int i = 0; i < ruleOrder.Length; i++) {
            ruleOrder[i] = defaultRuleOrder[i];
        }
    }

    void ResetRuleOrder() {
        ruleOrder = new RuleTileType[defaultRuleOrder.Length];
        for (int i = 0; i < ruleOrder.Length; i++) {
            ruleOrder[i] = RuleTileType.none;
        }
    }

    void OnGUI()
    {
        GUILayout.ExpandWidth(false);

        int normalTextSize = GUI.skin.label.fontSize;
        GUI.skin.label.fontSize = 20;

        GUILayout.Label("Tileset Creator");
        GUILayout.Space(10);

        GUI.skin.label.fontSize = normalTextSize;

        EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                
                    EditorGUILayout.LabelField("Order of Tile Orientations", EditorStyles.boldLabel);

                    for(int i = 0; i < ruleOrder.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        ruleOrder[i] = (RuleTileType)EnumPopup(i+"", ruleOrder[i]);
                        //GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }

                    GUILayout.Space(15);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Default Order")) DefaultRuleOrder();
                    if (GUILayout.Button("Reset Order")) ResetRuleOrder();
                    EditorGUILayout.EndHorizontal();
                    
                EditorGUILayout.EndVertical();

                GUILayout.Space(40);

                EditorGUILayout.BeginVertical();

                    EditorGUILayout.LabelField("Tileset Save Name", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    saveName = EditorGUILayout.TextField(saveName);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("Tile Variants", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    tileVariants = Mathf.Max(1, IntField("", tileVariants));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("Tile Collider Type", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    colliderType = (UnityEngine.Tilemaps.Tile.ColliderType)EnumPopup("", colliderType);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("Full Tile Spritesheet", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Insert any tile from the sliced spritesheet", EditorStyles.miniLabel);

                    EditorGUILayout.BeginHorizontal();
                    tileSetSprite = (Sprite)ObjectField("", tileSetSprite, typeof(Sprite));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(40);
            if (GUILayout.Button("Generate Tileset!") && tileSetSprite != null) TilesetGenerator.GenerateTileset(tileVariants, ruleOrder, tileSetSprite, saveName, colliderType);

        EditorGUILayout.EndVertical();
    }
}

public class TilesetGenerator : Editor
{
    public static void GenerateTileset(int tileVariants, RuleTileType[] ruleTileTypeOrder, Sprite tileSetSprite, string saveName, UnityEngine.Tilemaps.Tile.ColliderType tilesetColliderType, float noiseScale = 0.5f) {
        List<Sprite> spriteSections = new List<Sprite>();

        string assetPath = AssetDatabase.GetAssetPath(tileSetSprite);
        UnityEngine.Object[] loadedAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in loadedAssets) {
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

        RuleTile newRuleTile = ScriptableObjectUtility.CreateAsset<RuleTile>("Assets/Resources/Tiles/RuleTiles", saveName);
        newRuleTile.m_DefaultSprite = spriteSections[0];
        newRuleTile.m_DefaultColliderType = tilesetColliderType;

        RuleTile.TilingRule newTilingRule = new RuleTile.TilingRule();

        for (int i = 0; i < spriteSections.Count; i++) {
            int ruleIndex = i / tileVariants;

            if (ruleTileTypeOrder[ruleIndex] == RuleTileType.none) {
                continue;
            }

            if (i % tileVariants == 0) {
                newTilingRule = new RuleTile.TilingRule();
                newTilingRule.m_RuleTransform = RuleTile.TilingRule.Transform.Fixed;
                newTilingRule.m_ColliderType = tilesetColliderType;
                newTilingRule.m_Output = (tileVariants != 1) ? RuleTile.TilingRule.OutputSprite.Random : RuleTile.TilingRule.OutputSprite.Single;
                newTilingRule.m_PerlinScale = noiseScale;
                newTilingRule.m_RandomTransform = RuleTile.TilingRule.Transform.Fixed;
                newTilingRule.m_Sprites = new Sprite[tileVariants];
                newTilingRule.m_Sprites[i % tileVariants] = spriteSections[i];
                newTilingRule.m_Neighbors = GetRuleNeighbors(ruleTileTypeOrder[ruleIndex]);
            } else {
                newTilingRule.m_Sprites[i % tileVariants] = spriteSections[i];
            }

            if ((i + 1) % tileVariants == 0) {
                newRuleTile.m_TilingRules.Add(newTilingRule);
            }
        }
        
    }

    static int[] GetRuleNeighbors(RuleTileType rule) {
        int[] neighborChecks = new int[8];

        int thisTile = RuleTile.TilingRule.Neighbor.This;
        int notThisTile = RuleTile.TilingRule.Neighbor.NotThis;

        int top = 1;
        int left = 3;
        int right = 4;
        int bottom = 6;

        switch (rule) {
            case RuleTileType.innerBlock:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.openTop:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.openLeft:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.openRight:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.openBottom:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = notThisTile;
            break;

            case RuleTileType.cornerTopLeft:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.cornerBottomLeft:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = notThisTile;
            break;

            case RuleTileType.cornerTopRight:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.cornerBottomRight:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = notThisTile;
            break;

            case RuleTileType.verticalPipe:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.horizontalPipe:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = notThisTile;
            break;

            case RuleTileType.topPipeEnd:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = thisTile;
            break;

            case RuleTileType.leftPipeEnd:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = thisTile;
                neighborChecks[bottom] = notThisTile;
            break;

            case RuleTileType.rightPipeEnd:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = thisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = notThisTile;
            break;

            case RuleTileType.bottomPipeEnd:
                neighborChecks[top] = thisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = notThisTile;
            break;

            case RuleTileType.exposedBlock:
                neighborChecks[top] = notThisTile;
                neighborChecks[left] = notThisTile;
                neighborChecks[right] = notThisTile;
                neighborChecks[bottom] = notThisTile;
            break;
        }

        return neighborChecks;
    }
}
