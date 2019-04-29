using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DebugToggler : MonoBehaviour
{
    public Rect buttonRect = new Rect();

    Renderer thisRenderer;

    void Start() {
        thisRenderer = GetComponent<Renderer>();
    }

    void OnGUI()
    {
        if (GUI.Button(buttonRect, "Toggle " + gameObject.name))
        {
            thisRenderer.enabled = !thisRenderer.enabled;
        }
    }
}
