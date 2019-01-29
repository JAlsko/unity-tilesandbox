using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class PlayerMouseInput : MonoBehaviour
{
    private static PlayerMouseInput instance = null;

    Vector3 curMousePos;

    Camera thisCam;

    public Transform tileSelectionBox;

    void Start() {
        if (instance == null) {
			instance = this;
		} else if (instance != this) {
			Destroy(this.gameObject);
		}

        thisCam = GetComponent<Camera>();
    }

    public static PlayerMouseInput Instance {
        get {
            return instance;
        }
    }

    void Update() {
        curMousePos = thisCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -transform.position.z));
        curMousePos = SimplifyMousePos(curMousePos);
        curMousePos = AdjustMousePos(curMousePos, .5f);
        tileSelectionBox.position = curMousePos;
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
