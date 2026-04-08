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
            float3 cameraForward;
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

            float3 ClosestPointOnSegment(float3 p, float3 segmentStart, float3 segmentEnd, out float segmentT)
            {
                float3 segmentVector = segmentEnd - segmentStart;
                float segmentLengthSq = max(dot(segmentVector, segmentVector), EPSILON);
                segmentT = saturate(dot(p - segmentStart, segmentVector) / segmentLengthSq);
                return segmentStart + segmentVector * segmentT;
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
                float4 baseColor = float4(1.0f, 0.0f, 1.0f, 0);
                const float circleRadiusPx = 16.0f;
                const float centerEpsilon = 0.01f;
                const float localBorderWidth = 0.015f;

                if (_DebugMode >= 1.0f && _DebugMode < 2.0f)
                {
                    return baseColor;
                }

                if (_DebugMode >= 2.0f && _DebugMode < 3.0f)
                {
                    return float4(uv.x, uv.y, 0.0f, 1.0f);
                }

                float3 leftCenterWS = leftPosition;
                float4 leftClip = mul(UNITY_MATRIX_VP, float4(leftCenterWS, 1.0f));
                if (leftClip.w <= 0.0f)
                {
                    return baseColor;
                }

                float2 leftNdc = leftClip.xy / max(abs(leftClip.w), EPSILON);
                float2 leftUv = leftNdc * 0.5f + 0.5f;
                float2 fragPx = uv * _ScreenParams.xy;
                float2 leftPx = leftUv * _ScreenParams.xy;
                float distanceToCenterPx = distance(fragPx, leftPx);
                float inCircle = step(distanceToCenterPx, circleRadiusPx);
                float3 leftMapped = mul(leftWorldToSdf, float4(leftCenterWS, 1.0f)).xyz;
                float isCentered = step(length(leftMapped), centerEpsilon);
                float4 circleColor = lerp(float4(1.0f, 0.0f, 0.0f, 1.0f), float4(0.0f, 1.0f, 0.0f, 1.0f), isCentered);
                float4 color = lerp(baseColor, circleColor, inCircle * circleColor.a);

                float3 centerVS = mul(UNITY_MATRIX_V, float4(leftCenterWS, 1.0f)).xyz;
                if (abs(centerVS.z) <= EPSILON)
                {
                    return color;
                }

                float2 ndc = uv * 2.0f - 1.0f;
                float4 clipFar = float4(ndc, 1.0f, 1.0f);
                float4 viewFar = mul(unity_CameraInvProjection, clipFar);
                float3 viewDir = viewFar.xyz / max(abs(viewFar.w), EPSILON);
                float viewDirZ = abs(viewDir.z) > EPSILON ? viewDir.z : (viewDir.z >= 0.0f ? EPSILON : -EPSILON);
                float tView = centerVS.z / viewDirZ;
                float3 sampleVS = viewDir * tView;
                float3 samplePositionWS = mul(unity_CameraToWorld, float4(sampleVS, 1.0f)).xyz;
                float3 centerLS = mul(leftWorldToSdf, float4(leftCenterWS, 1.0f)).xyz;
                float3 sampleLSRaw = mul(leftWorldToSdf, float4(samplePositionWS, 1.0f)).xyz;
                float3 sampleLS = sampleLSRaw - centerLS;

                float maxAbsLocal = max(max(abs(sampleLS.x), abs(sampleLS.y)), abs(sampleLS.z));
                float insideDist = 0.5f - maxAbsLocal;
                float outsideBorder = 1.0f - smoothstep(localBorderWidth, localBorderWidth * 2.0f, abs(insideDist));

                float3 uvw = clamp(sampleLS + 0.5f, 0.0f, 1.0f);
                float sdfValue = tex3Dlod(leftSDF, float4(uvw, 0.0f)).r;
                float sdfWidth = max(0.004f, abs(ddx(sdfValue)) + abs(ddy(sdfValue)));
                float sdfZeroBorder = 1.0f - smoothstep(sdfWidth, sdfWidth * 2.0f, abs(sdfValue));
                sdfZeroBorder *= step(-localBorderWidth, insideDist);

                if (_DebugMode >= 3.0f && _DebugMode < 4.0f)
                {
                    float3 localVis = float3(
                        saturate(sampleLS.x + 0.5f),
                        saturate(sampleLS.y + 0.5f),
                        saturate(0.5f + insideDist)
                    );
                    return float4(localVis, 1.0f);
                }

                if (_DebugMode >= 4.0f && _DebugMode < 5.0f)
                {
                    float signedVis = saturate(0.5f + sdfValue * 8.0f);
                    return float4(signedVis, 1.0f - signedVis, sdfZeroBorder, 1.0f);
                }

                if (_DebugMode >= 5.0f)
                {
                    return float4(outsideBorder, sdfZeroBorder, 0.0f, 1.0f);
                }

                float4 sdfBorderColor = float4(0.0f, 1.0f, 0.0f, 1.0f);
                float4 outsideBorderColor = float4(1.0f, 0.0f, 0.0f, 1.0f);
                color.rgb = lerp(color.rgb, sdfBorderColor.rgb, sdfZeroBorder);
                color.a = max(color.a, sdfZeroBorder * sdfBorderColor.a);
                color.rgb = lerp(color.rgb, outsideBorderColor.rgb, outsideBorder);
                color.a = max(color.a, outsideBorder * outsideBorderColor.a);

                return color;

                // float3 rayOrigin = _WorldSpaceCameraPos;
                // float3 rayDirection = GetRayDirection(uv);
                // float3 bridgeVector = rightPosition - leftPosition;
                // float bridgeDistance = max(length(bridgeVector), EPSILON);
                // float3 bridgeDirection = bridgeVector / bridgeDistance;
                // float3 planeNormal = normalize(cameraForward);
                // float planeDenom = dot(rayDirection, planeNormal);
                // if (abs(planeDenom) <= EPSILON)
                // {
                //     return float4(0.0f, 0.0f, 0.0f, 0.0f);
                // }

                // float3 bridgeMidPoint = (leftPosition + rightPosition) * 0.5f;
                // float tPlane = dot(bridgeMidPoint - rayOrigin, planeNormal) / planeDenom;
                // if (tPlane <= 0.0f)
                // {
                //     return float4(0.0f, 0.0f, 0.0f, 0.0f);
                // }

                // float3 samplePositionWS = rayOrigin + rayDirection * tPlane;
                // float thinDistance = max(_ThinDistance, EPSILON);
                // float along = dot(samplePositionWS - leftPosition, bridgeDirection);
                // float alongMargin = max(0.05f, bridgeDistance * 0.2f);
                // float startGate = smoothstep(-alongMargin, 0.0f, along);
                // float endGate = 1.0f - smoothstep(bridgeDistance, bridgeDistance + alongMargin, along);
                // float betweenGate = startGate * endGate;

                // float segmentT = 0.0f;
                // float3 closestOnBridge = ClosestPointOnSegment(samplePositionWS, leftPosition, rightPosition, segmentT);
                // float radialDistance = length(samplePositionWS - closestOnBridge);
                // float rangeRadius = max(_ConnectionRange, 0.02f);
                // float rangeGate = saturate(1.0f - radialDistance / rangeRadius);
                // rangeGate *= rangeGate;

                // float leftDistance = 0.0f;
                // float rightDistance = 0.0f;
                // if (_UseMockInput > 0.5f)
                // {
                //     leftDistance = distance(samplePositionWS, leftPosition) - _MockRadius;
                //     rightDistance = distance(samplePositionWS, rightPosition) - _MockRadius;
                // }
                // else
                // {
                //     float3 leftPositionLS = mul(leftWorldToSdf, float4(samplePositionWS, 1.0f)).xyz;
                //     float3 rightPositionLS = mul(rightWorldToSdf, float4(samplePositionWS, 1.0f)).xyz;
                //     leftDistance = SampleSDF(leftSDF, leftPositionLS);
                //     rightDistance = SampleSDF(rightSDF, rightPositionLS);
                // }

                // float leftOutsideDistance = max(leftDistance, 0.0f);
                // float rightOutsideDistance = max(rightDistance, 0.0f);
                // float bothDistance = 0.5f * (leftOutsideDistance + rightOutsideDistance);
                // float thinFactor = saturate(bothDistance / thinDistance);
                // float connectionThickness = lerp(_NearConnectionThickness, _FarConnectionThickness, thinFactor);
                // connectionThickness = max(connectionThickness, EPSILON);

                // float overlap = saturate((connectionThickness - max(leftOutsideDistance, rightOutsideDistance)) / connectionThickness);
                // float outsideGate = step(0.0f, leftDistance) * step(0.0f, rightDistance);
                // float grey = overlap * betweenGate * rangeGate * outsideGate;
                // grey = saturate(grey);

                // if (_UseMockInput > 0.5f)
                // {
                //     float mockTube = rangeGate;
                //     float mockAlong = smoothstep(0.0f, 0.1f, segmentT) * (1.0f - smoothstep(0.9f, 1.0f, segmentT));
                //     grey = max(grey, mockTube * mockAlong);
                // }

                // if (_DebugMode >= 3.0f && _DebugMode < 4.0f)
                // {
                //     return float4(betweenGate, rangeGate, outsideGate, 1.0f);
                // }

                // if (_DebugMode >= 4.0f && _DebugMode < 5.0f)
                // {
                //     float overlapVis = saturate(overlap);
                //     float thicknessVis = saturate(connectionThickness / max(_NearConnectionThickness, EPSILON));
                //     return float4(overlapVis, thicknessVis, grey, 1.0f);
                // }

                // if (_DebugMode >= 5.0f)
                // {
                //     return float4(grey, grey, grey, 1.0f);
                // }

                // float finalGrey = pow(saturate(grey), 0.7f);
                // float finalAlpha = saturate(finalGrey * _Alpha);
                // return float4(finalGrey, finalGrey, finalGrey, finalAlpha);
            }
            ENDHLSL
        }
    }
}
