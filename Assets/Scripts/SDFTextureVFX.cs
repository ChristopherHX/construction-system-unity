using UnityEngine;
using UnityEngine.VFX.SDF;
using System.Collections.Generic;

[ExecuteAlways]
public class SDFTextureVFX : MonoBehaviour
{
    public enum Mode
    {
        None = 0,
        Dynamic = 2
    }

    public enum UpdateMode
    {
        OnEnable,
        EveryFrame,
        Manual
    }

    [Header("Source")]
    [SerializeField] Transform m_SourceRoot;
    [SerializeField] MeshFilter m_MeshFilter;
    [SerializeField] SkinnedMeshRenderer m_SkinnedMeshRenderer;

    [Header("Bake Volume (Local Space)")]
    [SerializeField] Vector3 m_Size = Vector3.one;
    [SerializeField] Vector3 m_Center = Vector3.zero;
    [SerializeField, Min(4)] int m_Resolution = 64;

    [Header("Bake Settings")]
    [SerializeField, Range(1, 20)] int m_SignPassesCount = 1;
    [SerializeField, Range(0.0f, 1.0f)] float m_Threshold = 0.5f;
    [SerializeField] float m_SdfOffset = 0.0f;
    [SerializeField] UpdateMode m_UpdateMode = UpdateMode.OnEnable;

    MeshToSDFBaker m_Baker;
    RenderTexture m_SdfTexture;
    Vector3 m_ActualSize = Vector3.one;
    Vector3Int m_GridSize = Vector3Int.one;
    bool m_LoggedMissingSource;
    readonly Dictionary<int, Mesh> m_BakedSkinnedMeshes = new Dictionary<int, Mesh>();
    readonly List<Mesh> m_BakeMeshes = new List<Mesh>(1);
    readonly List<Matrix4x4> m_BakeTransforms = new List<Matrix4x4>(1);

    public Texture sdf => m_SdfTexture;
    public Mode mode => m_SdfTexture != null ? Mode.Dynamic : Mode.None;
    public Vector3Int voxelResolution => m_GridSize;
    public Bounds voxelBounds => new Bounds(m_Center, m_ActualSize);

    public Matrix4x4 worldToSDFTexCoords
    {
        get
        {
            Vector3 size = voxelBounds.size;
            if (size.x <= 0.0f || size.y <= 0.0f || size.z <= 0.0f)
                return Matrix4x4.identity;

            Matrix4x4 sdfLocalToTex =
                Matrix4x4.Translate(Vector3.one * 0.5f) *
                Matrix4x4.Scale(new Vector3(1.0f / size.x, 1.0f / size.y, 1.0f / size.z)) *
                Matrix4x4.Translate(-voxelBounds.center);

            return sdfLocalToTex * transform.worldToLocalMatrix;
        }
    }

    void Reset()
    {
        m_MeshFilter = GetComponent<MeshFilter>();
        m_SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
    }

    void OnEnable()
    {
        if (m_UpdateMode == UpdateMode.OnEnable)
            BakeSDF();
    }

    void Update()
    {
        if (m_UpdateMode == UpdateMode.EveryFrame)
            BakeSDF();
    }

    public void BakeSDF()
    {
        if (!TryBuildBakeInputs())
        {
            if (!m_LoggedMissingSource)
            {
                Debug.LogWarning($"No MeshFilter or SkinnedMeshRenderer found for '{name}'.", this);
                m_LoggedMissingSource = true;
            }
            return;
        }

        m_LoggedMissingSource = false;

        Vector3 size = new Vector3(
            Mathf.Max(0.001f, m_Size.x),
            Mathf.Max(0.001f, m_Size.y),
            Mathf.Max(0.001f, m_Size.z));
        int resolution = Mathf.Max(4, m_Resolution);

        if (m_Baker == null)
        {
            m_Baker = new MeshToSDFBaker(size, m_Center, resolution, m_BakeMeshes, m_BakeTransforms, m_SignPassesCount, m_Threshold, m_SdfOffset);
        }
        else
        {
            m_Baker.Reinit(size, m_Center, resolution, m_BakeMeshes, m_BakeTransforms, m_SignPassesCount, m_Threshold, m_SdfOffset);
        }

        m_Baker.BakeSDF();
        m_SdfTexture = m_Baker.SdfTexture;
        m_ActualSize = m_Baker.GetActualBoxSize();
        m_GridSize = m_Baker.GetGridSize();
    }

    bool TryBuildBakeInputs()
    {
        m_BakeMeshes.Clear();
        m_BakeTransforms.Clear();

        Transform root = ResolveSourceRoot();
        if (root != null && TryAddSourcesFromRoot(root))
            return true;

        return TryAddSingleSourceFallback();
    }

    Transform ResolveSourceRoot()
    {
        if (m_SourceRoot != null)
            return m_SourceRoot;

        if (m_SkinnedMeshRenderer != null)
        {
            Animator animator = m_SkinnedMeshRenderer.GetComponentInParent<Animator>();
            return animator != null ? animator.transform : m_SkinnedMeshRenderer.transform.root;
        }

        if (m_MeshFilter != null)
            return m_MeshFilter.transform.root;

        Animator closestAnimator = FindClosestAnimator();
        if (closestAnimator != null)
            return closestAnimator.transform;

        return null;
    }

