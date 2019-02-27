using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    PlayerInventory pInv;

    void Start()
    {
        pInv = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D col) {
        if (col.tag == "DroppedItem") {
            DroppedItem droppedItem = col.GetComponent<DroppedItem>();
            ItemObject leftOverItem = droppedItem.GetDroppedItem();
            if (leftOverItem == null) {
                //Debug.Log("Trying to pickup null item!");
                return;
            }
            leftOverItem = pInv.TryAddItem(leftOverItem);
            if (leftOverItem == null) {
                droppedItem.HideDroppedItem();
            } else {
                droppedItem.InitializeItem(leftOverItem);
            }
        }
    }

    /*void OnTriggerExit2D(Collider2D col) {
        if (col.tag == "DroppedItem") {
            isPickingUpItem = false;
        }
    }*/
}
