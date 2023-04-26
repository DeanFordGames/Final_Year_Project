Shader "Custom/Terrain"
{
    Properties
    {
        _Displacement("Displacement", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Amount("Amount", Range(0.1, 10.0)) = 0.5
    }
        SubShader
        {
            pass {
                Tags {"LightMode"="ForwardBase"}

                CGPROGRAM

                #pragma vertex vertexFunc
                #pragma fragment fragmentFunc

                #include "UnityCG.cginc"
                #include "UnityLightingCommon.cginc"

                struct appdata {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float4 diff : COLOR0;
                };

                sampler2D _Displacement;
                sampler2D _NormalMap;
                float _Amount;

                v2f vertexFunc(appdata IN)
                {
                    v2f OUT;

                    OUT.uv = IN.uv;

                    float3 normal = float3(0, 1, 0);
                    float h = tex2Dlod(_Displacement, float4(OUT.uv.xy, 0, 0)).r + tex2Dlod(_Displacement, float4(OUT.uv.xy, 0, 0)).g;
                    float3 vert = IN.vertex;
                    vert += normal * h * _Amount;

                    float3 n = tex2Dlod(_NormalMap, float4(OUT.uv.xy, 0, 0)).rgb;

                    half nl = max(0, dot(n, _WorldSpaceLightPos0.xyz));

                    OUT.diff = nl * _LightColor0;
                    //OUT.diff.rgb += ShadeSH9(half4(n, 1));

                    OUT.vertex = UnityObjectToClipPos(vert);

                    return OUT;
                }


                float4 fragmentFunc(v2f IN) : SV_Target
                {
                    float h = tex2D(_Displacement, float4(IN.uv.xy, 0, 0)).g * 3;
                    float4 colour = float4(0.5 - h, 0.3, 0.3 + h, 1);
                    colour *= IN.diff;

                    return colour;
                }

                ENDCG
            }

        }
    FallBack "Diffuse"
}
