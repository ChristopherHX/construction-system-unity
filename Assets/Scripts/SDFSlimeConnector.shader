Shader "Unlit/SDFSlimeConnector"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            // For rendering inside the cube
            Cull Off

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct VertOutput
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            struct FragOutput
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };

            float4x4 _WorldToSDFSpace;
            float4x4 _WorldToSDFSpace2;
            sampler3D _SDF;
            float _Margin;
            float _Margin2;
            float3 _BoxSize;
            float3 _BoxPos;
            float3 _LightColor;
            float3 _LightDir;
            float3 _Ambient;
            float3 _Albedo;
            float3 _Sky;

            float _BlendDistance;

            float3 left;
            float3 leftNormal;
            float3 right;
            float3 rightNormal;

            VertOutput vert(float4 vertex : POSITION)
            {
                VertOutput o;
                float3 positionWS = TransformObjectToWorld(vertex.xyz);
                o.vertex = TransformWorldToHClip(positionWS);
                o.world = positionWS;
                return o;
            }

            float SDFTex(int d, float3 worldPos, float margin)
            {
                float3 sdfLocalPos = mul(d ? _WorldToSDFSpace2 : _WorldToSDFSpace, float4(worldPos, 1)).xyz;

                // // Check if inside the texture volume, fixes glitches for outer tests that may clip
                bool inside = all(sdfLocalPos >= 0.0) && all(sdfLocalPos <= 1.0);
                float sdf = 1000000000;
                if (inside)
                {
                    sdf = tex3Dlod(_SDF, float4(sdfLocalPos, 0)).r;
                    // -_Margin to be able to converge on an isosurface other than 0.
                    sdf -= margin;
                }
                return sdf;
            }

            float Box(float3 worldPos, float radius)
            {
                float3 d = abs(worldPos - _BoxPos) - _BoxSize + radius;
                return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0)) - radius;
            }

            float Sphere(float3 worldPos, float radius)
            {
                return length(worldPos) - radius;
            }

            float SmoothMinCubic(float a, float b, float k)
            {
                float h = max(k - abs(a - b), 0.0);
                return min(a, b) - h * h * h / (6.0 * k * k);
            }

            float SmoothMaxCubic(float a, float b, float k)
            {
                float h = max(k - abs(a - b), 0.0);
                return max(a, b) + h * h * h / (6.0 * k * k);
            }

            float SurfaceBand(int idx, float3 p, float inner, float outer)
            {
                float s = SDFTex(idx, p, 0.0);
                return max(s - outer, -s - inner);
            }

            float BoxScene(float3 worldPos)
            {
                // float band0 = SurfaceBand(0, worldPos, _Margin, _Margin2);
                // // float band1 = SurfaceBand(1, worldPos, _Margin, _Margin2);

                // float dist = band0;//SmoothMinCubic(band0, band1, _BlendDistance);

                // float dist = Box(worldPos, 0.1);

                // Limit Outer (Does not always work somehow)
                // dist = SmoothMaxCubic(SDFTex(0, worldPos, _Margin2), dist, _BlendDistance);
                // Limit Inner
                float dist = SDFTex(0, worldPos, _Margin);//SmoothMinCubic(-SDFTex(0, worldPos, _Margin), 1000000000, _BlendDistance);
                
                // float dist2 = Box(worldPos, 1);

                // // Limit Outer (Does not always work somehow)
                // dist2 = SmoothMaxCubic(SDFTex(1, worldPos, _Margin2), dist2, _BlendDistance);
                // // Limit Inner
                // dist2 = SmoothMaxCubic(-SDFTex(1, worldPos, _Margin), dist2, _BlendDistance);
                // dist = min(dist, dist2);

                // Connection SDF
                float insidedistance = distance(right.xyz, left.xyz) / 2;
                // calcualate the normed ray of the connection
                float3 ray = normalize(right.xyz - left.xyz);
                // calculate the anchor in the middle, so +- length / 2 is within
                float3 m = (left.xyz + right.xyz) / 2;
                // Limit the sphere root to be inside the connection line
                float p_a = clamp(dot(worldPos - m, ray), -insidedistance, insidedistance);
                // Worldpos with limited connection line
                float3 c = m + p_a * ray;
                // Now a single sphere on the calculated sphere center
                // Use exponential size b
                dist = SmoothMinCubic(dist, Sphere(worldPos - c, exp(2 * abs(p_a) / insidedistance) * 0.01 + 0.01), 0.02);
                
                // worldPos
                return dist;
            }

            float SampleSDF(float3 worldPos)
            {
                return BoxScene(worldPos);
            }

            // For rendering inside the cube
            bool IntersectTraceBounds(float3 rayOrigin, float3 rayDir, out float entryDistance, out float exitDistance)
            {
                float3 boxMin = _BoxPos - _BoxSize;
                float3 boxMax = _BoxPos + _BoxSize;

                float3 safeDir = rayDir;
                safeDir.x = abs(safeDir.x) < 1e-6 ? (safeDir.x < 0.0 ? -1e-6 : 1e-6) : safeDir.x;
                safeDir.y = abs(safeDir.y) < 1e-6 ? (safeDir.y < 0.0 ? -1e-6 : 1e-6) : safeDir.y;
                safeDir.z = abs(safeDir.z) < 1e-6 ? (safeDir.z < 0.0 ? -1e-6 : 1e-6) : safeDir.z;

                float3 t0 = (boxMin - rayOrigin) / safeDir;
                float3 t1 = (boxMax - rayOrigin) / safeDir;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);

                entryDistance = max(max(tMin.x, tMin.y), tMin.z);
                exitDistance = min(min(tMax.x, tMax.y), tMax.z);

                return exitDistance >= max(entryDistance, 0.0);
            }
            // End For rendering inside the cube

            float3 CalcNormal(float3 p)
            {
                // Neighborhood size should be the size of a voxel in case of the sdf tex.
                // TODO: make dynamic
                // TODO: adjust for the fact we're sampling in a tetrahedral pattern.
                float eps = 0.02;

                float2 off = float2(1, -1);
                return normalize(off.xyy * SampleSDF(p + off.xyy * eps) +
                                 off.yyx * SampleSDF(p + off.yyx * eps) +
                                 off.yxy * SampleSDF(p + off.yxy * eps) +
                                 off.xxx * SampleSDF(p + off.xxx * eps));
            }

            float3 Shade(float3 p, float3 v, float3 n)
            {
                float3 l = -_LightDir;

                float fresnel = pow(max(0.0, 1.0 + dot(n, v)), 5.0);
                float diffuse = max(0.0, dot(n, l));
                fresnel *= 0.3;
                diffuse *= 0.3;
                return _Ambient + fresnel * _Sky + _LightColor * (_Albedo * diffuse);
            }

            FragOutput frag(VertOutput i)
            {
                float3 cam = GetCameraPositionWS();
                float3 pos = i.world;

                float3 dir = normalize(pos - cam);
                // For rendering inside the cube
                float entryDistance;
                float exitDistance;
                if (IntersectTraceBounds(cam, dir, entryDistance, exitDistance))
                {
                    float startDistance = max(entryDistance, max(_ProjectionParams.y, 0.001));
                    clip(exitDistance - startDistance);
                    pos = cam + dir * startDistance;
                }
                // End For rendering inside the cube

                // float minDist = -1;
                bool wasInside = 0;

                // the count of k is relevant if it does not really converges a higher loop helps
                for (int k = 0; k < 128; k++)
                {
                    float dist = SampleSDF(pos);

                    // We don't early-out, because going through the full iteration count
                    // converges on the surface much better.
                    pos += dir * dist;
                }

                float3 normal = CalcNormal(pos);

                FragOutput o;
                o.color = Shade(pos, dir, normal).xyzz * 0.5;
                o.depth = ComputeNormalizedDeviceCoordinatesWithZ(pos, GetWorldToHClipMatrix()).z;

                return o;
            }
            ENDHLSL
        }
    }
}
