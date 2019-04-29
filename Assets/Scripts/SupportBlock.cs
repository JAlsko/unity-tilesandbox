using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SupportEdge {
	top = 0,
	left = 1,
	right = 2,
	bottom = 3,
	back = 4,
}

public class SupportTile {
	public bool requiresSupport = false;
	public bool takesAnySupport = false;
	private bool[] currentSupport = new bool[5];
	public bool[] givenSupport = new bool[5];
	public bool[] neededSupport = new bool[5];
	
	public SupportTile(bool requiresSupport = false, bool takesAnySupport = false) {
		this.requiresSupport = requiresSupport;
		this.takesAnySupport = takesAnySupport;
		for (int i = 0; i < 5; i++) {
			currentSupport[i] = false;
			givenSupport[i] = false;
			neededSupport[i] = false;
		}
	}

	public SupportTile(bool requiresSupport, bool takesAnySupport, bool[] neededSupport) {
		this.requiresSupport = requiresSupport;
		this.takesAnySupport = takesAnySupport;
		this.neededSupport = neededSupport;
		for (int i = 0; i < 5; i++) {
			currentSupport[i] = false;
			givenSupport[i] = false;
			neededSupport[i] = false;
		}
	}

	public SupportTile(SupportTile toCopy) {
		this.requiresSupport = toCopy.requiresSupport;
		this.takesAnySupport = toCopy.takesAnySupport;
		this.neededSupport = toCopy.neededSupport;
		this.givenSupport = toCopy.givenSupport;
		if (toCopy.currentSupport == null) {
			for (int i = 0; i < 5; i++) {
				currentSupport[i] = false;
			}
		} else {
			this.currentSupport = toCopy.currentSupport;
		}
	}

	public void UpdateSupportTile(SupportTile toCopy) {
		this.requiresSupport = toCopy.requiresSupport;
		this.takesAnySupport = toCopy.takesAnySupport;
		this.neededSupport = toCopy.neededSupport;
		this.givenSupport = toCopy.givenSupport;
	}

	public bool RemoveSide(int removedSide) {
		currentSupport[removedSide] = false;
		Debug.Log("Removing side " + removedSide);
		bool remainingSupport = RemainingSupport();
		if (remainingSupport)
			Debug.Log("We still have support!");
		else
			Debug.Log("No support!");
		return remainingSupport;
	}
	
	public void AddSide(int newSide) {
		currentSupport[newSide] = true;
		//Debug.Log("Adding side " + relativeSide);
	}
	
	bool RemainingSupport() {
		for (int i = 0; i < currentSupport.Length; i++) {
			if (currentSupport[i] && neededSupport[i]) {
				if (takesAnySupport) {
					Debug.Log("Have some support at direction " + i);
					return true;
				}
			} else if (neededSupport[i]) {
				if (!takesAnySupport)
					if (!currentSupport[i]) {
						Debug.Log("Missing vital support at direction " + i);
						return false;
					}
			}
		}
		
		Debug.Log("Finished support check!");
		return !takesAnySupport;
	}

	public static int GetInverseSide(int inputSide) {
		switch (inputSide) {
			case 0:
				return 3;
			case 1:
				return 2;
			case 2:
				return 1;
			case 3:
				return 0;
			default:
				return 4;
		}
	}
}