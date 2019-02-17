using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMover))]
public class PlayerInput : MonoBehaviour {

	private CharacterMover cMov;
	public Camera mainCam;

    void Start () {
		cMov = GetComponent<CharacterMover>();
	}
	
	void Update () {
		/*if (Input.mousePosition.x < Screen.width/2) {
			transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, 180, transform.rotation.z));
		} else {
			transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, 0, transform.rotation.z));
		}*/

		HandleMove();

		//HandleClick();
	}

	void HandleMove() {
		cMov.Move(Input.GetAxis("Horizontal"));
		if (Input.GetKeyDown(KeyCode.Space)) {
			cMov.Jump();
		}
	}

	void HandleClick() {
		if (Input.GetKeyDown(KeyCode.Mouse0)) {
			Vector3 mousePos = PlayerMouseInput.Instance.GetMousePos();
			WorldController.Instance.RemoveTile((int)mousePos.x, (int)mousePos.y);
		}

		if (Input.GetKeyDown(KeyCode.Mouse1)) {
			Vector3 mousePos = PlayerMouseInput.Instance.GetMousePos();
			WorldController.Instance.AddTile((int)mousePos.x, (int)mousePos.y, 3);
		}
	}
}
