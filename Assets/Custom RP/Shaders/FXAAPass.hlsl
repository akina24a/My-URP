#ifndef CUSTOM_FXAA_PASS_INCLUDED
#define CUSTOM_FXAA_PASS_INCLUDED


float GetLuma (float2 uv) {
    return GetSource(uv).g;
    return sqrt(Luminance(GetSource(uv)));
}

float4 FXAAPassFragment (Varyings input) : SV_TARGET {
    return GetLuma(input.screenUV);
}

#endif