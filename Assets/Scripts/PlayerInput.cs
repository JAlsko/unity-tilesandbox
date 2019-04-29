using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : Singleton<PlayerInput> {

	public CharacterMover cMov;
	public Camera mainCam;

	Vector3 curMousePos;
    public Transform tileSelectionBox;
	
	void Update () {
		curMousePos = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCam.transform.position.z));
        curMousePos = SimplifyMousePos(curMousePos);
        Vector3 adjustedMousePos = AdjustMousePos(curMousePos, .5f);
        tileSelectionBox.position = adjustedMousePos;

		HandleMove();
		HandleClick();
		HandleUI();
	}

	void HandleMove() {
		cMov.Move(Input.GetAxis("Horizontal"));
		if (Input.GetKeyDown(KeyCode.Space)) {
			cMov.Jump();
		}
	}

	void HandleClick() {
		if (Input.GetKeyDown(KeyCode.Mouse0)) {
			//WorldModifier.Instance.RemoveTile((int)curMousePos.x, (int)curMousePos.y);
		}

		if (Input.GetKeyDown(KeyCode.Mouse1)) {
			//WorldController.Instance.AddTile((int)curMousePos.x, (int)curMousePos.y, 3);
		}
	}

	void HandleUI() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			UIController.Instance.ToggleInventory();
		}

		if (Input.GetKeyDown(KeyCode.I)) {
			UIController.Instance.ToggleCrafting();
		}
	}

	Vector3 SimplifyMousePos(Vector3 mousePos) {
        mousePos.x = (int)mousePos.x;
        mousePos.y = (int)mousePos.y;
        return mousePos;
    }

    Vector3 AdjustMousePos(Vector3 mousePos, float offset) { 
        mousePos.x += offset;
        mousePos.y += offset;
        return mousePos;
    }

    public Vector3 GetMousePos() {
        return curMousePos;
    }
}
