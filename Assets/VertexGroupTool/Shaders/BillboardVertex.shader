Shader "Custom/BillboardVertex"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }
        Pass
        {
            ZTest LEqual
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #include "UnityCG.cginc"

            float4 _Color;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                // Get instance position from object-to-world matrix
                float3 instancePos = unity_ObjectToWorld._m03_m13_m23;

                // Compute camera-facing orientation
                float3 right = UNITY_MATRIX_V._m00_m10_m20;
                float3 up    = UNITY_MATRIX_V._m01_m11_m21;

                // Apply scale from the matrix
                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20), // X-axis
                    length(unity_ObjectToWorld._m01_m11_m21), // Y-axis
                    length(unity_ObjectToWorld._m02_m12_m22)  // Z-axis
                );

                float3 viewDir = normalize(_WorldSpaceCameraPos - instancePos);
                float3 offset = (v.vertex.x * right + v.vertex.y * up) * scale.x;

                // Small push toward camera
                float3 nudgedPos = instancePos + offset + viewDir * 0.01; // adjust 0.001 as needed

                v2f o;
                o.uv = v.uv;
                o.pos = UnityWorldToClipPos(nudgedPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
