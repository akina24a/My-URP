﻿using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

partial class PostFXStack 
{
    
    const string fxaaQualityLowKeyword = "FXAA_QUALITY_LOW",  fxaaQualityMediumKeyword = "FXAA_QUALITY_MEDIUM";
    
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
    bool  DoBloom (int sourceId)
    {
        //buffer.BeginSample("Bloom");
        BloomSettings bloom = settings.Bloom;
        // int width = bufferSize.x / 2, height =  bufferSize.y  / 2;
        int width, height;
        if (bloom.ignoreRenderScale) {
            width = camera.pixelWidth / 2;
            height = camera.pixelHeight / 2;
        }
        else {
            width = bufferSize.x / 2;
            height = bufferSize.y / 2;
        }

        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimit * 2|| width < bloom.downscaleLimit* 2)
        {
            // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            // buffer.EndSample("Bloom");
            return false;
        }
        buffer.BeginSample("Bloom");
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        RenderTextureFormat format =useHDR ?  RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        buffer.GetTemporaryRT( bloomPrefilterId,bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, format );
        Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? Pass.BloomPrefilterFireflies :Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        int fromId = bloomPrefilterId, toId = bloomPyramidId+ 1;
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
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == BloomSettings.Mode.Additive) 
        {
            combinePass =finalPass =  Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }

        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, combinePass);
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
        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, sourceId);
        buffer.GetTemporaryRT( bloomResultId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, format );
        // Draw(fromId, BuiltinRenderTextureType.CameraTarget,  finalPass);
        Draw(fromId, bloomResultId,  finalPass);
        buffer.ReleaseTemporaryRT(fromId);
      

        buffer.EndSample("Bloom");
        return true;
    }
    
    void ConfigureWhiteBalance () {
        WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
        buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
            whiteBalance.temperature, whiteBalance.tint
        ));
    }
    void ConfigureChannelMixer () {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
    }
    
    void ConfigureShadowsMidtonesHighlights () {
        ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalVector(smhRangeId, new Vector4(
            smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
        ));
    }

    void ConfigureFXAA () {
        if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Low) {
            buffer.EnableShaderKeyword(fxaaQualityLowKeyword);
            buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
        }
        else if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Medium) {
            buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
            buffer.EnableShaderKeyword(fxaaQualityMediumKeyword);
        }
        else {
            buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
            buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
        }
        buffer.SetGlobalVector(fxaaConfigId, new Vector4(
            fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending
        ));
    }
    
    void DoFinal(int sourceId) {                  
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHighlights();
        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT( colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR );
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4( lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f) ));
        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = mode < 0 ? Pass.Copy : Pass.ColorGradingNone  + (int)mode;
        buffer.SetGlobalFloat( colorGradingLUTInLogId, useHDR && pass != Pass.ColorGradingNone ? 1f : 0f  );
        Draw(sourceId, colorGradingLUTId, pass);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f) );
        //final blend mode for color grading is set to One Zero.
        buffer.SetGlobalFloat(finalSrcBlendId, 1f);
        buffer.SetGlobalFloat(finalDstBlendId, 0f);
        if (fxaa.enabled) {
            ConfigureFXAA();
            // buffer.SetGlobalVector(fxaaConfigId, new Vector4(fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending));
            buffer.GetTemporaryRT( colorGradingResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default );
            Draw(sourceId, colorGradingResultId, keepAlpha ? Pass.ApplyColorGrading : Pass.ApplyColorGradingWithLuma);
        }
        if (bufferSize.x == camera.pixelWidth) {
            if (fxaa.enabled) {
                DrawFinal(colorGradingResultId,  keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else {
                DrawFinal(sourceId, Pass.ApplyColorGrading);
            }
        }
        else {
            buffer.GetTemporaryRT( finalResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default  );
            if (fxaa.enabled) {
                Draw(colorGradingResultId, finalResultId, keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else {
                Draw(sourceId, finalResultId, Pass.ApplyColorGrading);
            }
            bool bicubicSampling =
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                bufferSize.x < camera.pixelWidth;
            buffer.SetGlobalFloat(copyBicubicId, bicubicSampling  ? 1f : 0f);
            DrawFinal(finalResultId, Pass.FinalRescale);
            buffer.ReleaseTemporaryRT(finalResultId);
        }
        buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }
    
        
    void ConfigureColorAdjustments () {
        ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
            Mathf.Pow(2f, colorAdjustments.postExposure),
            colorAdjustments.contrast * 0.01f + 1f,
            colorAdjustments.hueShift * (1f / 360f),
            colorAdjustments.saturation * 0.01f + 1f
        ));
        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }
    
    void ConfigureSplitToning () {
        SplitToningSettings splitToning = settings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalColor(splitToningShadowsId, splitColor);
        buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
    }

}