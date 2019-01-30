using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	public Transform toFollow;
	public bool followX = true;
	public bool followY = true;
	Vector3 offset;
	Vector3 origin;

	public Vector2 xBounds;
	public Vector2 yBounds;
	
	void Start () {
		origin = transform.position;
		offset = transform.position - toFollow.position;
		transform.parent = null;
	}
	
	void LateUpdate () {
		Vector3 newPos = toFollow.position + offset;
		
		float newX;
		float newY;
		
		if (!followX) {
			newX = origin.x;
		} else {
			if (newPos.x >= xBounds.x && newPos.x <= xBounds.y) {
				newX = newPos.x;
			} else {
				newX = transform.position.x;
			}
		}

		if (!followY) {
			newY = origin.y;
		} else {
			if (newPos.y >= yBounds.x && newPos.y <= yBounds.y) {
				newY = newPos.y;
			} else {
				newY = transform.position.y;
			}
		}

		transform.position = new Vector3(newX, newY, newPos.z);
	}
}
