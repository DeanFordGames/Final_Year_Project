Shader "Custom/Terrain"
{
    Properties
    {
        _Displacement("Displacement", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Amount("Amount", Range(0.1, 10.0)) = 0.5
        _MapSize("Map Size", float) = 0.0
        /*[Toggle(ANIMATE_WATER)] */_AnimateWater("Animate Water", int) = 0
    }
        SubShader
        {
            pass {
                Tags {"LightMode"="ForwardBase"}

                CGPROGRAM

                #pragma vertex vertexFunc
                #pragma fragment fragmentFunc
                //#pragma shader_feature ANIMATE_WATER

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
                float _MapSize;
                float _AnimateWater;

                v2f vertexFunc(appdata IN)
                {
                    v2f OUT;

                    OUT.uv = IN.uv;

                    float h = tex2Dlod(_Displacement, float4(OUT.uv.xy, 0, 0)).r;

                    //#ifdef ANIMATE_WATER
                    if (_AnimateWater == 1)
                    {
                        float uvDis = 1 / _MapSize;

                        float w = (tex2Dlod(_Displacement, float4(OUT.uv.xy, 0, 0)).g +
                            tex2Dlod(_Displacement, float4(OUT.uv.x + uvDis, OUT.uv.y, 0, 0)).g +
                            tex2Dlod(_Displacement, float4(OUT.uv.x - uvDis, OUT.uv.y, 0, 0)).g +
                            tex2Dlod(_Displacement, float4(OUT.uv.x, OUT.uv.y + uvDis, 0, 0)).g +
                            tex2Dlod(_Displacement, float4(OUT.uv.x, OUT.uv.y - uvDis, 0, 0)).g) / 5;
                        h += w;
                    }
                    //#endif


                    float3 normal = float3(0, 1, 0);
                    float3 vert = IN.vertex;
                    vert += normal * h * _Amount;

                    float3 n = tex2Dlod(_NormalMap, float4(OUT.uv.xy, 0, 0)).rgb;

                    half nl = max(0, dot(n, _WorldSpaceLightPos0.xyz)) * 6;

                    OUT.diff = nl * _LightColor0;

                    OUT.vertex = UnityObjectToClipPos(vert);

                    return OUT;
                }


                float4 fragmentFunc(v2f IN) : SV_Target
                {
                    float4 colour = float4(0.0f, 0.0f, 0.0f, 0.0f);
                    //#ifdef ANIMATE_WATER
                    if (_AnimateWater == 1)
                    {
                        float h = tex2D(_Displacement, float4(IN.uv.xy, 0, 0)).g * 3;
                        colour = float4(0.5 - h, 0.3, 0.3 + h, 1);
                    }else      //#else
                        colour = float4(0.5, 0.3, 0.3, 1);
                    //#endif

                    colour *= IN.diff;

                    return colour;
                }

                ENDCG
            }

        }
    FallBack "Diffuse"
}
