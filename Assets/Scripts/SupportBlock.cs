using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SupportSide {
	top = 0,
	left = 1,
	right = 2,
	bottom = 3,
	back = 4,
}

public class SupportTile {
	public bool active = false;
	public bool anySupport = false;
	private bool[] currentSupport = new bool[5];
	public bool[] requiredSupport = new bool[5];
	
	public bool RemoveSide(int removedSide) {
		currentSupport[removedSide] = false;
		return RemainingSupport();
	}
	
	public void AddSide(int newSide) {
		currentSupport[newSide] = true;
	}
	
	bool RemainingSupport() {
		for (int i = 0; i < currentSupport.Length; i++) {
			if (currentSupport[i] == requiredSupport[i]) {
				if (anySupport)
					return true;
			} else if (requiredSupport[i]) {
				if (!anySupport)
					if (!currentSupport[i])
						return false;
			}
		}
		
		return !anySupport;
	}
}