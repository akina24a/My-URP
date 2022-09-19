using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

partial class PostFXStack 
{
    const int maxBloomPyramidLevels = 16;
    int bloomPyramidId;
    partial void ApplySceneViewState ();

#if UNITY_EDITOR

    partial void ApplySceneViewState () {
        if (
            camera.cameraType == CameraType.SceneView &&
            !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects
        ) {
            settings = null;
        }
    }

#endif
    public PostFXStack () {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevels * 2; i++) {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }
    void DoBloom (int sourceId)
    {
        buffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;


        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimit * 2|| width < bloom.downscaleLimit* 2)
        {
            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            buffer.EndSample("Bloom");
            return;
        }
        
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        RenderTextureFormat format =useHDR ?  RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        buffer.GetTemporaryRT( bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format );
        Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? Pass.BloomPrefilterFireflies :Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        int fromId = bloomPrefilterId, toId = bloomPyramidId;
        int i;
        for (i = 0; i < bloom.maxIterations; i++) 
        {
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
            {
                break;
            }
            int midId = toId - 1;
            buffer.GetTemporaryRT( midId, width, height, 0, FilterMode.Bilinear, format );   
            buffer.GetTemporaryRT( toId, width, height, 0, FilterMode.Bilinear, format );
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        buffer.SetGlobalFloat( bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f );
        // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        buffer.SetGlobalFloat(bloomIntensityId, 1f);
        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, Pass.BloomCombine);
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }
        buffer.SetGlobalFloat(bloomIntensityId, bloom.intensity);
        buffer.SetGlobalTexture(fxSource2Id, sourceId);
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        buffer.ReleaseTemporaryRT(fromId);
        buffer.EndSample("Bloom");
    }
}