Shader "Custom/URPStylizedWater"
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
            // Vertex and fragment function definitions for URP
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            // Include core and lighting headers from URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Attributes coming from the mesh
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            // Data passed from vertex to fragment shader
            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal   : TEXCOORD2;
            };

            // Exposed properties are put into a constant buffer.
            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float4 _DeepColor;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _NormalScale;
                float4 _SpecularColor;
                float _Shininess;
            CBUFFER_END

            sampler2D _NormalMap;

            // Vertex shader: displaces vertices using a combination of sine/cosine waves.
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // Transform the vertex to world space.
                float3 worldPos = mul(unity_ObjectToWorld, IN.vertex).xyz;
                float time = _Time.y;

                // Combine several wave functions for a more dynamic water surface.
                float wave1 = sin(worldPos.x * _WaveFrequency + time * _WaveSpeed);
                float wave2 = cos(worldPos.z * _WaveFrequency + time * _WaveSpeed * 1.2);
                float wave3 = sin((worldPos.x + worldPos.z) * _WaveFrequency * 0.5 + time * _WaveSpeed * 0.8);
                float displacement = (wave1 + wave2 * 0.5 + wave3 * 0.3) * _WaveAmplitude;
                worldPos.y += displacement;

                OUT.worldPos = worldPos;
                // Start with an upward normal; the normal map will help to simulate detail.
                OUT.normal = float3(0, 1, 0);
                OUT.uv = IN.uv;
                OUT.position = TransformWorldToHClip(worldPos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the normal map and perturb the base normal.
                float2 tiledUV = IN.uv * 4.0; // Adjust tiling as needed.
                float3 normalTex = UnpackNormal(tex2D(_NormalMap, tiledUV));
                float3 normal = normalize(IN.normal + normalTex * _NormalScale);

                // Compute view direction.
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);

                // Fresnel effect for a reflective edge.
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), 3.0);

                // Get the main light using URP's lighting function.
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normal, lightDir));

                // Blend between deep and shallow water colors based on lighting.
                float3 baseColor = lerp(_DeepColor.rgb, _WaterColor.rgb, NdotL);

                // Compute a simple specular highlight.
                float3 halfDir = normalize(lightDir + viewDir);
                float specFactor = pow(saturate(dot(normal, halfDir)), _Shininess);
                float3 specular = _SpecularColor.rgb * specFactor;

                // Combine base color and specular; blend with a white tint based on the fresnel term.
                float3 color = baseColor + specular;
                color = lerp(color, float3(1.0, 1.0, 1.0), fresnel * 0.3);

                return half4(color, _WaterColor.a);
            }

            // Function to get the main light direction
            float3 GetMainLightDirection()
            {
                // Get the main light
                Light mainLight = GetMainLight();

                // Return the direction of the main light
                return mainLight.direction;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Forward"
}