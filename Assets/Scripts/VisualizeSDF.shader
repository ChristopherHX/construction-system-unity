Shader "Unlit/VolumeShader"
{
    Properties
    {
        leftSDF ("Left SDF", 3D) = "white" {}
        rightSDF ("Right SDF", 3D) = "white" {}
        _Alpha ("Alpha", float) = 0.35
        _StepSize ("Step Size", float) = 0.01
        _NearConnectionThickness ("Near Connection Thickness", float) = 0.5
        _FarConnectionThickness ("Far Connection Thickness", float) = 0.12
        _ThinDistance ("Thin Distance", float) = 4.0
        _ConnectionRange ("Connection Range", float) = 8.0
        _DebugMode ("Debug Mode", float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Maximum number of raymarching samples
            #define MAX_STEP_COUNT 512

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler3D leftSDF;
            sampler3D rightSDF;

            float4x4 leftWorldToSdf;
            float4x4 rightWorldToSdf;
            float3 leftPosition;
            float3 rightPosition;

            float _Alpha;
            float _StepSize;
            float _NearConnectionThickness;
            float _FarConnectionThickness;
            float _ThinDistance;
            float _ConnectionRange;
            float _DebugMode;

            v2f vert (appdata v)
            {
                v2f o;
                // Procedural fullscreen triangle from vertex ID.
                float2 uv = float2((v.vertexID << 1) & 2, v.vertexID & 2);
                o.vertex = float4(uv * 2.0f - 1.0f, 0.0f, 1.0f);
                o.uv = uv;
                return o;
            }

            float4 BlendUnder(float4 color, float4 newColor)
            {
                color.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
                color.a += (1.0 - color.a) * newColor.a;
                return color;
            }

            bool RayBoxIntersection(float3 rayOrigin, float3 rayDirection, out float tMin, out float tMax)
            {
                float3 boundsMin = float3(-0.5f, -0.5f, -0.5f);
                float3 boundsMax = float3(0.5f, 0.5f, 0.5f);

                float3 invRayDirection = 1.0f / rayDirection;
                float3 t0 = (boundsMin - rayOrigin) * invRayDirection;
                float3 t1 = (boundsMax - rayOrigin) * invRayDirection;

                float3 nearPlane = min(t0, t1);
                float3 farPlane = max(t0, t1);

                tMin = max(max(nearPlane.x, nearPlane.y), nearPlane.z);
                tMax = min(min(farPlane.x, farPlane.y), farPlane.z);
                return tMax >= max(tMin, 0.0f);
            }

            bool RaySphereIntersection(float3 rayOrigin, float3 rayDirection, float3 center, float radius, out float tMin, out float tMax)
            {
                float3 oc = rayOrigin - center;
                float b = dot(oc, rayDirection);
                float c = dot(oc, oc) - radius * radius;
                float discriminant = b * b - c;
                if (discriminant < 0.0f)
                {
                    tMin = 0.0f;
                    tMax = 0.0f;
                    return false;
                }

                float s = sqrt(discriminant);
                tMin = -b - s;
                tMax = -b + s;
                return tMax >= max(tMin, 0.0f);
            }

            float SampleSDF(sampler3D sdfTex, float3 localPosition)
            {
                float3 clampedPos = clamp(localPosition, -0.5f, 0.5f);
                float boundary = tex3Dlod(sdfTex, float4(clampedPos + 0.5f, 0.0f)).r;
                float outsideDistance = length(localPosition - clampedPos);
                return boundary + outsideDistance;
            }

            float3 GetRayDirection(float2 uv)
            {
                float2 ndc = uv * 2.0f - 1.0f;
                float4 clipPoint = float4(ndc, UNITY_NEAR_CLIP_VALUE, 1.0f);
                float4 viewPoint = mul(unity_CameraInvProjection, clipPoint);
                viewPoint.xyz /= max(viewPoint.w, EPSILON);
                float3 worldPoint = mul(unity_CameraToWorld, float4(viewPoint.xyz, 1.0f)).xyz;
                return normalize(worldPoint - _WorldSpaceCameraPos);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = saturate(i.uv);

                if (_DebugMode >= 1.0f && _DebugMode < 2.0f)
                {
                    return float4(1.0f, 0.0f, 1.0f, 1.0f);
                }

                if (_DebugMode >= 2.0f && _DebugMode < 3.0f)
                {
                    return float4(uv.x, uv.y, 0.0f, 1.0f);
                }

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = GetRayDirection(uv);
                float3 midPoint = (leftPosition + rightPosition) * 0.5f;
                float3 bridgeVector = rightPosition - leftPosition;
                float bridgeDistance = max(length(bridgeVector), EPSILON);
                float3 bridgeDirection = bridgeVector / bridgeDistance;
                float tMid = dot(midPoint - rayOrigin, rayDirection);

                float3 leftRayOrigin = mul(leftWorldToSdf, float4(rayOrigin, 1.0f)).xyz;
                float3 leftRayDirection = mul((float3x3)leftWorldToSdf, rayDirection);
                float3 rightRayOrigin = mul(rightWorldToSdf, float4(rayOrigin, 1.0f)).xyz;
                float3 rightRayDirection = mul((float3x3)rightWorldToSdf, rayDirection);

                float leftTMin = 0.0f;
                float leftTMax = 0.0f;
                float rightTMin = 0.0f;
                float rightTMax = 0.0f;

                bool leftHit = RayBoxIntersection(leftRayOrigin, leftRayDirection, leftTMin, leftTMax);
                bool rightHit = RayBoxIntersection(rightRayOrigin, rightRayDirection, rightTMin, rightTMax);

                float rayStart = 0.0f;
                float rayEnd = 0.0f;
                if (leftHit || rightHit)
                {
                    rayStart = leftHit ? leftTMin : rightTMin;
                    rayEnd = leftHit ? leftTMax : rightTMax;
                    if (rightHit)
                    {
                        rayStart = min(rayStart, rightTMin);
                        rayEnd = max(rayEnd, rightTMax);
                    }
                }
                else
                {
                    float sphereRadius = bridgeDistance * 0.5f + _ConnectionRange;
                    if (!RaySphereIntersection(rayOrigin, rayDirection, midPoint, sphereRadius, rayStart, rayEnd))
                    {
                        return float4(0.0f, 0.0f, 0.0f, 0.0f);
                    }
                }

                rayStart = max(rayStart, 0.0f);
                float focusHalfRange = max(_ConnectionRange, bridgeDistance);
                rayStart = max(rayStart, tMid - focusHalfRange);
                rayEnd = min(rayEnd, tMid + focusHalfRange);
                if (rayEnd <= rayStart)
                {
                    return float4(0.0f, 0.0f, 0.0f, 0.0f);
                }

                if (_DebugMode >= 3.0f && _DebugMode < 4.0f)
                {
                    float hitLength = max(rayEnd - rayStart, 0.0f);
                    float hitLengthVis = saturate(hitLength / 5.0f);
                    return float4(leftHit ? 1.0f : 0.0f, rightHit ? 1.0f : 0.0f, hitLengthVis, 1.0f);
                }

                float t = rayStart;
                float segmentLength = max(rayEnd - rayStart, EPSILON);
                float stepSize = max(_StepSize, segmentLength / (float)MAX_STEP_COUNT);
                float thinDistance = max(_ThinDistance, EPSILON);
                float minDistanceDifference = 1e9f;
                float minBothDistance = 1e9f;
                float maxGrey = 0.0f;
                float marchedStepCount = 0.0f;
                int maxMarchSteps = min(MAX_STEP_COUNT, (int)ceil(segmentLength / stepSize));

                for (int step = 0; step < MAX_STEP_COUNT && step < maxMarchSteps && t <= rayEnd; step++)
                {
                    float3 samplePositionWS = rayOrigin + rayDirection * t;

                    float3 leftPositionLS = mul(leftWorldToSdf, float4(samplePositionWS, 1.0f)).xyz;
                    float3 rightPositionLS = mul(rightWorldToSdf, float4(samplePositionWS, 1.0f)).xyz;

                    float leftDistance = SampleSDF(leftSDF, leftPositionLS);
                    float rightDistance = SampleSDF(rightSDF, rightPositionLS);

                    // Connection is centered where both SDF distances are similar.
                    float distanceDifference = abs(leftDistance - rightDistance);
                    float bothDistance = (leftDistance + rightDistance) * 0.5f;

                    // When both SDFs are far from their surfaces, reduce the overlap thickness.
                    float thinFactor = saturate(max(bothDistance, 0.0f) / thinDistance);
                    float connectionThickness = lerp(_NearConnectionThickness, _FarConnectionThickness, thinFactor);
                    connectionThickness = max(connectionThickness, EPSILON);

                    float coreBand = saturate((connectionThickness - distanceDifference) / connectionThickness);

                    // Restrict bridge effect to a bounded region between left and right centers.
                    float outsideMeanDistance = max(bothDistance, 0.0f);
                    float rangeFade = saturate(1.0f - outsideMeanDistance / max(_ConnectionRange, EPSILON));
                    rangeFade *= rangeFade;

                    float along = dot(samplePositionWS - leftPosition, bridgeDirection);
                    float alongMargin = max(connectionThickness * 2.0f, 0.05f);
                    float startGate = smoothstep(-alongMargin, 0.0f, along);
                    float endGate = 1.0f - smoothstep(bridgeDistance, bridgeDistance + alongMargin, along);
                    float betweenGate = startGate * endGate;

                    float3 closestOnBridge = leftPosition + bridgeDirection * saturate(along / bridgeDistance) * bridgeDistance;
                    float radialDistance = length(samplePositionWS - closestOnBridge);
                    float radialRadius = bridgeDistance * 0.6f + _ConnectionRange;
                    float radialGate = saturate(1.0f - radialDistance / max(radialRadius, EPSILON));
                    radialGate *= radialGate;

                    float grey = coreBand * rangeFade * betweenGate * radialGate;
                    grey = smoothstep(0.05f, 1.0f, grey);
                    minDistanceDifference = min(minDistanceDifference, distanceDifference);
                    minBothDistance = min(minBothDistance, bothDistance);
                    maxGrey = max(maxGrey, grey);
                    marchedStepCount += 1.0f;

                    t += stepSize;
                }

                if (_DebugMode >= 4.0f && _DebugMode < 5.0f)
                {
                    float diffVis = saturate(1.0f - minDistanceDifference / max(_NearConnectionThickness, EPSILON));
                    float distVis = saturate(1.0f - max(minBothDistance, 0.0f) / max(_ConnectionRange, EPSILON));
                    float stepVis = saturate(marchedStepCount / 64.0f);
                    return float4(diffVis, distVis, stepVis, 1.0f);
                }

                if (_DebugMode >= 5.0f)
                {
                    float alphaVis = saturate(maxGrey);
                    return float4(alphaVis, alphaVis, alphaVis, 1.0f);
                }

                float finalGrey = saturate(maxGrey);
                float finalAlpha = saturate(finalGrey * _Alpha);
                float premultGrey = finalGrey * finalAlpha;
                return float4(premultGrey, premultGrey, premultGrey, finalAlpha);
            }
            ENDHLSL
        }
    }
}
