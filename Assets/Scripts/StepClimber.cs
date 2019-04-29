using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepClimber : MonoBehaviour
{
    public Transform groundFinder;
    public float groundDistance;
    public LayerMask groundMask;

    public Rigidbody2D player;

    public bool feetHit = false;

    void Update() {
        RaycastHit2D hit = Physics2D.Raycast(groundFinder.position, Vector2.down, groundDistance, groundMask);

        if (hit && !feetHit) {
            player.MovePosition(new Vector2(player.transform.position.x, hit.point.y));
        }
    }

    void OnColliderStay2D(Collider2D col) {
        feetHit = true;
    }

    void OnColliderExit2D(Collider2D col) {
        feetHit = false;
    }
}
