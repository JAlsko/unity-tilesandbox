using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientPanel : MonoBehaviour
{
    public Image ingredientIcon;
    public TextMeshProUGUI ingredientCount;

    public void HideIngredientPanel() {
        this.gameObject.SetActive(false);
    }

    public void ShowIngredientPanel() {
        this.gameObject.SetActive(true);
    }
}
