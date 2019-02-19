using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerTriggerCol : MonoBehaviour
{
    public PlayerDetection parentDetector;

    void OnTriggerEnter2D(Collider2D col) {
        parentDetector.OnChildTriggerEnter2D(col);
    }

    void OnTriggerExit2D(Collider2D col) {
        parentDetector.OnChildTriggerExit2D(col);
    }
}
