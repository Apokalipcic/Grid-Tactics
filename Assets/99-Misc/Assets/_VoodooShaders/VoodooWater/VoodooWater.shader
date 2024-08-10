// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Voodoo/WaterUnlit"
{
	Properties
	{
		_MainColor("Main Color", Color) = (0.1132075,0.7023883,1,1)
		_FoamColor("Foam Color", Color) = (1,1,1,1)
		_ShadowColor("ShadowColor", Color) = (0.2242791,0.372331,0.6792453,0.5764706)
		_MainTexture("Main Texture", 2D) = "white" {}
		_WorldScale("World Scale", Range( 0.001 , 1)) = 1
		_Sharpness("Sharpness", Range( 0 , 1)) = 0.5
		_SecondaryFoam("Secondary Foam", Range( 0 , 1)) = 0.5
		_FadeDistance("Fade Distance", Range( 0 , 100)) = 10
		_HSpeed("H Speed", Range( -1 , 1)) = 0.1
		_VSpeed("V Speed", Range( -1 , 1)) = 0.1
		[Toggle(_DISTORTION_ON)] _Distortion("Texture Ripples", Float) = 0
		_DistortionScale("Distortion Scale", Range( 0.001 , 5)) = 1
		_DistortionIntensity("Distortion Intensity", Range( 0 , 1)) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#pragma target 2.5
		#pragma shader_feature_local _DISTORTION_ON
		#pragma exclude_renderers xbox360 xboxone ps4 psp2 n3ds wiiu 
		#pragma surface surf StandardCustomLighting keepalpha exclude_path:deferred nodynlightmap 
		struct Input
		{
			float3 worldPos;
			half3 worldNormal;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform half4 _ShadowColor;
		uniform half4 _MainColor;
		uniform half _Sharpness;
		uniform sampler2D _MainTexture;
		uniform half _HSpeed;
		uniform half _VSpeed;
		uniform half _WorldScale;
		uniform half _DistortionScale;
		uniform half _DistortionIntensity;
		uniform half _FadeDistance;
		uniform half _SecondaryFoam;
		uniform half4 _FoamColor;

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			#ifdef UNITY_PASS_FORWARDBASE
			float ase_lightAtten = data.atten;
			if( _LightColor0.a == 0)
			ase_lightAtten = 0;
			#else
			float3 ase_lightAttenRGB = gi.light.color / ( ( _LightColor0.rgb ) + 0.000001 );
			float ase_lightAtten = max( max( ase_lightAttenRGB.r, ase_lightAttenRGB.g ), ase_lightAttenRGB.b );
			#endif
			#if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
			half bakedAtten = UnitySampleBakedOcclusion(data.lightmapUV.xy, data.worldPos);
			float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
			float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
			ase_lightAtten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
			#endif
			float3 ase_worldPos = i.worldPos;
			half3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			half3 ase_worldNormal = i.worldNormal;
			half fresnelNdotV179 = dot( ase_worldNormal, ase_worldViewDir );
			half fresnelNode179 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV179, 3.0 ) );
			half temp_output_35_0 = ( _Sharpness + 0.1 );
			half2 appendResult54 = (half2(_HSpeed , _VSpeed));
			half2 appendResult14 = (half2(ase_worldPos.x , ase_worldPos.z));
			half2 temp_output_17_0 = ( appendResult14 * _WorldScale );
			half2 appendResult158 = (half2(0.0 , tex2D( _MainTexture, ( temp_output_17_0 * _DistortionScale ) ).g));
			half2 lerpResult159 = lerp( temp_output_17_0 , ( temp_output_17_0 + appendResult158 ) , _DistortionIntensity);
			#ifdef _DISTORTION_ON
				half2 staticSwitch163 = lerpResult159;
			#else
				half2 staticSwitch163 = temp_output_17_0;
			#endif
			half2 ProjectedUV147 = staticSwitch163;
			half2 panner5 = ( _Time.y * appendResult54 + ProjectedUV147);
			half smoothstepResult32 = smoothstep( temp_output_35_0 , _Sharpness , tex2D( _MainTexture, panner5 ).r);
			half3 temp_output_5_0_g1 = ( ( ase_worldPos - _WorldSpaceCameraPos ) / _FadeDistance );
			half dotResult8_g1 = dot( temp_output_5_0_g1 , temp_output_5_0_g1 );
			half clampResult68 = clamp( pow( saturate( dotResult8_g1 ) , 5.0 ) , 0.0 , 0.75 );
			half cameraFade101 = clampResult68;
			half mulTime140 = _Time.y * 0.5;
			half2 appendResult139 = (half2(( _HSpeed * -1.0 ) , ( _VSpeed * -1.0 )));
			half2 panner137 = ( mulTime140 * appendResult139 + ProjectedUV147);
			half smoothstepResult103 = smoothstep( temp_output_35_0 , ( _Sharpness * 0.5 ) , tex2D( _MainTexture, ( panner137 * 0.5 ) ).b);
			half WaterNoise28 = ( saturate( ( smoothstepResult32 + cameraFade101 ) ) * saturate( ( smoothstepResult103 + ( 1.0 - _SecondaryFoam ) ) ) );
			half4 WaterColor74 = saturate( ( ( saturate( ( 0.6 + _MainColor ) ) * fresnelNode179 ) + ( saturate( ( ( _MainColor * WaterNoise28 ) + ( _FoamColor * ( 1.0 - WaterNoise28 ) ) ) ) * ( 1.0 - fresnelNode179 ) ) ) );
			half4 lerpResult83 = lerp( _ShadowColor , WaterColor74 , saturate( ( ( 1.0 - _ShadowColor.a ) + ase_lightAtten ) ));
			c.rgb = lerpResult83.rgb;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18500
-1678;182;1678;830;2028.166;1314.719;2.212039;True;True
Node;AmplifyShaderEditor.CommentaryNode;170;-6104.147,-2119.558;Inherit;False;2242.881;643.3651;UVS;20;13;8;14;161;17;162;156;169;154;167;168;158;152;165;160;166;159;164;163;147;;0.7924528,0.485938,0.485938,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;13;-6054.146,-1979.688;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;95;-3720.902,-2077.583;Inherit;False;302.3333;280;MASTER TEXTURE R= foam G= noise  B = water;1;19;;1,0.1056694,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-6024.111,-1810.118;Inherit;False;Property;_WorldScale;World Scale;4;0;Create;True;0;0;False;0;False;1;0.472;0.001;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;19;-3670.902,-2027.584;Inherit;True;Property;_MainTexture;Main Texture;3;0;Create;True;0;0;False;0;False;None;627d2fa6b2ecc3e498ddc1924f96a002;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.DynamicAppendNode;14;-5830.678,-1955.455;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-5631.1,-1948.359;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;97;-3365.43,-1989.663;Inherit;False;masterTexture;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;161;-5818.237,-1712.672;Inherit;False;Property;_DistortionScale;Distortion Scale;11;0;Create;True;0;0;False;0;False;1;0.99;0.001;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;162;-5505.481,-1836.224;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;156;-5734.739,-1599.371;Inherit;False;97;masterTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;154;-5386.377,-1706.193;Inherit;True;Property;_DistortionNoise;DistortionNoise;10;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;169;-5468.375,-1913.511;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;167;-5474.868,-1960.294;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;158;-5074.978,-1868.251;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;168;-4934.28,-1901.816;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;152;-4886.435,-1908.521;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;165;-5486.329,-2059.701;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;92;-6116.883,-1405.026;Inherit;False;3489.359;1371.173;TEXTURE;18;96;20;110;125;122;123;121;32;103;115;35;50;98;33;99;120;146;56;;0.3381987,0.6678959,0.8962264,1;0;0
Node;AmplifyShaderEditor.WireNode;166;-4792.343,-1954.652;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;160;-4940.84,-1789.364;Inherit;False;Property;_DistortionIntensity;Distortion Intensity;12;0;Create;True;0;0;False;0;False;0;0.275;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;164;-4534.418,-2069.558;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;159;-4651.49,-1954.192;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;56;-6077.433,-1105.825;Inherit;False;1239.271;303.7602;Texture Scrolling;6;148;5;55;54;7;6;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;146;-5565.73,-727.3776;Inherit;False;755.9697;421.3342;secondary texture scrolling;8;94;137;139;140;144;143;93;149;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-6070.455,-967.9889;Inherit;False;Property;_VSpeed;V Speed;9;0;Create;True;0;0;False;0;False;0.1;0.067;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;163;-4448.563,-1981.927;Inherit;False;Property;_Distortion;Texture Ripples;10;0;Create;False;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-6055.055,-1054.876;Inherit;False;Property;_HSpeed;H Speed;8;0;Create;True;0;0;False;0;False;0.1;0.06;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;144;-5506.034,-530.3286;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;143;-5503.26,-641.145;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;147;-4089.264,-1960.112;Inherit;False;ProjectedUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;51;-6135.812,42.82878;Inherit;False;1007.537;411.1754;Camera distance fade ;5;68;46;45;43;69;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-6085.812,251.8893;Inherit;False;Property;_FadeDistance;Fade Distance;7;0;Create;True;0;0;False;0;False;10;9.4;0;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;139;-5328.229,-620.9216;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;149;-5335.839,-692.7015;Inherit;False;147;ProjectedUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;140;-5344.262,-524.0303;Inherit;False;1;0;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;43;-6015.833,92.82884;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleTimeNode;55;-5751.901,-941.7274;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;45;-5704.106,123.144;Inherit;True;SphereMask;-1;;1;988803ee12caf5f4690caee3c8c4a5bb;0;3;15;FLOAT3;0,0,0;False;14;FLOAT;0;False;12;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;69;-5483.963,364.1465;Inherit;False;Constant;_maxFade;maxFade;8;0;Create;True;0;0;False;0;False;0.75;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;137;-5118.717,-630.5471;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;54;-5742.721,-1042.153;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;148;-5576.682,-1063.53;Inherit;False;147;ProjectedUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;94;-5445.085,-384.8108;Inherit;False;Constant;_SecondaryTile;Secondary Tile;9;0;Create;True;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;68;-5329.242,127.0491;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-4601.49,-1255.717;Inherit;False;Property;_Sharpness;Sharpness;5;0;Create;True;0;0;False;0;False;0.5;0.438;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;99;-4747.682,-692.6829;Inherit;False;97;masterTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;98;-4813.842,-1127.888;Inherit;False;97;masterTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.PannerNode;5;-5373.991,-1047.663;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;93;-4963.397,-535.6703;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;50;-4281.355,-1009.688;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;120;-3672.496,-1116.062;Inherit;False;562.5149;239.4866;Fade primary noise over dist;3;48;49;102;;1,1,1,0.2666667;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;115;-4197.573,-687.7148;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;20;-4604.01,-1087.514;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;35;-4270.554,-1250.704;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;101;-5066.583,132.7711;Inherit;False;cameraFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;110;-3685.078,-628.8949;Inherit;False;Property;_SecondaryFoam;Secondary Foam;6;0;Create;True;0;0;False;0;False;0.5;0.295;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;96;-4515.918,-651.1647;Inherit;True;Property;_TextureSample1;Texture Sample 1;9;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;103;-3958.454,-646.559;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;32;-3983.265,-1065.885;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;102;-3622.496,-991.5753;Inherit;False;101;cameraFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;125;-3399.229,-632.3683;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;122;-3244.565,-758.1707;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;49;-3428.706,-1065.268;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;48;-3276.648,-1066.062;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;123;-3115.967,-760.8231;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;121;-3054.841,-988.463;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;73;-1854.148,-1315.905;Inherit;False;2674.616;985.6975;Color Blending;18;178;186;183;182;180;175;25;181;172;174;184;185;10;29;22;31;1;179;;0,0.8773585,0.7792435,0.8235294;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;28;-2516.333,-988.2572;Inherit;False;WaterNoise;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1;-1716.994,-1086.983;Inherit;False;Property;_MainColor;Main Color;0;0;Create;True;0;0;False;0;False;0.1132075,0.7023883,1,1;0.1137255,0.6919538,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;31;-1802.491,-565.2078;Inherit;False;28;WaterNoise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;22;-1587.921,-559.6713;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;29;-1723.477,-907.1907;Inherit;False;28;WaterNoise;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;10;-1754.036,-766.263;Inherit;False;Property;_FoamColor;Foam Color;1;0;Create;True;0;0;False;0;False;1,1,1,1;0.745283,0.923501,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;185;-1451.272,-1147.487;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;178;-1447.874,-1052.501;Inherit;False;640.9974;600.2535;Blend foam and main (could be a lerp);3;24;21;23;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WireNode;184;-500.2248,-1101.638;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-1316.085,-705.2477;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-1397.874,-1002.501;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;174;-556.7393,-1185.283;Inherit;False;Constant;_Float0;Float 0;13;0;Create;True;0;0;False;0;False;0.6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;179;-406.7712,-936.9159;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;172;-381.2415,-1112.904;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;24;-1041.877,-737.8138;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;175;-235.4666,-1115.92;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;25;-530.5797,-723.903;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;181;-154.3174,-677.0701;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;180;-82.2576,-960.3405;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;91;-1429.575,-206.5969;Inherit;False;873.2739;512.1017;Colored Shadows;7;76;72;75;83;88;89;90;;0.8584906,0.7543246,0.1214845,0.9529412;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;182;-1.500702,-718.0698;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;76;-1379.575,-156.5969;Inherit;False;Property;_ShadowColor;ShadowColor;2;0;Create;True;0;0;False;0;False;0.2242791,0.372331,0.6792453,0.5764706;0,0.4718938,1,0.6196079;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;183;155.6187,-776.2772;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;186;277.4954,-772.5502;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LightAttenuation;72;-1367.185,195.5048;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;90;-1134.104,108.7231;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;88;-974.0194,130.247;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;74;972.8363,-796.3831;Inherit;False;WaterColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;89;-859.6739,145.0447;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;75;-1376.998,39.69267;Inherit;False;74;WaterColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;83;-738.9673,-14.5966;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;70;-200.3143,-249.8449;Half;False;True;-1;1;ASEMaterialInspector;0;0;CustomLighting;Voodoo/WaterUnlit;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;ForwardOnly;8;d3d9;d3d11_9x;d3d11;glcore;gles;gles3;metal;vulkan;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;187;-606.7393,-1235.283;Inherit;False;1050.901;668.2133;Horizon;0;;1,1,1,1;0;0
WireConnection;14;0;13;1
WireConnection;14;1;13;3
WireConnection;17;0;14;0
WireConnection;17;1;8;0
WireConnection;97;0;19;0
WireConnection;162;0;17;0
WireConnection;162;1;161;0
WireConnection;154;0;156;0
WireConnection;154;1;162;0
WireConnection;169;0;17;0
WireConnection;167;0;17;0
WireConnection;158;1;154;2
WireConnection;168;0;169;0
WireConnection;152;0;168;0
WireConnection;152;1;158;0
WireConnection;165;0;17;0
WireConnection;166;0;167;0
WireConnection;164;0;165;0
WireConnection;159;0;166;0
WireConnection;159;1;152;0
WireConnection;159;2;160;0
WireConnection;163;1;164;0
WireConnection;163;0;159;0
WireConnection;144;0;7;0
WireConnection;143;0;6;0
WireConnection;147;0;163;0
WireConnection;139;0;143;0
WireConnection;139;1;144;0
WireConnection;45;15;43;0
WireConnection;45;14;46;0
WireConnection;137;0;149;0
WireConnection;137;2;139;0
WireConnection;137;1;140;0
WireConnection;54;0;6;0
WireConnection;54;1;7;0
WireConnection;68;0;45;0
WireConnection;68;2;69;0
WireConnection;5;0;148;0
WireConnection;5;2;54;0
WireConnection;5;1;55;0
WireConnection;93;0;137;0
WireConnection;93;1;94;0
WireConnection;50;0;33;0
WireConnection;115;0;33;0
WireConnection;20;0;98;0
WireConnection;20;1;5;0
WireConnection;35;0;33;0
WireConnection;101;0;68;0
WireConnection;96;0;99;0
WireConnection;96;1;93;0
WireConnection;103;0;96;3
WireConnection;103;1;35;0
WireConnection;103;2;115;0
WireConnection;32;0;20;1
WireConnection;32;1;35;0
WireConnection;32;2;50;0
WireConnection;125;0;110;0
WireConnection;122;0;103;0
WireConnection;122;1;125;0
WireConnection;49;0;32;0
WireConnection;49;1;102;0
WireConnection;48;0;49;0
WireConnection;123;0;122;0
WireConnection;121;0;48;0
WireConnection;121;1;123;0
WireConnection;28;0;121;0
WireConnection;22;0;31;0
WireConnection;185;0;1;0
WireConnection;184;0;185;0
WireConnection;23;0;10;0
WireConnection;23;1;22;0
WireConnection;21;0;1;0
WireConnection;21;1;29;0
WireConnection;172;0;174;0
WireConnection;172;1;184;0
WireConnection;24;0;21;0
WireConnection;24;1;23;0
WireConnection;175;0;172;0
WireConnection;25;0;24;0
WireConnection;181;0;179;0
WireConnection;180;0;175;0
WireConnection;180;1;179;0
WireConnection;182;0;25;0
WireConnection;182;1;181;0
WireConnection;183;0;180;0
WireConnection;183;1;182;0
WireConnection;186;0;183;0
WireConnection;90;0;76;4
WireConnection;88;0;90;0
WireConnection;88;1;72;0
WireConnection;74;0;186;0
WireConnection;89;0;88;0
WireConnection;83;0;76;0
WireConnection;83;1;75;0
WireConnection;83;2;89;0
WireConnection;70;13;83;0
ASEEND*/
//CHKSM=E6D4BEFE41E176FCB58353078666A48608845070