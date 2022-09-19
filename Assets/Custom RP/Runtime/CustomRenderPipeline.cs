using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{

    CameraRenderer renderer = new CameraRenderer();
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSettings;
    PostFXSettings postFXSettings;
    bool allowHDR;
    public CustomRenderPipeline (bool allowHDR,bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,bool useLightsPerObject, ShadowSettings shadowSettings ,
		PostFXSettings postFXSettings) 
    {
        this.allowHDR = allowHDR;
        this.shadowSettings = shadowSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.useLightsPerObject = useLightsPerObject;
        this.postFXSettings = postFXSettings;
        InitializeForEditor();
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //simple to support different rendering approaches per camera
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, allowHDR,
                useDynamicBatching, useGPUInstancing,useLightsPerObject,shadowSettings, postFXSettings);
        }
    }

}