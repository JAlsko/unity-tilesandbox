using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EntityCollider : MonoBehaviour
{
    public float tileHeight = 1;

    string[,] world;
    int colliderIndex;

    void Start() {

    }

    void OnEnable() {
        world = WorldController.Instance.world_fg;
        ColliderManager.Instance.AddCollider(this);
    }

    public void SetIndex(int index) {
        colliderIndex = index;
    }

    public void UpdateWorld(string[,] newWorld) {
        world = newWorld;
    }

    public void OnDestroy() {
        ColliderManager.Instance.RemoveCollider(colliderIndex);
    }

    public void OnDisable() {
        ColliderManager.Instance.RemoveCollider(colliderIndex);
    }
}
