using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMover : MonoBehaviour {

	public float maxSpeed = 10f;
	public float jumpForce = 100f;
	Rigidbody2D rbody;
	Animator anim;
	bool isAnimated = true;

	void Start () {
		rbody = GetComponent<Rigidbody2D>();
		if (!GetComponent<Animator>())
			isAnimated = false;
		else
			anim = GetComponent<Animator>();
	}
	
	void Update () {
		
	}

	public void Move(float dir) {
		rbody.velocity = new Vector2(dir * maxSpeed, rbody.velocity.y);
		AnimRun(Mathf.Abs(dir));
	}

	public void Jump() {
		rbody.AddForce(Vector2.up * jumpForce);
		AnimJump();
	}

	void AnimRun(float speed) {
		if (isAnimated)
			anim.SetFloat("RunSpeed", speed);
	}

	void AnimJump() {
		if (isAnimated)
			anim.SetTrigger("Jump");
	}
}
