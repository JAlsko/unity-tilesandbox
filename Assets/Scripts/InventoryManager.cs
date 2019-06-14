using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : Singleton<InventoryManager> {
	public Dictionary<int, Inventory> allInventories;
	
	public void InitializeInventories() {
		allInventories = new Dictionary<int, Inventory>();
		InventoryManager.Instance.AddNewInventory(42, new Inventory(24, "Bank"));
	}

	public int AddNewInventory(int id, Inventory newInv) {
		if (allInventories.ContainsKey(id) || newInv == null) {
			return -1;
		}
		
		allInventories[id] = newInv;
		
		return 1;
	}
	
	public int SetInventoryItem(int invID, int itemIndex, ItemObject newItem) {
		if (!allInventories.ContainsKey(invID)) {
			return -1;
		}
		
		Inventory selectedInventory = allInventories[invID];
		return selectedInventory.SetItem(itemIndex, newItem);
	}

    public int DisplayInventory(int invID) {
        if (!allInventories.ContainsKey(invID)) {
			return -1;
		}

        Inventory invToDisplay = allInventories[invID];
        return 1;
    }

	public Inventory GetInventory(int invID) {
		if (!allInventories.ContainsKey(invID)) {
			Debug.LogError("Inventory " + invID + " not found!");
			return null;
		}

		return allInventories[invID];
	}
}