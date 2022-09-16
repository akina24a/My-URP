using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    const string bufferName = "Post FX";
    public bool IsActive => settings != null;

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    Camera camera;

    PostFXSettings settings;

    enum Pass 
    {
        BloomHorizontal,
        BloomVertical,
        Copy
    }
    
    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    public void Setup( ScriptableRenderContext context, Camera camera, PostFXSettings settings )
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        DoBloom(sourceId);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    void Draw ( RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass )
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(  to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store );
        buffer.DrawProcedural( Matrix4x4.identity, settings.Material, (int)pass,  MeshTopology.Triangles, 3 );
    }
}