using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Effect))]
public class EffectEditor : Editor
{
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        Effect effect = (Effect)target;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Effect Timing", EditorStyles.boldLabel);
        effect.effectFrequency = EditorGUILayout.FloatField(new GUIContent("Effect Frequency", "The length (in seconds) between each tick of this effect. [Set to 0 for single effects]"), effect.effectFrequency);        
        EditorGUILayout.LabelField(new GUIContent("Total Effect Ticks", "The total number of times this effect will tick in a normal run."), new GUIContent(effect.totalEffectTicks.ToString()));
    }
}
