using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    PlayerInventory pInv;
    public bool isPickingUpItem = false;

    public List<int> instanceIDs = new List<int>();

    void Start()
    {
        pInv = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D col) {
        if (col.tag == "DroppedItem" && !instanceIDs.Contains(col.GetInstanceID())) {
            instanceIDs.Add(col.GetInstanceID());
            DroppedItem droppedItem = col.GetComponent<DroppedItem>();
            droppedItem.DisableCollision();
            ItemObject leftOverItem = droppedItem.GetDroppedItem();
            if (leftOverItem == null) {
                Debug.Log("Trying to pickup null item!");
                return;
            }
            leftOverItem = pInv.TryAddItem(leftOverItem);
            if (leftOverItem == null) {
                droppedItem.HideDroppedItem();
                droppedItem.EnableCollision();
            } else {
                droppedItem.InitializeItem(leftOverItem);
                droppedItem.EnableCollision();
            }
            instanceIDs.Remove(col.GetInstanceID());
        }
    }

    /*void OnTriggerExit2D(Collider2D col) {
        if (col.tag == "DroppedItem") {
            isPickingUpItem = false;
        }
    }*/
}
