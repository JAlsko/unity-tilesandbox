using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>{
	public void ActivateEffect(Entity ent, Effect eff) {
		eff.Activate(ent);
		Helpers.Invoke(this, eff.Deactivate, ent, eff.effectLength);
	}	
}

public enum AttributeType {
    Float = 0,
    Bool = 1,
}

public enum AttributeChangeOp {
    Add = 0,
    Multiply = 1,
    Set = 2,
}

[Serializable]
public class FloatAttributeChange {
    [Tooltip("The name of the attribute to change.")]
    public string attributeName;
    [Tooltip("How do we change the attribute (+/*/=).")]
    public AttributeChangeOp changeOp;
    [Tooltip("How much to change the attribute.")]
    public float valueChange;
    [Tooltip("Does this change reverse once its associated effect has ended?")]
    public bool reverseChangeOnEffectTermination;
    [HideInInspector] public float totalValueChange;
}

