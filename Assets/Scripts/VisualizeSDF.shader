Shader "Unlit/VolumeShader"
{
    Properties
    {
        leftSDF ("Left SDF", 3D) = "white" {}
        rightSDF ("Right SDF", 3D) = "white" {}
        _Alpha ("Alpha", float) = 0.4
        _StepSize ("Step Size", float) = 0.02
        _NearConnectionThickness ("Near Connection Thickness", float) = 0.25
        _FarConnectionThickness ("Far Connection Thickness", float) = 0.06
        _ThinDistance ("Thin Distance", float) = 2.0
        _ConnectionRange ("Connection Range", float) = 0.6
        _UseMockInput ("Use Mock Input", float) = 0
        _MockRadius ("Mock Radius", float) = 0.5
        _DebugMode ("Debug Mode", float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
            float _UseMockInput;
            float _MockRadius;
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

            void ClosestRayToSegment(
                float3 rayOrigin,
                float3 rayDirection,
                float3 segmentStart,
                float3 segmentEnd,
                out float rayT,
                out float segmentT,
                out float distanceToSegment)
            {
                float3 segmentVector = segmentEnd - segmentStart;
                float segmentLengthSq = max(dot(segmentVector, segmentVector), EPSILON);
                float raySegmentDot = dot(rayDirection, segmentVector);
                float3 originToSegment = rayOrigin - segmentStart;
                float rayOriginDot = dot(rayDirection, originToSegment);
                float segmentOriginDot = dot(segmentVector, originToSegment);

                float denom = segmentLengthSq - raySegmentDot * raySegmentDot;
                float unclampedSegmentT = 0.5f;
                if (abs(denom) > EPSILON)
                {
                    unclampedSegmentT = (segmentOriginDot - raySegmentDot * rayOriginDot) / denom;
                }

                segmentT = saturate(unclampedSegmentT);
                float3 closestOnSegment = segmentStart + segmentVector * segmentT;
                rayT = max(dot(closestOnSegment - rayOrigin, rayDirection), 0.0f);
                float3 closestOnRay = rayOrigin + rayDirection * rayT;
                distanceToSegment = length(closestOnRay - closestOnSegment);
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
                float3 bridgeVector = rightPosition - leftPosition;
                float bridgeDistance = max(length(bridgeVector), EPSILON);
                float3 bridgeDirection = bridgeVector / bridgeDistance;

                float closestRayT = 0.0f;
                float closestBridgeT = 0.0f;
                float rayToBridgeDistance = 0.0f;
                ClosestRayToSegment(rayOrigin, rayDirection, leftPosition, rightPosition, closestRayT, closestBridgeT, rayToBridgeDistance);

                float gateRadius = _ConnectionRange + bridgeDistance * 0.3f;
                if (rayToBridgeDistance > gateRadius)
                {
                    return float4(0.0f, 0.0f, 0.0f, 0.0f);
                }

                if (_DebugMode >= 3.0f && _DebugMode < 4.0f)
                {
                    float gateVis = saturate(1.0f - rayToBridgeDistance / max(gateRadius, EPSILON));
                    return float4(gateVis, closestBridgeT, saturate(closestRayT / 20.0f), 1.0f);
                }

                float3 samplePositionWS = rayOrigin + rayDirection * closestRayT;
                float thinDistance = max(_ThinDistance, EPSILON);
                float leftDistance = 0.0f;
                float rightDistance = 0.0f;
                if (_UseMockInput > 0.5f)
                {
                    leftDistance = distance(samplePositionWS, leftPosition) - _MockRadius;
                    rightDistance = distance(samplePositionWS, rightPosition) - _MockRadius;
                }
                else
                {
                    float3 leftPositionLS = mul(leftWorldToSdf, float4(samplePositionWS, 1.0f)).xyz;
                    float3 rightPositionLS = mul(rightWorldToSdf, float4(samplePositionWS, 1.0f)).xyz;
                    leftDistance = SampleSDF(leftSDF, leftPositionLS);
                    rightDistance = SampleSDF(rightSDF, rightPositionLS);
                }

                float distanceDifference = abs(leftDistance - rightDistance);
                float bothDistance = (leftDistance + rightDistance) * 0.5f;
                float thinFactor = saturate(max(bothDistance, 0.0f) / thinDistance);
                float connectionThickness = lerp(_NearConnectionThickness, _FarConnectionThickness, thinFactor);
                connectionThickness = max(connectionThickness, EPSILON);

                float coreBand = saturate((connectionThickness - distanceDifference) / connectionThickness);
                float outsideMeanDistance = max(bothDistance, 0.0f);
                float rangeFade = saturate(1.0f - outsideMeanDistance / max(_ConnectionRange, EPSILON));

                float along = dot(samplePositionWS - leftPosition, bridgeDirection);
                float alongMargin = max(connectionThickness * 1.5f, 0.02f);
                float startGate = smoothstep(-alongMargin, 0.0f, along);
                float endGate = 1.0f - smoothstep(bridgeDistance, bridgeDistance + alongMargin, along);
                float betweenGate = startGate * endGate;

                float3 closestOnBridge = leftPosition + bridgeDirection * saturate(along / bridgeDistance) * bridgeDistance;
                float radialDistance = length(samplePositionWS - closestOnBridge);
                float radialRadius = max(connectionThickness * 2.5f, _ConnectionRange + bridgeDistance * 0.1f);
                float radialGate = saturate(1.0f - radialDistance / max(radialRadius, EPSILON));
                radialGate *= radialGate;

                float grey = coreBand * rangeFade * betweenGate * radialGate;
                grey = saturate(grey);

                if (_UseMockInput > 0.5f)
                {
                    float mockTube = saturate(1.0f - rayToBridgeDistance / max(gateRadius, EPSILON));
                    float mockAlong = smoothstep(0.0f, 0.1f, closestBridgeT) * (1.0f - smoothstep(0.9f, 1.0f, closestBridgeT));
                    grey = max(grey, mockTube * mockAlong);
                }

                if (_DebugMode >= 4.0f && _DebugMode < 5.0f)
                {
                    float coreVis = saturate(coreBand);
                    float rangeVis = saturate(rangeFade);
                    float gateVis = saturate(1.0f - rayToBridgeDistance / max(gateRadius, EPSILON));
                    return float4(coreVis, rangeVis, gateVis, 1.0f);
                }

                if (_DebugMode >= 5.0f)
                {
                    return float4(grey, grey, grey, 1.0f);
                }

                float finalGrey = pow(saturate(grey), 0.7f);
                float finalAlpha = saturate(finalGrey * _Alpha);
                return float4(finalGrey, finalGrey, finalGrey, finalAlpha);
            }
            ENDHLSL
        }
    }
}
