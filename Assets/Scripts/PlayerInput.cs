using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMover))]
public class PlayerInput : MonoBehaviour {

	private CharacterMover cMov;
	public Camera mainCam;

	Vector3 curMousePos;
    public Transform tileSelectionBox;

    void Start () {
		cMov = GetComponent<CharacterMover>();
	}
	
	void Update () {
		curMousePos = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -transform.position.z));
        curMousePos = SimplifyMousePos(curMousePos);
        curMousePos = AdjustMousePos(curMousePos, .5f);
        tileSelectionBox.position = curMousePos;

		HandleMove();
		HandleClick();
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

    public Vector2 GetMousePos() {
        return new Vector2((int)curMousePos.x, (int)curMousePos.y);
    }
}
