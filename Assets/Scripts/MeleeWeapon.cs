using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Basic Melee Weapon")]
public class MeleeWeapon : Weapon
{
    public float swingsPerSecond = 1;
    public bool fullAuto = false;

    //private MeleeShooter meleeShooter;

    public override void Initialize(GameObject obj) {
        //meleeShooter = obj.GetComponent<MeleeShooter>();
        //meleeShooter.Initialize();
    }

    public override void FireWeapon() {
        //meleeShooter.Fire();
    }
}
