#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END


#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED
int GetDirectionalLightCount () {
    return _DirectionalLightCount;
}

struct Light {
    float3 color;
    float3 direction;
    float attenuation;
};

int GetOtherLightCount () {
    return _OtherLightCount;
}

DirectionalShadowData GetDirectionalShadowData (int lightIndex, ShadowData shadowData) {
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y+ shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}
Light GetDirectionalLight (int index, Surface surfaceWS, ShadowData shadowData) {
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData dirShadowData  = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
    //light.attenuation = shadowData.cascadeIndex * 0.25;
    return light;
}

#endif