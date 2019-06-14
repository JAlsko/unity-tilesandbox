using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Entity : MonoBehaviour {
	public string entityType = "nullEntity";
	private bool inLiquid = false;
	private bool enteredLiquid = false;

	[Space(10)]
	public string gravityAttrName = "gravity";

	private Rigidbody2D rbody;

	public Dictionary<string, FloatAttribute> floatAttrs = new Dictionary<string, FloatAttribute>();
	[HideInInspector] public bool initialized = false;

    public void Awake() {
        //InitializeEntity();
    }

	public void InitializeEntity() {
		initialized = true;
		floatAttrs = EntityManager.Instance.GetDefaultEntityAttrs();
		List<FloatAttribute> entityOverrides = EntityManager.Instance.GetEntityAttrs(entityType);
		if (entityOverrides == null) {
			return;
		}

		for (int i = 0; i < entityOverrides.Count; i++) {
			FloatAttribute attr = entityOverrides[i];
			floatAttrs[attr.attributeName].UpdateAttributeValues(attr);
		}

		if (GetComponent<CharacterMover>()) {
			GetComponent<CharacterMover>().enabled = true;
		}

		rbody = GetComponent<Rigidbody2D>();
	}

	//When entering a new liquid collider, we're either entering a liquid from dry space or from another liquid collider
	//If we're coming from dry space, we only update the primary boolean; if entering another liquid collider, update the secondary boolean to cancel the upcoming TriggerExit call
	void OnTriggerEnter2D(Collider2D col) {
		if (col.tag == "Liquid" && !enteredLiquid) {
			MultiplyFloatMult(gravityAttrName, LiquidController.liquidGravity);
			enteredLiquid = true;
			rbody.velocity *= .5f;
		} else if (col.tag == "Liquid" && enteredLiquid) {
			inLiquid = true;
		}
	}

	//When exiting a liquid collider, we're either entering a dry space or another liquid collider
	//If coming from a liquid to another liquid, reset the secondary boolean for the next TriggerExit call; if entering a dry space, reset the primary boolean
	void OnTriggerExit2D(Collider2D col) {
		if (col.tag == "Liquid" && enteredLiquid && !inLiquid) {
			MultiplyFloatMult(gravityAttrName, 1/LiquidController.liquidGravity);
			enteredLiquid = false;
		} else if (col.tag == "Liquid" && inLiquid) {
			inLiquid = false;
		}
	}
	
	public void Damage(float damage) {
		IncrementFloat(EntityManager.Instance.healthAttributeName, -(damage - (GetFloat(EntityManager.Instance.armorAttributeName) * .5f)));
	}
	
	public int AddNewFloat(FloatAttribute newAttr) {
		if (!floatAttrs.ContainsKey(newAttr.attributeName)) {
			return -1;
		}
			
		else {
			floatAttrs[newAttr.attributeName] = newAttr;
			return 1;
		}
	}
	
	void UpdateGravity() {
		FloatAttribute gAttr = floatAttrs[gravityAttrName];
		rbody.gravityScale = gAttr.GetValue();
	}

	public int SetFloat(string name, float newVal) {
		if (floatAttrs.ContainsKey(name)) {
			FloatAttribute attr = floatAttrs[name];
			attr.SetValue(newVal);

			if (name == gravityAttrName) {
				UpdateGravity();
			}
			return 1;
		}
		
		else {
			return -1;
		}
	}

    public int IncrementFloat(string name, float additionVal) {
        if (floatAttrs.ContainsKey(name)) {
			FloatAttribute attr = floatAttrs[name];
            float attrValue = attr.GetValue();
			attr.SetValue(attrValue + additionVal);

			if (name == gravityAttrName) {
				UpdateGravity();
			}
			return 1;
		} 
        
        else {
            return -1;
        }
    }

    public int MultiplyFloat(string name, float multiplyVal) {
        if (floatAttrs.ContainsKey(name)) {
			FloatAttribute attr = floatAttrs[name];
            float attrValue = attr.GetValue();
			attr.SetValue(attrValue * multiplyVal);

			if (name == gravityAttrName) {
				UpdateGravity();
			}
			return 1;
		} 
        
        else {
            return -1;
        }
    }
	
	public float GetFloat(string name) {
		if (floatAttrs.ContainsKey(name)) {
			return floatAttrs[name].GetValue();	
		}
		
		return -1f;
	}
	
	public int SetFloatMult(string name, float newMult) {
		if (floatAttrs.ContainsKey(name)) {
			FloatAttribute attr = floatAttrs[name];
			
			attr.SetMult(newMult);

			if (name == gravityAttrName) {
				UpdateGravity();
			}
			return 1;
		}
		
		else {
			return -1;
		}
	}

	public int MultiplyFloatMult(string name, float multIncrease) {
		if (floatAttrs.ContainsKey(name)) {
			FloatAttribute attr = floatAttrs[name];
			
			attr.IncreaseMult(multIncrease);

			if (name == gravityAttrName) {
				UpdateGravity();
			}
			return 1;
		}
		
		else {
			return -1;
		}
	}

	public int ResetFloat(string name) {
		if (floatAttrs.ContainsKey(name)) {
			FloatAttribute attr = floatAttrs[name];
			attr.ResetAttr();

			if (name == gravityAttrName) {
				UpdateGravity();
			}
			return 1;
		}
		
		else {
			return -1;
		}
	}
}

[System.Serializable]
public class FloatAttribute {
	public string attributeName;
	[SerializeField] public float initialValue = 0;
	[HideInInspector] public float currentValue;
	[SerializeField] public float initialMult = 1;
	[HideInInspector] public float currentMult;
	
	public FloatAttribute(string _attributeName, float _initialValue, float _initialMult = 1f) {
		attributeName = _attributeName;
		initialValue = _initialValue;
		currentValue = _initialValue;
		initialMult = _initialMult;
		currentMult = _initialMult;
	}

    public FloatAttribute(FloatAttribute _toCopy) {
        attributeName = _toCopy.attributeName;
		initialValue = _toCopy.initialValue;
		currentValue = _toCopy.initialValue;
		initialMult = _toCopy.initialMult;
		currentMult = _toCopy.initialMult;
    }

	public void UpdateAttributeValues(FloatAttribute _toCopy) {
		attributeName = _toCopy.attributeName;
		initialValue = _toCopy.initialValue;
		currentValue = _toCopy.initialValue;
		initialMult = _toCopy.initialMult;
		currentMult = _toCopy.initialMult;
	}
	
	public void SetValue(float _value) {
		currentValue = _value;
	}
	
	public float GetValue() {
		return currentValue * currentMult;	
	}
	
	public void SetMult(float _mult) {
		currentMult = _mult;	
	}
	
	public void IncreaseMult(float _increase) {
		currentMult *= _increase;	
	}
	
	public float GetMult() {
		return currentMult;	
	}
	
	public void ResetAttr() {
		currentValue = initialValue;
		currentMult = initialMult;
	}
	
}