Shader "Custom/PaperStyleBoilingURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BoilStrength ("Boil Strength", Range(0.0, 1.0)) = 0.1
        _TimeScale ("Time Scale", Range(0.1, 10.0)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _BoilStrength;
            float _TimeScale;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // Boil Effect
            float2 BoilEffect(float2 uv, float strength, float time)
            {
                uv.x += sin(uv.y * 10.0 + time * 2.0) * strength;
                uv.y += cos(uv.x * 10.0 + time * 2.0) * strength;
                return uv;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float boilTime = _Time.y * _TimeScale;
                float2 boilUV = BoilEffect(IN.uv, _BoilStrength, boilTime);
                half4 col = tex2D(_MainTex, boilUV);

                // Convert to grayscale
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = float3(gray, gray, gray);

                // Paper-like contrast (black and white)
                col.rgb = step(0.5, col.rgb);

                return col; 
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
