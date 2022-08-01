using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{

    CameraRenderer renderer = new CameraRenderer();
    public CustomRenderPipeline () {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //simple to support different rendering approaches per camera
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }

}