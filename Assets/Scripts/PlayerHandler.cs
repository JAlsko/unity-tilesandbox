using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandler : Singleton<PlayerHandler>
{
    public PlayerInventory pInv;
    public PlayerInput pInput;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public Vector2 GetMainPlayerMousePos() {
        return pInput.GetMousePos();
    }
}
