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
            };

            StructuredBuffer<float3> _InstancePosition;
            float col;

            v2f vert (uint id : SV_InstanceID, appdata v)
            {
                v2f o;
                float3 instancePos = _InstancePosition[id];
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex + float4(instancePos, 0.0));
                o.worldPos = worldPosition.xyz;
                o.vertex = UnityObjectToClipPos(worldPosition);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }


            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 frag (v2f i) : SV_Target
            {
                return half4(col,col,col,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
