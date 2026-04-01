using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MeshTextureRegions
{
    public class ColorRegion
    {
        public Color color;
        public List<Vector2Int> pixels = new();
        public Vector2 centerUV;
        public Vector3 localPos;
        public Vector3 localNormal;
    }

    // ---------------------------------------------------------
    // PUBLIC ENTRY POINT
    // ---------------------------------------------------------
    public static List<ColorRegion> Extract(Texture2D tex, Mesh mesh, float tolerance = 0.01f)
    {
        var regions = ExtractColorRegions(tex, tolerance);
        ComputeRegionCenters(regions, tex.width, tex.height);
        ComputeMeshDataForRegions(regions, mesh);
        regions.RemoveAll(r => r.localNormal == Vector3.zero);
        return regions;
    }

    // ---------------------------------------------------------
    // 1. FLOOD-FILL ALL COLOR REGIONS
    // ---------------------------------------------------------
    private static List<ColorRegion> ExtractColorRegions(Texture2D tex, float tolerance)
    {
        int w = tex.width;
        int h = tex.height;

        bool[,] visited = new bool[w, h];
        List<ColorRegion> regions = new();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (visited[x, y]) continue;

                Color target = tex.GetPixel(x, y);
                ColorRegion region = new()
                {
                    color = target
                };

                Queue<Vector2Int> q = new();
                q.Enqueue(new Vector2Int(x, y));

                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    int px = p.x;
                    int py = p.y;

                    if (px < 0 || px >= w || py < 0 || py >= h) continue;
                    if (visited[px, py]) continue;

                    Color c = tex.GetPixel(px, py);
                    if (Vector4.Distance(c, target) > tolerance) continue;

                    visited[px, py] = true;
                    region.pixels.Add(p);

                    q.Enqueue(new Vector2Int(px + 1, py));
                    q.Enqueue(new Vector2Int(px - 1, py));
                    q.Enqueue(new Vector2Int(px, py + 1));
                    q.Enqueue(new Vector2Int(px, py - 1));
                }

                if (region.pixels.Count > 0)
                    regions.Add(region);
            }
        }

        return regions;
    }

    // ---------------------------------------------------------
    // 2. COMPUTE CENTER UV FOR EACH REGION
    // ---------------------------------------------------------
    private static void ComputeRegionCenters(List<ColorRegion> regions, int width, int height)
    {
        foreach (var r in regions)
        {
            Vector2 sum = Vector2.zero;
            foreach (var p in r.pixels)
                sum += new Vector2(p.x, p.y);

            Vector2 centerPixel = sum / r.pixels.Count;
            r.centerUV = new Vector2(centerPixel.x / width, centerPixel.y / height);
        }
    }

    // ---------------------------------------------------------
    // 3. GET LOCAL POSITION + NORMAL FOR EACH REGION
    // ---------------------------------------------------------
    private static void ComputeMeshDataForRegions(List<ColorRegion> regions, Mesh mesh)
    {
        foreach (var r in regions)
        {
            r.localPos = GetLocalPositionFromUV(mesh, r.centerUV);
            r.localNormal = GetLocalNormalFromUV(mesh, r.centerUV);
        }
    }

    // ---------------------------------------------------------
    // UV → LOCAL POSITION
    // ---------------------------------------------------------
    private static Vector3 GetLocalPositionFromUV(Mesh mesh, Vector2 uv)
    {
        var uvs = mesh.uv;
        var verts = mesh.vertices;
        var tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];

            Vector2 uv0 = uvs[i0];
            Vector2 uv1 = uvs[i1];
            Vector2 uv2 = uvs[i2];

            if (PointInTriangleUV(uv, uv0, uv1, uv2))
            {
                Vector3 bary = Barycentric(uv, uv0, uv1, uv2);

                return verts[i0] * bary.x +
                       verts[i1] * bary.y +
                       verts[i2] * bary.z;
            }
        }

        return Vector3.zero;
    }

    // ---------------------------------------------------------
    // UV → LOCAL NORMAL
    // ---------------------------------------------------------
    private static Vector3 GetLocalNormalFromUV(Mesh mesh, Vector2 uv)
    {
        var uvs = mesh.uv;
        var normals = mesh.normals;
        var tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];

            Vector2 uv0 = uvs[i0];
            Vector2 uv1 = uvs[i1];
            Vector2 uv2 = uvs[i2];

            if (PointInTriangleUV(uv, uv0, uv1, uv2))
            {
                Vector3 bary = Barycentric(uv, uv0, uv1, uv2);

                return (normals[i0] * bary.x +
                        normals[i1] * bary.y +
                        normals[i2] * bary.z).normalized;
            }
        }

        return Vector3.zero;
    }

    // ---------------------------------------------------------
    // BARYCENTRIC COORDINATES
    // ---------------------------------------------------------
    private static Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = p - a;

        float d00 = Vector2.Dot(v0, v0);
        float d11 = Vector2.Dot(v1, v1);
        float d01 = Vector2.Dot(v0, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1f - v - w;

        return new Vector3(u, v, w);
    }

    // ---------------------------------------------------------
    // POINT IN TRIANGLE (UV SPACE)
    // ---------------------------------------------------------
    private static bool PointInTriangleUV(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector3 bary = Barycentric(p, a, b, c);
        return bary.x >= 0f && bary.y >= 0f && bary.z >= 0f &&
               bary.x <= 1f && bary.y <= 1f && bary.z <= 1f;
    }
}
