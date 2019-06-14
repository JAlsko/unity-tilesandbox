using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "New Effect", menuName = "Effects/Stat Change")]
public class Effect : ScriptableObject {
    [Tooltip("Name of effect for displaying to the player.")]
    public string effectName;

    [Tooltip("The icon that will be displayed for this effect.")]
    public Sprite effectIcon;

    [Tooltip("Effect's time length in seconds.")]
	public float effectLength = 0;
    
    [HideInInspector] public float effectFrequency = 0;
	[HideInInspector] public int totalEffectTicks {
        get {
            if (effectFrequency <= 0) {
                return 1;
            }
            
            return (int) (effectLength / effectFrequency);
        }
    }

    [Header("Effects Per Tick")]
    [Tooltip("The entity attribute changes that will be made each tick of this effect.")]
    public List<FloatAttributeChange> attrChanges = new List<FloatAttributeChange>();
	

	public void Awake() {
	}
	
	public void Activate(Entity ent) {
		if (totalEffectTicks > 1) {
            EffectManager.Instance.InvokeLimited(TickEffect, ent, effectFrequency, totalEffectTicks);
        } else {
            TickEffect(ent);
        }
	}
	public void TickEffect(Entity ent) {
        for (int i = 0; i < attrChanges.Count; i++) {
            FloatAttributeChange attrChange = attrChanges[i];
            switch (attrChange.changeOp) {
                case AttributeChangeOp.Add:
                    ent.IncrementFloat(attrChange.attributeName, attrChange.valueChange);
                    attrChange.totalValueChange += attrChange.valueChange;
                break;
                
                case AttributeChangeOp.Multiply:
                    ent.MultiplyFloat(attrChange.attributeName, attrChange.valueChange);
                    attrChange.totalValueChange *= attrChange.valueChange;
                break;

                case AttributeChangeOp.Set:
                    ent.SetFloat(attrChange.attributeName, attrChange.valueChange);
                    attrChange.totalValueChange = attrChange.valueChange;
                break;

                default:
                break;
            }
        }
    }
	public void Deactivate(Entity ent) {
        for (int i = 0; i < attrChanges.Count; i++) {
            FloatAttributeChange attrChange = attrChanges[i];
            if (attrChange.reverseChangeOnEffectTermination) {
                ent.IncrementFloat(attrChange.attributeName, -attrChange.totalValueChange);
            }
            attrChange.totalValueChange = 0;
        }
    }
}