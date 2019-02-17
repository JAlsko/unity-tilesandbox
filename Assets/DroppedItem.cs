using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    private ItemObject thisItem;
    public SpriteRenderer itemIcon;
    public BoxCollider2D triggerCol;
    public BoxCollider2D physCol;
    public Rigidbody2D rbody;

    public string GetDescriptionText() {
        return ItemManager.GetItem(thisItem.id).name + " (" + thisItem.currentStack + ")"; 
    }

    public ItemObject GetDroppedItem() {
        return thisItem;
    }

    public void AddForceToItem(Vector2 forceToAdd) {
        rbody.AddForce(forceToAdd);
    }

    public void InitializeItem(ItemObject itemObj) {
        thisItem = itemObj;
        Item itemInfo = ItemManager.GetItem(thisItem.id);
        itemIcon.sprite = itemInfo.icon;
        float colSize = itemInfo.colliderSize;
        triggerCol.size = Vector2.one * colSize;
        physCol.size = Vector2.one * colSize;
    }

    public void HideDroppedItem() {
        thisItem = null;
        physCol.transform.gameObject.SetActive(false);
    }

    public void EnableCollision() {
        triggerCol.enabled = true;
    }

    public void DisableCollision() {
        triggerCol.enabled = false;
    }
}
