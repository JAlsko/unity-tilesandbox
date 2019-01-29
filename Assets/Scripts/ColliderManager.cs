using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderManager : MonoBehaviour
{
    private static ColliderManager instance = null;

    private Dictionary<int, EntityCollider> colliders = new Dictionary<int, EntityCollider>();
    private static int MAX_COLLIDERS = 128;
    private int colliderCount = 0;
    private int runningIndex = 0;

    void Start()
    {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
    }

    public static ColliderManager Instance {
        get {
            return instance;
        }
    }

    public void AddCollider(EntityCollider col) {
        if (colliderCount > MAX_COLLIDERS) {
            Debug.LogError("Too many colliders!");
            return;
        }

        if (colliders.ContainsKey(runningIndex)) {
            Debug.LogError("Trying to add collider in position with existing collider!");
            return;
        }

        colliderCount++;

        col.SetIndex(runningIndex);
        colliders[runningIndex] = col;
        runningIndex++;
    }

    public void RemoveCollider(int index) {
        if (colliders.ContainsKey(index)) {
            colliderCount--;
            colliders.Remove(index);
        } else {
            Debug.Log("Trying to remove nonexistent collider!");
        }
    }

    public void OnWorldUpdate(int[,] world) {
        foreach (KeyValuePair<int, EntityCollider> kvp in colliders) {
            EntityCollider col = kvp.Value;
            col.UpdateWorld(world);
        }
    }

}
