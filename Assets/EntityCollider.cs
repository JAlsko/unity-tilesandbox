using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EntityCollider : MonoBehaviour
{
    public float tileHeight = 1;

    int[,] world;
    int colliderIndex;

    void Start() {

    }

    void OnEnable() {
        world = WorldController.Instance.world;
        ColliderManager.Instance.AddCollider(this);
    }

    public void SetIndex(int index) {
        colliderIndex = index;
    }

    public void UpdateWorld(int[,] newWorld) {
        world = newWorld;
    }

    public void OnDestroy() {
        ColliderManager.Instance.RemoveCollider(colliderIndex);
    }

    public void OnDisable() {
        ColliderManager.Instance.RemoveCollider(colliderIndex);
    }
}
