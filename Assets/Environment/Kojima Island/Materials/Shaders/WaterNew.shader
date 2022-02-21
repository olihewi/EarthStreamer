Shader "Custom/WaterNew" 
{
	Properties 
	{
		[Header(Water Appearance)]
		[Space(10)]
		[HDR] _Color	("Water Color", Color)				= (1,1,1,1)
		_Specular		("Specular intensity", Range(0,1))	= 0.5
		_Glossy			("Water Glossyness", Range(0,1))	= 1.0
		
		[Space(10)]
    	[Header(Fog appearance)]
    	[Space(10)]
		[HDR] _FogColor		("Fog Color", Color)			= (0,0,0,0)
		_FogThreshold		("Fog threshold", Range(0,100)) = 1.0

		[Space(10)]
    	[Header(Form)]
    	[Space(10)]
		[HDR] _FormColor	("Form Color", Color)				= (1,1,1,1)
		_IntersectThreshold	("Form threshold", Range(1,10))		= 1.0
		_IntersectPower		("Form power", Range(1,4))			= 1.0
		
		[Space(10)]
    	[Header((XY direction) (Z steepness) (W wavelength))]
		[Space(10)]
		_Wave1 ("Wave 1", Vector) = (1.0, 1.0, 1.0, 1.25)
    	_Wave2 ("Wave 2", Vector) = (0.0, 1.0, 1.0, 1.25)
	}
	SubShader 
	{
		Name "Low poly Water shader"
		Tags 
		{ 
			"RenderType" = "Transparent" 
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
		}
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
        Cull back 
		LOD 100

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma target 3.0
			#pragma fragment frag alpha
			#pragma vertex vert alpha

			#include <UnityImageBasedLighting.cginc>

			#include "UnityCG.cginc"
			#include "Assets/Environment/Kojima Island/Materials/Shaders/Includes/Vertex.cginc"
			#include "Assets/Environment/Kojima Island/Materials/Shaders/Includes/CustomLighting.cginc"
			#include "Assets/Environment/Kojima Island/Materials/Shaders/Includes/Helpers.cginc"

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			sampler2D _WaterBackground;
			float4 _CameraDepthTexture_TexelSize;
			float4 _Color, _FormColor, _FogColor;
			half _Specular, _FogDensity, _IntersectThreshold, _IntersectPower, _DepthFactor, _DepthPow, _Glossy, _FogThreshold;
			float4 _Wave1, _Wave2;

			// Normal distribution function
			half trowbridge_reitz_ggx(const half n_dot_h, const half roughness)
			{
			    const half a = roughness * roughness;
			    const half a2 = a * a;
			    const half n_dot_h2 = n_dot_h * n_dot_h;

			    const half num = a2;
			    half de_nom = (n_dot_h2 * (a2 - 1.0h) + 1.0h);
			    de_nom = UNITY_PI * de_nom * de_nom;

			    return num / de_nom;
			}

			half geometry_schlick_ggx(const half n_dot_v, const half roughness)
			{
			    const half r = roughness + 1.0h; // Disney modification to remap roughness to reduce roughness 'Hotness'
			    const half k = (r * r) / 8.0h;

			    const half num = n_dot_v;
			    const half de_nom = n_dot_v * (1.0h - k) + k;

			    return num / de_nom;
			}

			// Geometry function
			half geometry_smith(const half n_dot_v, const half n_dot_l, const half roughness)
			{
			    const half ggx1 = geometry_schlick_ggx(n_dot_v, roughness);
			    const half ggx2 = geometry_schlick_ggx(n_dot_l, roughness);

			    return ggx1 * ggx2;
			}

			// Fresnel function
			half3 fresnel_schlick(const half n_dot_l, const half3 f0, const half f90)
			{
			    return f0 + (f90 - f0) * pow(1.0h - n_dot_l, 5.0h);
			}
			
			float3 gerstner_wave(float4 wave_data, float3 vertex)
			{
				// Unpack Wave properties
				const float steepness = wave_data.z;
				const float wavelength = wave_data.w;
				float2 direction = normalize(wave_data.xy);

				const float num_wave = 2.0f * UNITY_PI / wavelength;
				const float phase_speed = sqrt(9.8f / num_wave);

				const float f = num_wave * (dot(wave_data, vertex.xz) - phase_speed * _Time.y);
				float amplitude = steepness / num_wave;
				const float anchor = amplitude * cos(f);

				return float3(direction.x * anchor, amplitude * sin(f), direction.y * anchor);
			}

			float3 generate_low_poly_normal(const float3 world_position)
			{
				const float3 world_pos_ddx = ddx(world_position);
	    		const float3 world_pos_ddy = ddy(world_position);
	    		return normalize(cross(world_pos_ddx, world_pos_ddy));
			}


			struct pixel_output
			{
				fixed4 albedo;
				fixed3 normal;
				fixed roughness;
				fixed metallic;
				fixed4 emissive;
			};

			pixel_output fragment(in const pixel_output input)
			{
				pixel_output output;
				output.normal = normalize(input.normal);
				output.metallic = saturate(input.metallic);
				output.roughness = saturate(input.roughness);
				output.emissive = input.emissive;

				fixed n_dot_l = saturate(dot(input.normal, normalize(_WorldSpaceLightPos0.xyz)));
				
				output.albedo = (input.albedo * n_dot_l) + input.emissive;
				return output;
			}
			
			vertex_output vert(appdata_full v)
			{
				// Assign vertex position xyz to temp p for displacing
				float3 p = v.vertex.xyz;
				p += gerstner_wave(_Wave1, v.vertex.xyz);
				p += gerstner_wave(_Wave2, v.vertex.xyz);
				v.vertex.xyz = p;

				vertex_output output;
				output.position			= UnityObjectToClipPos(v.vertex);
				output.normal			= UnityObjectToWorldNormal(v.normal);
				output.uv				= float4(v.texcoord.xy, 0.0f, 0.0f);
				output.tangent			= float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
				output.world_position	= mul(unity_ObjectToWorld, v.vertex).xyz;
				output.screen_position	= ComputeScreenPos(output.position);
				COMPUTE_EYEDEPTH(output.screen_position.z);

				// Unity fog and shadows
				UNITY_TRANSFER_FOG(output, output.position);
				TRANSFER_VERTEX_TO_FRAGMENT(output); // Misleading name. Its transfer data for shadows
				
				return output;
			}

			// pixel_output frag(vertex_output input) : SV_Target
			// {
			// 	pixel_output output;
			// 	output.albedo = _Color;
			// 	output.emissive = fixed4(1.0f, 0.0f, 0.0f, 1.0f);
			// 	output.roughness = 1.0f;
			// 	output.metallic = 1.0f;
			// 	output.normal = -generate_low_poly_normal(input.world_position);
			//
			// 	return fragment(output);
			// }

			// https://halisavakis.com/my-take-on-shaders-stylized-water-shader/
			// https://www.edraflame.com/blog/custom-shader-depth-texture-sampling/
			
			fixed4 frag(vertex_output input) : SV_Target
			{
				// invert Glossy to make more sense in editor
				_Glossy = 1.0f - _Glossy;
				
				// Get lighting information from Unity
				UnityLight light = get_lighting_info();
				light.color *= LIGHT_ATTENUATION(input);
			
				// Generate low-poly normals from the vertex world position using DDX and DDY
	    		const float3 lowPoly_normal = generate_low_poly_normal(input.world_position);
				
				// Get view direction | half vector | reflection direction
				const half3 view_dir		= normalize(UnityWorldSpaceViewDir(input.world_position));
				const half3 half_vector		= normalize(view_dir + light.dir);
				const half3 reflection_dir	= normalize(reflect(-view_dir, lowPoly_normal));

				const half n_dot_l = saturate(dot(-lowPoly_normal, light.dir));		// Dot product of normal and light
				const half n_dot_h = saturate(dot(-lowPoly_normal, half_vector));	// Dot product of normal and half vector
				const half n_dot_v = saturate(dot(-lowPoly_normal, view_dir));		// Dot product of normal and view
				const half v_dot_h = saturate(dot(view_dir, half_vector));

				// Functions for Cook-Torrance BRDF
				half n = trowbridge_reitz_ggx(n_dot_h, _Glossy);
				half g = geometry_smith(n_dot_v, n_dot_l, _Glossy) * geometry_smith(n_dot_v, n_dot_l, _Glossy);
				half3 f = fresnel_schlick(v_dot_h, float3(0.04h, 0.04h, 0.04h), 1.0h);
				
				// Create base specular effect using fresnel
				half3 fresnel = fresnel_schlick(n_dot_v, float3(0.04h, 0.04h, 0.04h), 1.0h);
				
				// Create specular highlights
				//fixed specular = pow(saturate(dot(half_vector, -lowPoly_normal)), _Glossy * 100.0f) * _Specular;
				fixed specular = (n * g * f) / (4.0f * n_dot_v * n_dot_l + 0.0001h) * _Specular;
				
				// Create base diffuse
				fixed3 diffuse = (_Color.rgb * light.color);
				
				// Sample camera z-buffer
				float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screen_position)));
				float depth = sceneZ - input.screen_position.w;

				// underwater fog
				fixed fog_diff = saturate((sceneZ - input.screen_position.w) / _FogThreshold);
				diffuse = lerp(diffuse.rgb, _FogColor.rgb, fog_diff);

				//_Color.a = lerp(_Color.a - 0.5f, _Color.a, fog_diff);

				// Generate form
				fixed intersect = saturate(depth / _IntersectThreshold);
				fixed3 form = _FormColor.rgb * pow(1.0f - intersect, 4) * _IntersectPower;
				diffuse += form;

				// Cube map
				// Unity_GlossyEnvironmentData env_data;
				// env_data.roughness = 0.0f;
				// env_data.reflUVW = reflection_dir;
				// fixed3 cube_map = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, env_data);

				return output_fixed4(diffuse + (fresnel + specular), _Color.a);
			}
			ENDCG
		}
	}
	// No fall back for transparent shaders
}