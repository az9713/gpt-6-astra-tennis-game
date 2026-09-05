Shader "RoboOpen/RobotTint"
{
    Properties
    {
        _BaseMap("Base map",2D)="white"{}
        _BaseColor("Base color",Color)=(1,1,1,1)
        _TeamTint("Team color",Color)=(0.15,0.72,0.65,1)
        _TintAmount("Tint amount",Range(0,1))=0
        _Cutoff("Cutoff",Float)=0.5
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor,_TeamTint;
            float _TintAmount,_Cutoff,_Cull;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
            V vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
            half4 frag(V i):SV_Target
            {
                half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                half mask=smoothstep(.06,.20,base.r-base.g)*smoothstep(.02,.12,base.g-base.b)*_TintAmount;
                base=lerp(base,_TeamTint.rgb*max(base.r,.25),mask);
                half3 n=normalize(i.normalWS);Light l=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl=saturate(dot(n,l.direction));half3 view=SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                half spec=pow(saturate(dot(n,normalize(l.direction+view))),55)*.23;
                half3 lit=base*(SampleSH(n)+l.color*(ndl*l.shadowAttenuation*.85+.12))+spec*l.color*l.shadowAttenuation;
                return half4(lit,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
