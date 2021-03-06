using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pooler : MonoBehaviour {

	public List<GameObject> pooledObject;
	public int pooledAmount = 20;
	public bool willGrow = true;

	List<GameObject> pooledObjects;

	void Awake() {
	}

	void Start () {
		pooledObjects = new List<GameObject> ();
		for (int i = 0; i < pooledAmount; i++) {
			GameObject obj = (GameObject)Instantiate (pooledObject[i%pooledObject.Count]);
			obj.transform.parent = transform;
			obj.SetActive (false);
			pooledObjects.Add (obj);
		}

	}

	public GameObject GetPooledObject() {
		for (int i = 0; i < pooledObjects.Count; i++) {
			if (!pooledObjects [i].activeInHierarchy) {
				return pooledObjects [i];
			}
		}

		if (willGrow) {
			GameObject obj = (GameObject)Instantiate (pooledObject[pooledObjects.Count%pooledObject.Count]);
			pooledObjects.Add (obj);
			return obj;
		}

		return null;
	}

}
