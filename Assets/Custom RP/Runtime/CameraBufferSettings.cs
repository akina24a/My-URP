using UnityEngine;
using System;

[Serializable]
public struct CameraBufferSettings {

    public bool allowHDR;

    public bool  copyColor, copyColorReflection, copyDepth, copyDepthReflections;
    
    public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }

    public BicubicRescalingMode bicubicRescaling;
    
    [Range(CameraRenderer.renderScaleMin, CameraRenderer.renderScaleMax)]
    public float renderScale;
    
    [Serializable]
    public struct FXAA {

        public bool enabled;
    }

    public FXAA fxaa;
}