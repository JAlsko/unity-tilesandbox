using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterShooter : MonoBehaviour {

	public bool isPlayer = false;
	public GameObject projectile;
	public float shotSpeed = 1f;
	public Transform shotOrigin;
	public AudioSource shotSound;
	private bool makeShotSound = true;

	public AudioSource hurtSound;
	private bool makeHurtSound = true;

	public int hearts = 5;
	private int heartsLeft;
	public AudioSource deathSound;
	private bool makeDeathSound = true;
	private Animator anim;
	private bool isAnimated = true;
	public float deathLength = 1f;

	void Start () {
		if (!GetComponent<Animator>())
			isAnimated = false;
		else
			anim = GetComponent<Animator>();

		if (shotSound == null) {
			makeShotSound = false;
		}
		if (hurtSound == null) {
			makeHurtSound = false;
		}
		if (deathSound == null) {
			makeDeathSound = false;
		}

		heartsLeft = hearts;
	}
	
	void Update () {
		
	}

	public void Shoot() {
		GameObject newProj = Instantiate(projectile, null) as GameObject;
		newProj.transform.position = shotOrigin.position;
		newProj.transform.rotation = shotOrigin.rotation;
		newProj.GetComponent<Rigidbody2D>().AddForce(newProj.transform.right * shotSpeed);
		MakeShotSound();
	}

	void MakeShotSound() {
		if (makeShotSound) {
			shotSound.pitch = UnityEngine.Random.Range(0.9f, 1.2f);
			shotSound.Play();
		}
	}

	void OnTriggerEnter2D(Collider2D col) {
		if (col.transform.tag == "TaserProj" && !isPlayer) {
			heartsLeft--;
			MakeHurtSound();
			Destroy(col.gameObject);
		} else if (col.transform.tag == "BulletProj" && isPlayer) {
			heartsLeft--;
			MakeHurtSound();
			Destroy(col.gameObject);
		}
		if (heartsLeft <= 0) {
			Die();
		}
	}

	void MakeDeathSound() {
		if (makeDeathSound) {
			deathSound.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
			deathSound.Play();
		}
	}

	void MakeHurtSound() {
		if (makeHurtSound)
			hurtSound.Play();
	}

	public void Die() {
		MakeDeathSound();
		AnimDeath();
		foreach(MonoBehaviour r in GetComponents<MonoBehaviour>()) {
			Destroy(r);
		}
		if (!isPlayer)
			Destroy(gameObject, deathLength);
	}

	void AnimDeath() {
		if (isAnimated)
			anim.SetTrigger("Die");
	}
}