    bool TryAddSourcesFromRoot(Transform root)
    {
        Matrix4x4 worldToSDFLocal = transform.worldToLocalMatrix;
        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = skinnedRenderers[i];
            if (renderer == null || renderer.sharedMesh == null)
                continue;

            Mesh bakedMesh = GetOrCreateBakedMesh(renderer);
            renderer.BakeMesh(bakedMesh);
            m_BakeMeshes.Add(bakedMesh);
            m_BakeTransforms.Add(worldToSDFLocal * renderer.transform.localToWorldMatrix);
        }

        if (m_BakeMeshes.Count > 0)
            return true;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter filter = meshFilters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;

            m_BakeMeshes.Add(filter.sharedMesh);
            m_BakeTransforms.Add(worldToSDFLocal * filter.transform.localToWorldMatrix);
        }

        return m_BakeMeshes.Count > 0;
    }

    bool TryAddSingleSourceFallback()
    {
        Matrix4x4 worldToSDFLocal = transform.worldToLocalMatrix;

        if (m_SkinnedMeshRenderer == null)
        {
            m_SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (m_SkinnedMeshRenderer == null)
                m_SkinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (m_SkinnedMeshRenderer == null)
                m_SkinnedMeshRenderer = FindClosestSkinnedMeshRenderer();
        }

        if (m_SkinnedMeshRenderer != null && m_SkinnedMeshRenderer.sharedMesh != null)
        {
            Mesh bakedMesh = GetOrCreateBakedMesh(m_SkinnedMeshRenderer);
            m_SkinnedMeshRenderer.BakeMesh(bakedMesh);
            m_BakeMeshes.Add(bakedMesh);
            m_BakeTransforms.Add(worldToSDFLocal * m_SkinnedMeshRenderer.transform.localToWorldMatrix);
            return true;
        }

        if (m_MeshFilter == null)
        {
            m_MeshFilter = GetComponent<MeshFilter>();
            if (m_MeshFilter == null)
                m_MeshFilter = GetComponentInChildren<MeshFilter>();
            if (m_MeshFilter == null)
                m_MeshFilter = FindClosestMeshFilter();
        }

        if (m_MeshFilter != null)
        {
            if (m_MeshFilter.sharedMesh != null)
            {
                m_BakeMeshes.Add(m_MeshFilter.sharedMesh);
                m_BakeTransforms.Add(worldToSDFLocal * m_MeshFilter.transform.localToWorldMatrix);
                return true;
            }
        }

        return false;
    }

    Mesh GetOrCreateBakedMesh(SkinnedMeshRenderer renderer)
    {
        int id = renderer.GetInstanceID();
        if (!m_BakedSkinnedMeshes.TryGetValue(id, out Mesh bakedMesh) || bakedMesh == null)
        {
            bakedMesh = new Mesh
            {
                name = $"{renderer.name}_BakedSDFMesh"
            };
            m_BakedSkinnedMeshes[id] = bakedMesh;
        }

        return bakedMesh;
    }

    Animator FindClosestAnimator()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Animator best = null;
        float bestSqDist = float.MaxValue;
        Vector3 p = transform.position;

        for (int i = 0; i < animators.Length; i++)
        {
            Animator a = animators[i];
            if (a == null)
                continue;

            float sqDist = (a.transform.position - p).sqrMagnitude;
            if (sqDist < bestSqDist)
            {
                bestSqDist = sqDist;
                best = a;
            }
        }

        return best;
    }

    SkinnedMeshRenderer FindClosestSkinnedMeshRenderer()
    {
        SkinnedMeshRenderer[] renderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        SkinnedMeshRenderer best = null;
        float bestSqDist = float.MaxValue;
        Vector3 p = transform.position;

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer r = renderers[i];
            if (r == null)
                continue;

            float sqDist = (r.transform.position - p).sqrMagnitude;
            if (sqDist < bestSqDist)
            {
                bestSqDist = sqDist;
                best = r;
            }
        }

        return best;
    }

    MeshFilter FindClosestMeshFilter()
    {
        MeshFilter[] filters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        MeshFilter best = null;
        float bestSqDist = float.MaxValue;
        Vector3 p = transform.position;

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter f = filters[i];
            if (f == null || f.sharedMesh == null)
                continue;

            float sqDist = (f.transform.position - p).sqrMagnitude;
            if (sqDist < bestSqDist)
            {
                bestSqDist = sqDist;
                best = f;
            }
        }

        return best;
    }

    void OnDisable()
    {
        DisposeBaker();
    }

    void OnDestroy()
    {
        DisposeBaker();
        foreach (var mesh in m_BakedSkinnedMeshes.Values)
        {
            if (mesh == null)
                continue;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(mesh);
            else
#endif
                Destroy(mesh);
        }
        m_BakedSkinnedMeshes.Clear();
    }

    void DisposeBaker()
    {
        if (m_Baker != null)
        {
            m_Baker.Dispose();
            m_Baker = null;
        }
        m_SdfTexture = null;
    }
}
