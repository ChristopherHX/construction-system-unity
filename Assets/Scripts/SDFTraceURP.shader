Shader "Unlit/SDFTraceURP"
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

            float4 _Color;
            float3 _TestPosition;
            float4x4 _WorldToSDFSpace;
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
            float4 _ScatterParams;

            #define SCATTER_AMOUNT _ScatterParams.x
            #define SCATTER_START _ScatterParams.y
            #define SCATTER_STEP _ScatterParams.z
            #define SCATTER_MAX_DEPTH _ScatterParams.w

            float _DirScatterAmount;
            int _DirScatterMaxIterations;
            int _DirScatterMaxIterationsSecondary;
            float _ExtinctionCoeff;
            float _Anisotropy;
            float _BlendDistance;
            int _Mode;

            #define BOX_SCENE 0
            #define SPHERES_SCENE 1

            StructuredBuffer<float4> _Spheres;
            int _Spheres_Count;


            VertOutput vert(float4 vertex : POSITION)
            {
                VertOutput o;
                float3 positionWS = TransformObjectToWorld(vertex.xyz);
                o.vertex = TransformWorldToHClip(positionWS);
                o.world = positionWS;
                return o;
            }

            float SDFTex(float3 worldPos, float margin)
            {
                float3 sdfLocalPos = mul(_WorldToSDFSpace, float4(worldPos, 1)).xyz;

                // Check if inside the texture volume, fixes glitches for outer tests that may clip
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

            // #define r(x) frac(1e4 * sin((x) * 541.17))
            // #define sr2(x) (r(float2(x, x + 0.1)) * 2.0 - 1.0)
            // #define sr3(x) (r(float4(x, x + 0.1, x + 0.2, 0)) * 2.0 - 1.0)

            float SpheresScene(float3 worldPos)
            {
                uint count, stride;
                // _Spheres.GetDimensions(count, stride);
                stride = 4 * 4;
                count = _Spheres_Count;
                float dist = 10000000.0;
                for (uint i = 0; i < count; i++)
                {
                    float4 sphere = _Spheres[i];
                    dist = SmoothMinCubic(dist, Sphere(worldPos - sphere.xyz, sphere.w), _BlendDistance);
                }

                // Removes the objects
                dist = SmoothMaxCubic(-SDFTex(worldPos, _Margin), dist, _BlendDistance);

                return dist;
            }

            float BoxScene(float3 worldPos)
            {
                float dist = Box(worldPos, 0.03);
                uint count, stride;
                // // _Spheres.GetDimensions(count, stride);
                // stride = 4 * 4;
                count = _Spheres_Count;
                // //float dist = 10000000.0;

                // dist = SmoothMinCubic(max(-SDFTex(worldPos, _Margin2), 0), dist, 0.1);

                // Limit Outer (Does not always work somehow)
                dist = SmoothMaxCubic(SDFTex(worldPos, _Margin2), dist, _BlendDistance);
                // Limit Inner
                dist = SmoothMaxCubic(-SDFTex(worldPos, _Margin), dist, _BlendDistance);

                // for (uint i = 0; i < count; i++)
                // {
                //     float4 sphere = _Spheres[i];
                //     dist = SmoothMinCubic(dist, Sphere(worldPos - sphere.xyz, sphere.w), 0.01);
                // }
                // Connection SDF

                float insidedistance = distance(_Spheres[1].xyz, _Spheres[0].xyz) / 2;
                // calcualate the normed ray of the connection
                float3 ray = normalize(_Spheres[1].xyz - _Spheres[0].xyz);
                // calculate the anchor in the middle, so +- length / 2 is within
                float3 m = (_Spheres[0].xyz + _Spheres[1].xyz) / 2;
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
                if (_Mode == BOX_SCENE)
                    return BoxScene(worldPos);
                else
                    return SpheresScene(worldPos);
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

            // float G1V(float nv, float k)
            // {
            //     return 1.0 / (nv * (1.0 - k) + k);
            // }

            // float GGX(float3 n, float3 v, float3 l, float roughness, float f0)
            // {
            //     float alpha = roughness * roughness;

            //     float3 h = normalize(v + l);

            //     float nl = saturate(dot(n, l));
            //     float nv = saturate(dot(n, v));
            //     float nh = saturate(dot(n, h));
            //     float lh = saturate(dot(l, h));

            //     float f, d;

            //     float alphaSqr = alpha * alpha;
            //     float denom = nh * nh * (alphaSqr - 1.0) + 1.0;
            //     d = alphaSqr / (PI * denom * denom);

            //     float lh5 = pow(1.0 - lh, 5.0);
            //     f = f0 + (1.0 - f0) * lh5;

            //     float k = alpha;
            //     return nl * d * f * G1V(nl, k) * G1V(nv, k);
            // }

            // float Scatter(float3 p, float3 v, float3 n)
            // {
            //     float3 d = refract(v, n, 1.0 / 1.5);
            //     float3 o = p;
            //     float a = 0.0;

            //     for (float i = SCATTER_START; i < SCATTER_MAX_DEPTH; i += SCATTER_STEP)
            //     {
            //         o += i * d;
            //         float t = SampleSDF(o);
            //         if (t > 0)
            //             break;
            //         a += t;
            //     }
            //     float thickness = max(0.01, -a);
            //     return SCATTER_AMOUNT * pow(SCATTER_MAX_DEPTH * 0.5, 3.0) / thickness;
            // }

            // float Extinction(float thickness)
            // {
            //     return exp(-_ExtinctionCoeff * thickness);
            // }

            // float Anisotropy(float costheta)
            // {
            //     float g = _Anisotropy;
            //     float gsq = g * g;
            //     float denom = 1 + gsq - 2.0 * g * costheta;
            //     denom = denom * denom * denom;
            //     denom = sqrt(max(0, denom));
            //     return (1 - gsq) / denom;
            // }

            // float DirScatter(float3 p, float3 v, float3 n)
            // {
            //     // I mean, there's a lot to trim here, but we're just having fun
            //     float3 d = refract(v, n, 1.0 / 1.5);
            //     float a = 0.0;
            //     float3 pos = p;

            //     pos += SCATTER_START * d;
            //     for (int k = 0; k < 10; k++)
            //     {
            //         float t = SampleSDF(pos);
            //         pos -= t * d;
            //     }

            //     float thickness = length(p - pos);
            //     float stepSize = thickness / (float)_DirScatterMaxIterations;

            //     pos = p + SCATTER_START * d;
            //     for (int i = 0; i < _DirScatterMaxIterations; i++)
            //     {
            //         pos += stepSize * d;
            //         float t = SampleSDF(pos);
            //         if (t >= 0)
            //             break;

            //         float3 posbis = pos;
            //         float tbis = t;
            //         for (int j = 0; j < _DirScatterMaxIterationsSecondary; j++)
            //         {
            //             posbis += tbis * _LightDir;
            //             tbis = SampleSDF(posbis);
            //             if (tbis >= 0)
            //                 break;
            //         }

            //         float thicknessToLight = length(pos - posbis);
            //         float inscatter = Extinction(thicknessToLight);
            //         float thicknessToInscatterPos = length(p - pos);
            //         a += inscatter * Extinction(thicknessToInscatterPos) / max(thickness, 0.01);
            //     }

            //     float aniso = Anisotropy(dot(v, -_LightDir));
            //     return _DirScatterAmount * a * aniso;
            // }

            float3 Shade(float3 p, float3 v, float3 n)
            {
                float3 l = -_LightDir;

                float fresnel = pow(max(0.0, 1.0 + dot(n, v)), 5.0);
                float diffuse = max(0.0, dot(n, l));
                //float spec = GGX(n, v, l, 3.0, fresnel);

                // shading crimes
                fresnel *= 0.3;
                diffuse *= 0.3;
                //spec *= 50.0;

                //float scatter = Scatter(p, v, n) + DirScatter(p, v, n);

                return _Ambient + fresnel * _Sky + _LightColor * (_Albedo * diffuse /*+ _Albedo * scatter + spec**/);
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

                    // if(k == 0 || dist <= minDist) {
                    //     minDist = dist;
                    // } else {
                    //     break;
                    // }
                    // if(dist <= 0) {
                    //     wasInside = 1;
                    // } else if(wasInside) {
                    //     break;
                    // }

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
