﻿using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public partial class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true,useLightsPerObject = true;
    [SerializeField]
    ShadowSettings shadows = default;
    [SerializeField]
    PostFXSettings postFXSettings = default;
  
    [SerializeField]
    CameraBufferSettings cameraBuffer = new CameraBufferSettings {
        allowHDR = true
    };
    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 }

    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
    [SerializeField]
    Shader cameraRendererShader = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer,useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject,shadows, postFXSettings, (int)colorLUTResolution,
            cameraRendererShader);
    }
}
