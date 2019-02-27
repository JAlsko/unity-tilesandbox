using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Basic Raycast Weapon")]
public class RaycastWeapon : Weapon
{
    public float roundsPerSecond = 1;
    public bool fullAuto = false;
    public float range = 50f;
    public Color trailColor = Color.white;

    //private RaycastShooter rcShooter;

    public override void Initialize(GameObject obj) {
        //rcShooter = obj.GetComponent<RaycastShooter>();
        //rcShooter.Initialize();
    }

    public override void FireWeapon() {
        //rcShooter.Fire();
    }
}
