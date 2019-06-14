using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RNGManager : Singleton<RNGManager> {
    
	public int GetRandomInt(int min, int max) {
		return UnityEngine.Random.Range(min, max);
	}
	
	public float GetRandomFloat() {
		return UnityEngine.Random.Range(0, 1f);
	}
}

public class Choice<T> {
	public T choiceObj;
	public float chance = 0;
	public int fractionChance = 1;
	
	public Choice(float _chance) {
		this.chance = _chance;
	}
	
	public Choice(int _fractionChance) {
		this.chance = (float)1/fractionChance;	
	}
	
	public virtual T GetRandomizedChoice() {
		return choiceObj;
	}
}


public class DropItem : Choice<ItemObject> {
	public string item = "nullitem";
	public int minCount = 1;
	public int maxCount = 1;
	
	public DropItem(string _item, int _minCount, int _maxCount, float _chance = 1f) : base(_chance) {
		this.item = _item;
		this.minCount = _minCount;
		this.maxCount = _maxCount;
	}
	
	public DropItem(string _item, int _minCount, int _maxCount, int _fractionChance) : base(_fractionChance) {
		this.item = _item;
		this.minCount = _minCount;
		this.maxCount = _maxCount;
	}
	
	public override ItemObject GetRandomizedChoice () {
		return new ItemObject(item, RNGManager.Instance.GetRandomInt(minCount, maxCount));
	}
}


public class RandomSelection<T> {
	public List<Choice<T>> selectionChoices = new List<Choice<T>>();
	public float chance = 0;
	
	public RandomSelection(float _chance) {
		this.chance = _chance;
		selectionChoices = new List<Choice<T>>();
	}
	
	public T GetRandomizedDrop() {
		float rndNum = RNGManager.Instance.GetRandomFloat();
		
		for (int i = 0; i < selectionChoices.Count; i++) {
			if (rndNum <= selectionChoices[i].chance) {
				return selectionChoices[i].GetRandomizedChoice();	
			}
			
			rndNum -= selectionChoices[i].chance;
		}
		
		int randomIndex = RNGManager.Instance.GetRandomInt(0, selectionChoices.Count-1);
		return selectionChoices[randomIndex].GetRandomizedChoice();
	}
}


public class RandomSet<T> {
	public List<RandomSelection<T>> dropSelections = new List<RandomSelection<T>>();
	
	public List<T> GetRandomChoices() {
		List<T> randomChoices = new List<T>();
		for (int i = 0; i < dropSelections.Count; i++) {
			if (RNGManager.Instance.GetRandomFloat() < dropSelections[i].chance) {
				T newChoice = dropSelections[i].GetRandomizedDrop();
				randomChoices.Add(newChoice);
			}
		}
		return randomChoices;
	}
}