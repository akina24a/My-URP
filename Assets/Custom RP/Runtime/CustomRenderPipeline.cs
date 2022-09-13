using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{

    CameraRenderer renderer = new CameraRenderer();
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSettings;
    public CustomRenderPipeline (bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,bool useLightsPerObject, ShadowSettings shadowSettings ) 
    {
        this.shadowSettings = shadowSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.useLightsPerObject = useLightsPerObject;
        InitializeForEditor();
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //simple to support different rendering approaches per camera
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing,useLightsPerObject,shadowSettings);
        }
    }

}