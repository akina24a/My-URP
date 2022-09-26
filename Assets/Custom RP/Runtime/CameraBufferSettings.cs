using UnityEngine;

[System.Serializable]
public struct CameraBufferSettings {

    public bool allowHDR;

    public bool  copyColor, copyColorReflection, copyDepth, copyDepthReflections;
    
    public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }

    public BicubicRescalingMode bicubicRescaling;
    
    [Range(CameraRenderer.renderScaleMin, CameraRenderer.renderScaleMax)]
    public float renderScale;
}