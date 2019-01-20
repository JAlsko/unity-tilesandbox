using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	public Transform toFollow;
	public bool followX = true;
	public bool followY = true;
	Vector3 offset;
	Vector3 origin;
	
	void Start () {
		origin = transform.position;
		offset = transform.position - toFollow.position;
	}
	
	void LateUpdate () {
		Vector3 newPos = toFollow.position + offset;
		
		float newX;
		float newY;
		
		if (!followX) {
			newX = origin.x;
		} else {
			newX = newPos.x;
		}

		if (!followY) {
			newY = origin.y;
		} else {
			newY = newPos.y;
		}

		transform.position = new Vector3(newX, newY, newPos.z);
	}
}
