using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Very simple movement script for the camera.
/// </summary>
public class SimpleMovement : MonoBehaviour
{
    public float speed;


	void Update ()
    {
        transform.position += new Vector3(
            Input.GetAxis("Horizontal") * Time.deltaTime * speed,
            Input.GetAxis("Vertical") * Time.deltaTime * speed, 0f);
    }
}
