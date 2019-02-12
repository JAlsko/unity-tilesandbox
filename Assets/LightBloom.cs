﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class LightBloom : MonoBehaviour
{
    [Range(1, 16)]
    public int interations = 1;

    public Shader bloomShader;

    RenderTexture[] textures = new RenderTexture[16];

    [NonSerialized]
    Material bloom;

    const int BoxDownPass = 0;
    const int BoxUpPass = 1;
    const int ApplyBloomPass = 2;

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (bloom == null) {
            bloom = new Material(bloomShader);
            bloom.hideFlags = HideFlags.HideAndDontSave;
        }

        int width = source.width/2;
        int height = source.height/2;
        RenderTextureFormat format = source.format;

        RenderTexture currentDestination = textures[0] = RenderTexture.GetTemporary( width, height, 0, format );

        Graphics.Blit(source, currentDestination, bloom, BoxDownPass);
        RenderTexture currentSource = currentDestination;

        int i = 1;
        for (; i < interations; i++) {
            width /= 2;
            height /= 2;
            if (height < 2) {
                break;
            }
            currentDestination = textures[i] = RenderTexture.GetTemporary(width, height, 0, format);
            Graphics.Blit(currentSource, currentDestination, bloom, BoxDownPass);
            currentSource = currentDestination;
        }

        for (i -= 2; i >= 0; i--) {
            currentDestination = textures[i];
            textures[i] = null;
            Graphics.Blit(currentSource, currentDestination, bloom, BoxUpPass);
            RenderTexture.ReleaseTemporary(currentSource);
            currentSource = currentDestination;
        }

        bloom.SetTexture("_SourceTex", source);
        Graphics.Blit(currentSource, destination, bloom, ApplyBloomPass);
        RenderTexture.ReleaseTemporary(currentSource);
    }
}
