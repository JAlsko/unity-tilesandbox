using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    public List<int> nearbyCraftTiers = new List<int>();

    public void OnChildTriggerEnter2D(Collider2D col) {
        if (col.tag == "DroppedItem") {
            if (col.GetComponent<DroppedItem>()) {
                col.GetComponent<DroppedItem>().UpdateMagnetTarget(transform, transform.GetInstanceID());
            }
        }
    }

    public void OnChildTriggerExit2D(Collider2D col) {
        if (col.tag == "DroppedItem") {
            if (col.GetComponent<DroppedItem>()) {
                col.GetComponent<DroppedItem>().UpdateMagnetTarget(null, transform.GetInstanceID());
            }
        }
    }
}
