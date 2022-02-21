#include "UnityShaderVariables.cginc"
#include "AutoLight.cginc"
#include "UnityLightingCommon.cginc"

// Gets lighting information like direction and colour
UnityLight get_lighting_info()
{
    UnityLight lighting;
    lighting.dir	= normalize(_WorldSpaceLightPos0.xyz);
    lighting.color	= _LightColor0.rgb;
    lighting.ndotl = -1; // Do not use this. Its deprecated
    return lighting;
}