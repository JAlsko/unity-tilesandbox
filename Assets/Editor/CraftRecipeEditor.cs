using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftRecipe))]
public class CraftRecipeEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("ingredients"), true);
        CraftRecipe recipe = (CraftRecipe)target;

        //recipe.initialized = EditorGUILayout.IntField(recipe.initialized);

        int normalTextSize = GUI.skin.label.fontSize;
        GUI.skin.label.fontSize = 25;

        GUILayout.Label("Output Item");
        GUILayout.Space(10);

        GUI.skin.label.fontSize = normalTextSize;

        GUILayout.BeginHorizontal();

        if (recipe.outputItem != null) {
            Texture textureToDraw = recipe.outputItem.icon.texture;
            GUILayoutOption[] iconOptions = new GUILayoutOption[]{GUILayout.MaxWidth(EditorGUIUtility.singleLineHeight*4), GUILayout.ExpandWidth(false)};
            EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(false, height:EditorGUIUtility.singleLineHeight*4, options:iconOptions), textureToDraw);
        }

        GUILayout.BeginVertical();
        string outputItemName = "New Item";

        if (recipe.outputItem != null) {
            outputItemName = recipe.outputItem.name;
        }

        GUILayout.Label(outputItemName);
        recipe.outputItem = (Item)EditorGUILayout.ObjectField(recipe.outputItem, typeof(Item), false);
        GUILayout.EndVertical();
        
        GUILayout.BeginVertical();
        GUILayout.Label("Output Item");
        recipe.outputCount = EditorGUILayout.IntField(recipe.outputCount);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        GUI.skin.label.fontSize = 25;
        GUILayout.Label("Ingredients List");
        GUILayout.Space(10);

        GUI.skin.label.fontSize = normalTextSize;

        Texture minusButtonTexture = Resources.Load("Editor/minusButton") as Texture;
        Texture plusButtonTexture = Resources.Load("Editor/plusButton") as Texture;

        //SerializedProperty ingredientsList = serializedObject.FindProperty("ingredientItems");
        int i = 0;
        for (; i < recipe.ingredientItems.Count; i++) {
            //SerializedProperty listElement = ingredientsList.GetArrayElementAtIndex(i);
            //SerializedObject sObj = ingredientsList.GetArrayElementAtIndex(i).serializedObject;
            
            string itemName = "New Item";

            if (recipe.ingredientItems[i] != null) {
                itemName = recipe.ingredientItems[i].name;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Minus"))) {
                recipe.ingredientCounts.RemoveAt(i);
                recipe.ingredientItems.RemoveAt(i);
            }

            if (recipe.ingredientItems.Count > i) {
                if (recipe.ingredientItems[i] != null) {
                    Texture textureToDraw = recipe.ingredientItems[i].icon.texture;
                    GUILayoutOption[] iconOptions = new GUILayoutOption[]{GUILayout.MaxWidth(EditorGUIUtility.singleLineHeight*2), GUILayout.ExpandWidth(false)};
                    EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(false, height:EditorGUIUtility.singleLineHeight*2, options:iconOptions), textureToDraw);
                }
            } else {
                break;
            }
            GUILayout.BeginVertical();
            GUILayout.Label(itemName);
            //EditorGUIUtility.labelWidth = 40f;
            recipe.ingredientItems[i] = (Item)EditorGUILayout.ObjectField(recipe.ingredientItems[i], typeof(Item), false);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Count");
            recipe.ingredientCounts[i] = EditorGUILayout.IntField(recipe.ingredientCounts[i]);
            GUILayout.EndVertical();
            //EditorGUILayout.PropertyField(sObj.FindProperty("ingredientItems.Array.data[" + index + "]"), new GUIContent("Item"));
            //EditorGUIUtility.labelWidth = 80f;
            //EditorGUILayout.PropertyField(sObj.FindProperty("ingredientCounts.Array.data[" + i + "]"), new GUIContent("Needed Count"));
            GUILayout.EndHorizontal();
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"))) {
            recipe.ingredientItems.Add(null);
            recipe.ingredientCounts.Add(1);
        }
        GUILayout.EndHorizontal();

            /* 
            
            SerializedProperty ingredientsList = serializedObject.FindProperty("ingredients");

            EditorGUILayout.PropertyField(ingredientsList);
            EditorGUILayout.PropertyField(ingredientsList.FindPropertyRelative("Array.size"));
            if (ingredientsList.isExpanded) {
                for (int i = 0; i < ingredientsList.arraySize; i++) {
                    SerializedProperty ingredientsListElement = ingredientsList.GetArrayElementAtIndex(i);
                    SerializedObject sObj = ingredientsList.GetArrayElementAtIndex(i).serializedObject;

                    Texture2D textureToDraw;

                    if (sObj == null) {
                        textureToDraw = null;
                    }

                    else if (sObj.FindProperty("item") == null) {
                        textureToDraw = null;
                    }

                    else if (sObj.FindProperty("item").objectReferenceValue == null) {
                        textureToDraw = null;
                    }
                    
                    else {
                        textureToDraw = ((Item)sObj.FindProperty("item").objectReferenceValue).icon.texture;
                    }
                    EditorGUILayout.PropertyField(sObj.FindProperty("item"));
                    GUILayout.BeginHorizontal();
                    EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(false, height:EditorGUIUtility.singleLineHeight*2, options:GUILayout.MaxWidth(EditorGUIUtility.singleLineHeight*2)), textureToDraw);
                    GUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(sObj.FindProperty("count"));
                    EditorGUILayout.PropertyField(sObj.FindProperty("count"));
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }

*/
		//EditorList.Show(serializedObject.FindProperty("output"));
		//EditorList.Show(serializedObject.FindProperty("craftingTier"));
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(recipe);
		serializedObject.ApplyModifiedProperties();
    }
}