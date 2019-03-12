using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicLightSource : MonoBehaviour
{
    public float startLightStrength = 1;
    private float curLightStrength;
    public Color lightColor = Color.black;
    [Range(0, 1f)]
    public float movementFadeAmount = 1f;
    public float minLightAmount = 0.1f;

    private float distanceTraveled = 0;
    private LightSource lightSource;
    private Vector2Int curPos;

    public void EnableLight()
    {
        curPos = GetSimplifiedPosition();
        curLightStrength = startLightStrength;
        GetNewLightSource();
        UpdateLight();
    }

    void FixedUpdate() {
        Vector2Int newPos = GetSimplifiedPosition();
        if (newPos != curPos) {            
            distanceTraveled += (Mathf.Abs(curPos.x-newPos.x) + Mathf.Abs(curPos.y-newPos.y));
            curLightStrength = GetUpdatedLightStrength();

            if (curLightStrength < minLightAmount) {
                curLightStrength = 0;
                UpdateLight();
                this.enabled = false;
            } else {
                UpdateLight();
            }
            curPos = newPos;
        }
    }

    Vector2Int GetSimplifiedPosition() {
        return new Vector2Int((int)transform.position.x, (int)transform.position.y);
    }

    float GetUpdatedLightStrength() {
        return startLightStrength - (distanceTraveled * movementFadeAmount);
    }

    void UpdateLight() {
        LightController.Instance.UpdateDynamicLight(this);
    }

    public LightSource GetLightSource() {
        return lightSource;
    }

    public LightSource GetNewLightSource() {
        lightSource = LightController.Instance.CreateLightSource(new Vector3Int((int)transform.position.x, (int)transform.position.y, 0), lightColor, curLightStrength, false);
        return lightSource;
    }

    public void RemoveLightSource() {
        if (lightSource != null)
            Destroy(lightSource.gameObject);
        lightSource = null;
    }

    public void DisableLight() {
        LightController.Instance.RemoveDynamicLight(this);
        RemoveLightSource();
        this.enabled = false;
    }
}
