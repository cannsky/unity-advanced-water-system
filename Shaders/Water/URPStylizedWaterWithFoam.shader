Shader "Custom/URPStylizedWaterWithFoam"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0.1, 0.3, 0.6, 1)
        _DeepColor ("Deep Water Color", Color) = (0.0, 0.1, 0.3, 1)
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 1.0
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Float) = 1.0
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Float) = 50.0

        // New properties for foam:
        _CoastLevel ("Coast Level", Float) = 0.0         // World y below which foam appears.
        _FoamTex ("Foam Texture", 2D) = "white" {}         // A texture that defines the foam pattern.
        _FoamScale ("Foam Scale", Float) = 3.0             // Controls the tiling/size of the foam pattern.
        _FoamSpeed ("Foam Speed", Float) = 0.5             // Animation speed for the foam.
        _FoamIntensity ("Foam Intensity", Float) = 5.0       // How strongly foam appears in shallow water.
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            // Vertex and fragment function definitions for URP.
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            // Include URP headers.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Vertex attributes coming from the mesh.
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            // Data passed from the vertex to the fragment shader.
            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal   : TEXCOORD2;
            };

            // Exposed properties in a constant buffer.
            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float4 _DeepColor;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _NormalScale;
                float4 _SpecularColor;
                float _Shininess;
                // Foam properties:
                float _CoastLevel;
                float _FoamScale;
                float _FoamSpeed;
                float _FoamIntensity;
            CBUFFER_END

            sampler2D _NormalMap;
            sampler2D _FoamTex;

            // Vertex shader: displaces vertices using a combination of sine/cosine waves.
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // Transform the vertex into world space.
                float3 worldPos = mul(unity_ObjectToWorld, IN.vertex).xyz;
                float time = _Time.y;

                // Combine several wave functions for dynamic water surface.
                float wave1 = sin(worldPos.x * _WaveFrequency + time * _WaveSpeed);
                float wave2 = cos(worldPos.z * _WaveFrequency + time * _WaveSpeed * 1.2);
                float wave3 = sin((worldPos.x + worldPos.z) * _WaveFrequency * 0.5 + time * _WaveSpeed * 0.8);
                float displacement = (wave1 + wave2 * 0.5 + wave3 * 0.3) * _WaveAmplitude;
                worldPos.y += displacement;

                OUT.worldPos = worldPos;
                // Start with an upward normal. The normal map will add fine detail.
                OUT.normal = float3(0, 1, 0);
                OUT.uv = IN.uv;
                OUT.position = TransformWorldToHClip(worldPos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the normal map and perturb the base normal.
                float2 tiledUV = IN.uv * 4.0;
                float3 normalTex = UnpackNormal(tex2D(_NormalMap, tiledUV));
                float3 normal = normalize(IN.normal + normalTex * _NormalScale);

                // Compute view direction.
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);

                // Fresnel effect.
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), 3.0);

                // Lighting.
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normal, lightDir));

                // Blend between deep and shallow water colors.
                float3 baseColor = lerp(_DeepColor.rgb, _WaterColor.rgb, NdotL);

                // Compute specular highlight.
                float3 halfDir = normalize(lightDir + viewDir);
                float specFactor = pow(saturate(dot(normal, halfDir)), _Shininess);
                float3 specular = _SpecularColor.rgb * specFactor;

                // Combine base color and specular; add a white tint based on fresnel.
                float3 color = baseColor + specular;
                color = lerp(color, float3(1.0, 1.0, 1.0), fresnel * 0.3);

                // ----- FOAM EFFECT CODE -----
                // Foam appears when the water surface is below the defined coast level.
                float foamFactor = saturate((_CoastLevel - IN.worldPos.y) * _FoamIntensity);
                if(foamFactor > 0.01)
                {
                    // Animate the foam texture by offsetting its UV coordinates with time.
                    float2 foamUV = IN.uv * _FoamScale + float2(_Time.y * _FoamSpeed, _Time.y * _FoamSpeed);
                    // Sample the foam texture (assumed to be grayscale; using its red channel).
                    float foamMask = tex2D(_FoamTex, foamUV).r;
                    // Blend foam into the water color. Adjust the blend factor as needed.
                    color = lerp(color, color + foamMask, foamFactor * 0.5);
                }
                // ----- END FOAM EFFECT CODE -----

                return half4(color, _WaterColor.a);
            }

            // (Optional) Utility function to get the main light direction.
            float3 GetMainLightDirection()
            {
                Light mainLight = GetMainLight();
                return mainLight.direction;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Forward"
}