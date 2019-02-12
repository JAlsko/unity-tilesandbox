using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ImageEffect : MonoBehaviour
{
    public Shader effectShader;

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Material effectMat = new Material(effectShader);
        Graphics.Blit(source, destination, effectMat);
    }
}
