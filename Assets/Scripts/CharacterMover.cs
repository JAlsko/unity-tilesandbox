using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Entity))]
public class CharacterMover : MonoBehaviour {

	Rigidbody2D rbody;
	Animator anim;
	Entity ent;
	bool isAnimated = true;

	void Start () {
		rbody = GetComponent<Rigidbody2D>();
		if (!GetComponent<Animator>())
			isAnimated = false;
		else
			anim = GetComponent<Animator>();

		ent = GetComponent<Entity>();
		if (!ent.initialized) {
			this.enabled = false;
		}
	}
	
	void Update () {
		
	}

	public void Move(float dir) {
		float moveSpeed = ent.GetFloat(EntityManager.Instance.moveSpeedAttributeName);
		rbody.velocity = new Vector2(dir * moveSpeed, rbody.velocity.y);
		AnimRun(Mathf.Abs(dir));
	}

	public void Jump() {
		rbody.velocity = Vector2.zero;
		float jumpSpeed = ent.GetFloat(EntityManager.Instance.jumpHeightAttributeName);
		rbody.AddForce(Vector2.up * jumpSpeed);
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
