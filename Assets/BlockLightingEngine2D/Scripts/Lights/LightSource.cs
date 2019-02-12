using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A lightweight struct that represents a block's lighting data while updating or removing lighting.
/// </summary>
public struct LightNode
{
    public Vector3Int position;
    public Color color;
    public LightNode(Vector3Int position, Color color)
    {
        this.position = position;
        this.color = color;
    }
}

/// <summary>
/// Describes a source of light in the game.
/// </summary>
public class LightSource : MonoBehaviour
{
    public Color lightColor;
    public float LightStrength { get; set; }
    public bool Initialized { get; set; }
    public Vector3Int Position { get; set; }


    private void Awake()
    {
        Position = new Vector3Int(
                    (int)transform.position.x,
                    (int)transform.position.y, 0);
        InitializeLight(lightColor, 1f);
    }

    /// <summary>
    /// Set the light's core values and settings.
    /// </summary>
    /// <param name="color"></param>
    /// <param name="strength"></param>
    public void InitializeLight(Color color, float strength)
    {
        lightColor = color;
        LightStrength = strength;
        Initialized = true;
    }
}