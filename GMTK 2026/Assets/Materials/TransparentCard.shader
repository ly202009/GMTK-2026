Shader "Cards/Card Effects"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Glass ("Glass", Range(0,1)) = 1
        _Blur ("Blur", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment GlassFragment
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            float _Glass;
            float _Blur;

            fixed4 GlassFragment(v2f i) : SV_Target
            {
                float2 blurStep = _MainTex_TexelSize.xy * 2;
                fixed4 center = SampleSpriteTexture(i.texcoord);
                fixed4 blurred = center * 4;
                blurred += SampleSpriteTexture(i.texcoord + float2(blurStep.x, 0));
                blurred += SampleSpriteTexture(i.texcoord - float2(blurStep.x, 0));
                blurred += SampleSpriteTexture(i.texcoord + float2(0, blurStep.y));
                blurred += SampleSpriteTexture(i.texcoord - float2(0, blurStep.y));
                blurred /= 8;

                fixed4 color = lerp(center, blurred, _Blur) * i.color;
                float2 edgeDistance = min(i.texcoord, 1 - i.texcoord);
                float edge = 1 - smoothstep(.015, .07, min(edgeDistance.x, edgeDistance.y));
                float diagonal = abs(i.texcoord.y + i.texcoord.x * .32 - .82);
                float shine = 1 - smoothstep(.015, .06, diagonal);
                color.rgb = lerp(color.rgb, fixed3(.55, .92, 1), edge * .65 * _Glass);
                color.rgb = lerp(color.rgb, fixed3(1, 1, 1), shine * .35 * _Glass);
                color.a *= lerp(1, .5, _Glass);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
