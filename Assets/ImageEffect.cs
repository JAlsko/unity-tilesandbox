using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ImageEffect : MonoBehaviour
{
    public Shader effectShader;
    Material effectMat;

    public int effectIterations = 1;

    RenderTexture[] textures = new RenderTexture[16];

    void OnRenderImage (RenderTexture source, RenderTexture destination) {
        if (effectMat == null) {
			effectMat = new Material(effectShader);
			effectMat.hideFlags = HideFlags.HideAndDontSave;
		}

		RenderTextureFormat format = source.format;

		RenderTexture currentDestination = textures[0] =
			RenderTexture.GetTemporary(source.width, source.height, 0, format);
		Graphics.Blit(source, currentDestination, effectMat);
		RenderTexture currentSource = currentDestination;

		int i = 1;
		for (; i < effectIterations; i++) {
			currentDestination = textures[i] =
				RenderTexture.GetTemporary(source.width, source.height, 0, format);
			Graphics.Blit(currentSource, currentDestination, effectMat);
			currentSource = currentDestination;
		}

		Graphics.Blit(currentSource, destination, effectMat);
		RenderTexture.ReleaseTemporary(currentSource);
	}
}
