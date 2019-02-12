using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PostProcessingCam : MonoBehaviour
{
    public Material postProcessingMat;

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, postProcessingMat);
    }
}
