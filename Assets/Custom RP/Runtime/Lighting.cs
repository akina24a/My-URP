using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
public class Lighting {

    const string bufferName = "Lighting";

    CommandBuffer buffer = new CommandBuffer {
        name = bufferName
    };
    static int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightShadowDataId =
            Shader.PropertyToID("_DirectionalLightShadowData");
    
    static Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],
        dirLightDirections = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount];
    
    static int
        otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
        otherLightColorsId = Shader.PropertyToID("_OtherLightColors"),
        otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");

    static Vector4[]
        otherLightColors = new Vector4[maxOtherLightCount],
        otherLightPositions = new Vector4[maxOtherLightCount];
    
    
    CullingResults cullingResults;
    const int maxDirLightCount = 4, maxOtherLightCount = 64;
    Shadows shadows = new Shadows();
    
    public void Setup (ScriptableRenderContext context, CullingResults cullingResults,
        ShadowSettings shadowSettings) {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    void SetupDirectionalLight (int index, ref VisibleLight visibleLight) {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }
    
    void SetupPointLight (int index, ref VisibleLight visibleLight) {
        otherLightColors[index] = visibleLight.finalColor;
        otherLightPositions[index] = visibleLight.localToWorldMatrix.GetColumn(3);
    }
    
    void SetupLights ()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0, otherLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++) {
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType) {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount) {
                        SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount) {
                        SetupPointLight(otherLightCount++, ref visibleLight);
                    }
                    break;
            }
           
        }

        if (dirLightCount > 0) {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }

        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount > 0) {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(
                otherLightPositionsId, otherLightPositions
            );
        }
    }
    public void Cleanup () {
        shadows.Cleanup();
    }
}