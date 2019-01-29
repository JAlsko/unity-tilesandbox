using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFollow : MonoBehaviour {

	public Transform player;

	void Start () {
		
	}
	
	void FixedUpdate () {
		transform.position = player.position;
	}
}
