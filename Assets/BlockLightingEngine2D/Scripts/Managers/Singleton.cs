using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the Singleton Design Pattern for any generic child.
/// Prevents duplicates of T.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("[Singleton] WARNING: An instance already exists of " + 
                Instance.GetType() + ". Destroying this duplicate...");
            Destroy(gameObject);
            return;
        }
        Instance = this as T;
    }
}