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

    bool useHDR;
    enum Pass 
    {
        Copy,
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomPrefilter,
        BloomPrefilterFireflies,
        BloomScatter,
        BloomScatterFinal,
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard,

    }
    
    int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
        bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
        bloomResultId = Shader.PropertyToID("_BloomResult"),
        bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
        fxSourceId = Shader.PropertyToID("_PostFXSource"),
        fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    public void Setup( ScriptableRenderContext context, Camera camera, PostFXSettings settings, bool useHDR )
    {
        this.useHDR = useHDR;
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        if (DoBloom(sourceId)) {
            DoToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else {
            DoToneMapping(sourceId);
        }
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