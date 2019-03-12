using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetZone : MonoBehaviour
{
    public DroppedItem di;

    void OnTriggerEnter2D(Collider2D col) {
        if (col.tag == "Player") {
            di.UpdateMagnetTarget(col.transform, col.GetInstanceID());
        }
    }

    void OnTriggerExit2D(Collider2D col) {
        if (col.tag == "Player") {
            di.UpdateMagnetTarget(null, col.GetInstanceID());
        }
    }
}
