Shader "Sprite/ShaderOutline"
{
    Properties
    {
		_MainTex("Main Texture", 2D) = "white" {}		
		_Color("Tint", Color) = (1,1,1,1)
		[PerRendererData] _Outline("Outline",  Range(0,1)) = 1
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Outline;
			fixed4 _Color;

			struct appdata {
				float4 vertex : POSITION;
				float4 color    : COLOR;
				float2 uv0 : TEXCOORD0;
			};

			struct v2f {
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
				float2 uv0 : TEXCOORD0;
			};

			v2f vert(appdata v) {
				v2f o;
				o.uv0 = v.uv0;
				o.position = UnityObjectToClipPos(v.vertex);
				o.color = v.color * _Color;
				return o;
			}

			fixed4 frag(v2f i) : SV_TARGET{
				fixed4 col0 = tex2D(_MainTex, i.uv0);
				col0.rgb = col0.rgb * i.color.rgb * _Outline + i.color.rgb * (1 - _Outline);
				col0.rgb *= col0.a;
				return col0;
			}

			ENDCG
        }
    }
}