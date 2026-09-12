Shader "Fractals/Bounded Menger Raymarch"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0.015, 0.08, 0.20, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.0, 0.35, 1.2, 1)
        _GlowStrength ("Glow Strength", Range(0, 3)) = 0.28
        _Iterations ("Fractal Iterations", Range(1, 10)) = 8
        _MaxSteps ("Maximum Ray Steps", Range(8, 128)) = 64
        _SurfaceEpsilon ("Surface Epsilon", Range(0.0001, 0.01)) = 0.0015
        _StepScale ("Step Scale", Range(0.4, 1.0)) = 0.82
        _MaxDistance ("Maximum Local Distance", Range(0.25, 4.0)) = 2.0
        _LightDirection ("Light Direction", Vector) = (-0.4, 0.8, -0.5, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "BoundedMengerRaymarch"
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlowColor;
                float _GlowStrength;
                float _Iterations;
                float _MaxSteps;
                float _SurfaceEpsilon;
                float _StepScale;
                float _MaxDistance;
                float4 _LightDirection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            float SdBox(float3 p, float3 b)
            {
                float3 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }

            // Start with a unit cube, then subtract Menger cross-shaped holes.
            // Each subtraction is a signed-distance Boolean operation. This avoids
            // the zero-distance interiors that otherwise stall/band ray steps.
            float MengerDistance(float3 p)
            {
                float distanceEstimate = SdBox(p, 0.5.xxx);
                int iterations = (int)round(_Iterations);

                [loop]
                for (int i = 1; i <= 10; i++)
                {
                    if (i > iterations) break;
                    float grid = pow(3.0, (float)i);
                    float3 cell = (frac((p + 0.5) * grid) - 0.5) / grid;
                    float halfCell = 0.5 / grid;
                    float band = halfCell / 3.0;
                    float cutXY = SdBox(cell, float3(band, band, halfCell));
                    float cutYZ = SdBox(cell, float3(halfCell, band, band));
                    float cutXZ = SdBox(cell, float3(band, halfCell, band));
                    float cross = min(cutXY, min(cutYZ, cutXZ));
                    distanceEstimate = max(distanceEstimate, -cross);
                }

                return distanceEstimate;
            }

            bool IntersectUnitBox(float3 rayOrigin, float3 rayDirection, out float startT, out float endT)
            {
                float3 inverseDirection = 1.0 / max(abs(rayDirection), 1e-5) * sign(rayDirection);
                float3 t0 = (-0.5 - rayOrigin) * inverseDirection;
                float3 t1 = ( 0.5 - rayOrigin) * inverseDirection;
                float3 nearT = min(t0, t1);
                float3 farT = max(t0, t1);
                startT = max(max(nearT.x, nearT.y), max(nearT.z, 0.0));
                endT = min(min(farT.x, farT.y), farT.z);
                return endT >= startT;
            }

            float3 EstimateNormal(float3 p, float epsilon)
            {
                float3 e = float3(epsilon, 0.0, 0.0);
                return normalize(float3(
                    MengerDistance(p + e.xyy) - MengerDistance(p - e.xyy),
                    MengerDistance(p + e.yxy) - MengerDistance(p - e.yxy),
                    MengerDistance(p + e.yyx) - MengerDistance(p - e.yyx)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 rayDirection = normalize(input.positionOS - rayOrigin);
                float startT;
                float endT;
                if (!IntersectUnitBox(rayOrigin, rayDirection, startT, endT)) discard;

                endT = min(endT, startT + _MaxDistance);
                float t = startT + max(_SurfaceEpsilon * 4.0, 0.006);
                bool foundSurface = false;
                float3 hitPoint = 0.0;
                int stepLimit = (int)round(_MaxSteps);

                // Hard capped to 128. Rays terminate on hit, box exit, or the configured step limit.
                [loop]
                for (int step = 0; step < 128; step++)
                {
                    if (step >= stepLimit || t > endT) break;
                    float3 p = rayOrigin + rayDirection * t;
                    float distanceEstimate = MengerDistance(p);
                    float epsilon = max(_SurfaceEpsilon, t * _SurfaceEpsilon * 0.15);
                    if (distanceEstimate < epsilon)
                    {
                        foundSurface = true;
                        hitPoint = p;
                        break;
                    }
                    t += max(distanceEstimate * _StepScale, epsilon * 0.25);
                }

                if (!foundSurface) discard;

                float normalEpsilon = max(_SurfaceEpsilon * 1.5, t * _SurfaceEpsilon * 0.2);
                float3 normal = EstimateNormal(hitPoint, normalEpsilon);
                float3 lightDirection = normalize(_LightDirection.xyz);
                float diffuse = saturate(dot(normal, lightDirection)) * 0.78 + 0.22;
                float edgeGlow = pow(saturate(1.0 - abs(dot(normal, -rayDirection))), 3.0);
                half3 color = _BaseColor.rgb * diffuse + _GlowColor.rgb * (edgeGlow * _GlowStrength);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
