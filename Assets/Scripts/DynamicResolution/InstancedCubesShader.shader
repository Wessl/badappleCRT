Shader "Custom/InstancedCubes"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 normal : NORMAL;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 color : COLOR0;
            };

            StructuredBuffer<float3> _InstancePosition;
            StructuredBuffer<float4> _InstanceColor;

            v2f vert (uint id : SV_InstanceID, appdata v)
            {
                v2f o;
                float3 instancePos = _InstancePosition[id];
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex + float4(instancePos, 0.0));
                o.worldPos = worldPosition.xyz;
                o.vertex = UnityObjectToClipPos(worldPosition);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = _InstanceColor[id] / 255.0;
                return o;
            }


            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 frag (v2f i) : SV_Target
            {
                // you could do something cool with the color here... just saying.
                return i.color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
