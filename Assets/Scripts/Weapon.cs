using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : ScriptableObject
{
    public float damage = 1f;

    public abstract void Initialize(GameObject obj);
    public abstract void FireWeapon();
}
