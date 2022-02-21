float output_float(const float color)
{
    return color;
}

float4 output_float4(const float3 color, const float a)
{
    return float4(color, a);
}

half output_half(const half color)
{
    return color;
}
half4 output_half4(const half3 color, const half a)
{
    return half4(color, a);
}

fixed output_fixed(const fixed color)
{
    return color;
}

fixed4 output_fixed4(const fixed3 color, const fixed alpha)
{
    return fixed4(color, alpha);
}
