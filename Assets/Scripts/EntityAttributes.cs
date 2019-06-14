using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "New Entity Attribute Settings", menuName = "Misc Data/Entity Attribute Settings")]
public class EntityAttributes : ScriptableObject {
	public string entityName;

	[Tooltip("List of entity attributes to override for this entity type.")]
	public List<FloatAttribute> entityAttributes = new List<FloatAttribute>();
}