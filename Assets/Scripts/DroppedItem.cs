﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    private ItemObject thisItem;
    public SpriteRenderer itemIcon;
    public BoxCollider2D triggerCol;
    public BoxCollider2D physCol;
    public Rigidbody2D rbody;
    public DynamicLightSource itemLight;

    public float magnetizeVelocity = 250f;
    public float minVelocity = 15f;
    private Transform magnetizeTarget = null;
    private int magnetizeTargetID = 0;

    void FixedUpdate() {
        if (magnetizeTarget == null) {
            return;
        }
        
        //physCol.transform.position = Vector3.Lerp(physCol.transform.position, magnetizeTarget.position, Time.deltaTime * magnetizeForce);
        Vector2 newVelocity = (magnetizeTarget.position - transform.position) * magnetizeVelocity * Time.fixedDeltaTime;
        float magnetVelocityMagnitude = newVelocity.magnitude;
        newVelocity = (Mathf.Abs(magnetVelocityMagnitude) < minVelocity) ? (newVelocity * minVelocity) / magnetVelocityMagnitude: newVelocity;
        rbody.velocity = newVelocity;
        //rbody.AddForce((magnetizeTarget.position - transform.position) * magnetizeForce * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D col) {
        if (col.tag == "ChunkDetection") {
            Transform newChunkParent = col.transform.parent;
            //physCol.gameObject.SetActive(newChunkParent.gameObject.activeInHierarchy);
            physCol.transform.SetParent(newChunkParent);
        }
    }

    public void UpdateMagnetTarget(Transform newTarget, int newTargetID) {
        if (newTarget == null) {
            if (newTargetID == magnetizeTargetID) {
                magnetizeTarget = null;
                magnetizeTargetID = 0;
                physCol.enabled = true;
            }
        } else {
            rbody.velocity = Vector2.zero;

            magnetizeTarget = newTarget;
            magnetizeTargetID = newTargetID;
            physCol.enabled = false;
        }
    }

    public string GetDescriptionText() {
        return ItemManager.GetItem(thisItem.name).name + " (" + thisItem.currentStack + ")"; 
    }

    public ItemObject GetDroppedItem() {
        return thisItem;
    }

    public void AddForceToItem(Vector2 forceToAdd) {
        rbody.AddForce(forceToAdd);
    }

    public void InitializeItem(ItemObject itemObj) {
        thisItem = itemObj;
        Item itemInfo = ItemManager.GetItem(thisItem.name);
        itemIcon.sprite = itemInfo.icon;
        float colSize = itemInfo.colliderSize;
        triggerCol.size = Vector2.one * colSize;
        physCol.size = Vector2.one * colSize;
        EnableCollision();
        physCol.transform.gameObject.SetActive(true);

        float lightVal = ItemManager.GetItem(itemInfo.name).lightStrength;
        if (lightVal > 0) {
            Color lightColor = ItemManager.GetItem(itemInfo.name).lightColor;
            itemLight.startLightStrength = lightVal;
            itemLight.lightColor = lightColor;
            itemLight.enabled = true;
            itemLight.EnableLight();
        }
    }

    public void HideDroppedItem() {
        thisItem = null;
        UpdateMagnetTarget(null, magnetizeTargetID);
        //physCol.transform.SetParent(null);
        physCol.transform.gameObject.SetActive(false);
        if (itemLight.enabled)
            itemLight.DisableLight();
    }

    public int CombineDroppedItems(int count) {
        int maxStack = ItemManager.GetItem(thisItem.name).maxStackSize;
        maxStack -= thisItem.currentStack;
        int leftOverStack = Mathf.Max(0, count - maxStack);
        int countToTake = count - leftOverStack;
        thisItem.currentStack += countToTake;

        return leftOverStack;
    }

    public void EnableCollision() {
        triggerCol.enabled = true;
    }

    public void DisableCollision() {
        triggerCol.enabled = false;
    }
}
