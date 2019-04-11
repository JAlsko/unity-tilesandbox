using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightController))]
public class LightControllerEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        LightController lCon = (LightController)target;

        if (GUILayout.Button("Update Sky")) {
            lCon.UpdateSkylight(lCon.skyColor);
        }
    }
}
