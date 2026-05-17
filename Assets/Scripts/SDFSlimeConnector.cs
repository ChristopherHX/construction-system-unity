using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[ExecuteAlways]
public class SDFSlimeConnector : MonoBehaviour
{
    public UnityEngine.Object m_SDFTexture;
    public Transform m_Box;
    [Range(-1, 1)]
    public float m_Margin = 0.0f;
    public float m_Margin2 = 0.1f;
    public float m_BlendDistance = 0.08f;
    MeshRenderer m_MeshRenderer;
    MaterialPropertyBlock m_Props;
    bool m_LoggedInvalidSDFSource;

    [Header("Shading")]
    public Light m_Light;
    [ColorUsage(showAlpha: false)]
    public Color m_Ambient = Color.black;
    [ColorUsage(showAlpha: false)]
    public Color m_Albedo = Color.white;
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color m_Sky = Color.white;

    [Header("Sky scatter")]
    public float m_ScatterAmount = 1.0f;
    public float m_ScatterStart = 0.05f;
    public int m_ScatterIterations = 100;
    public float m_ScatterMaxDepth = 1.0f;

    [Header("Directional scatter")]
    public float m_DirScatterAmount = 1.0f;
    public float m_ExtinctionCoeff = 1.0f;
    [Range(0, 1)]
    public float m_Anisotropy = 0.5f;
    public int m_DirScatterIterations = 20;
    public int m_DirScatterIterationsSecondary = 10;

    ComputeBuffer m_SpheresCB;
    ComputeBuffer m_FallbackSpheresCB;
    Vector4[] m_SpheresData;
    static readonly Vector4[] kFallbackSphereData = { Vector4.zero };

    static class Uniforms
    {
        internal static int _Color = Shader.PropertyToID("_Color");
        internal static int _BoxSize = Shader.PropertyToID("_BoxSize");
        internal static int _BoxPos = Shader.PropertyToID("_BoxPos");
        internal static int _WorldToSDFSpace = Shader.PropertyToID("_WorldToSDFSpace");
        internal static int _WorldToSDFSpace2 = Shader.PropertyToID("_WorldToSDFSpace2");
        internal static int _SDF = Shader.PropertyToID("_SDF");
        internal static int _Margin = Shader.PropertyToID("_Margin");
        internal static int _Margin2 = Shader.PropertyToID("_Margin2");
        internal static int _ScatterParams = Shader.PropertyToID("_ScatterParams");
        internal static int _LightColor = Shader.PropertyToID("_LightColor");
        internal static int _LightDir = Shader.PropertyToID("_LightDir");
        internal static int _Ambient = Shader.PropertyToID("_Ambient");
        internal static int _Albedo = Shader.PropertyToID("_Albedo");
        internal static int _Sky = Shader.PropertyToID("_Sky");
        internal static int _DirScatterAmount = Shader.PropertyToID("_DirScatterAmount");
        internal static int _DirScatterMaxIterations = Shader.PropertyToID("_DirScatterMaxIterations");
        internal static int _DirScatterMaxIterationsSecondary = Shader.PropertyToID("_DirScatterMaxIterationsSecondary");
        internal static int _ExtinctionCoeff = Shader.PropertyToID("_ExtinctionCoeff");
        internal static int _Anisotropy = Shader.PropertyToID("_Anisotropy");
        internal static int _Spheres = Shader.PropertyToID("_Spheres");
        internal static int _BlendDistance = Shader.PropertyToID("_BlendDistance");
        internal static int _Mode = Shader.PropertyToID("_Mode");
    }

    void OnValidate()
    {
        m_ScatterAmount = Mathf.Max(m_ScatterAmount, 0.0f);
        m_ScatterStart = Mathf.Max(m_ScatterStart, 0.0001f);
        m_ScatterIterations = Mathf.Max(m_ScatterIterations, 10);
        m_ScatterMaxDepth = Mathf.Max(m_ScatterMaxDepth, 0.1f);

        m_DirScatterAmount = Mathf.Max(m_DirScatterAmount, 0.0f);
        m_ExtinctionCoeff = Mathf.Max(m_ExtinctionCoeff, 0.0f);
    }

    void Start()
    {
        m_MeshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (m_MeshRenderer == null)
            m_MeshRenderer = GetComponent<MeshRenderer>();

        if (m_MeshRenderer == null || m_Box == null)
            return;

        if (!TryGetSDFSourceData(out Matrix4x4 localToSDFSpace, out Texture sdfTexture, out Bounds voxelBounds))
            return;

        if (m_Props == null)
            m_Props = new MaterialPropertyBlock();

        m_Props.Clear();
        m_Props.SetColor(Uniforms._Color, Color.red);
        m_Props.SetVector(Uniforms._BoxSize, m_Box.transform.localScale * 0.5f);
        m_Props.SetVector(Uniforms._BoxPos, m_Box.transform.position);

        m_Props.SetMatrix(Uniforms._WorldToSDFSpace, localToSDFSpace * Matrix4x4.Translate(new Vector3(0, 0, 1.5f)));
        m_Props.SetMatrix(Uniforms._WorldToSDFSpace2, localToSDFSpace * Matrix4x4.Translate(new Vector3(0, 0, -1.5f)));
        m_Props.SetTexture(Uniforms._SDF, sdfTexture);
        m_Props.SetFloat(Uniforms._Margin, m_Margin * voxelBounds.size.magnitude);
        m_Props.SetFloat(Uniforms._Margin2, m_Margin2 * voxelBounds.size.magnitude);

        m_Props.SetVector(Uniforms._ScatterParams, new Vector4(m_ScatterAmount * 50.0f, m_ScatterStart, m_ScatterMaxDepth/(float)m_ScatterIterations, m_ScatterMaxDepth));
        Color lightColor = Color.white;
        Vector4 lightDirPos = Vector3.down;
        if (m_Light != null)
        {
            lightColor = m_Light.color;
            Transform lightTransform = m_Light.transform;
            lightDirPos = lightTransform.forward;

            // TODO: proper support for non-directional light types
            if (m_Light.type != LightType.Directional)
                lightDirPos = (transform.position - m_Light.transform.position).normalized;
        }

        m_Props.SetVector(Uniforms._LightColor, lightColor);
        
        m_Props.SetVector(Uniforms._LightDir, lightDirPos);
        m_Props.SetColor(Uniforms._Ambient, m_Ambient);
        m_Props.SetColor(Uniforms._Albedo, m_Albedo);
        m_Props.SetColor(Uniforms._Sky, m_Sky);
        m_Props.SetFloat(Uniforms._DirScatterAmount, m_DirScatterAmount * 0.001f);
        m_Props.SetInt(Uniforms._DirScatterMaxIterations, m_DirScatterIterations);
        m_Props.SetInt(Uniforms._DirScatterMaxIterationsSecondary, m_DirScatterIterationsSecondary);
        m_Props.SetFloat(Uniforms._ExtinctionCoeff, m_ExtinctionCoeff);
        m_Props.SetFloat(Uniforms._Anisotropy, Mathf.Min(m_Anisotropy, 0.99f));
        m_Props.SetFloat(Uniforms._BlendDistance, m_BlendDistance);

        m_Props.SetVector("left", new Vector4(0, 0, 0.5f, 0.1f));
        m_Props.SetVector("right", new Vector4(0, 0, -0.5f, 0.1f));

        m_MeshRenderer.SetPropertyBlock(m_Props);
    }

    bool TryGetSDFSourceData(out Matrix4x4 localToSDFSpace, out Texture sdfTexture, out Bounds voxelBounds)
    {
        localToSDFSpace = Matrix4x4.identity;
        sdfTexture = null;
        voxelBounds = new Bounds(Vector3.zero, Vector3.zero);

        if (m_SDFTexture == null)
            return false;

        if (!TryReadMember(m_SDFTexture, "mode", out object modeValue))
            return false;

        if (IsModeNone(modeValue))
            return false;

        bool hasLocalToSDF = TryReadMember(m_SDFTexture, "localToSDFTexCoords", out localToSDFSpace);
        bool hasTexture = TryReadMember(m_SDFTexture, "sdf", out sdfTexture);
        bool hasBounds = TryReadMember(m_SDFTexture, "voxelBounds", out voxelBounds);

        if (hasLocalToSDF && hasTexture && hasBounds && sdfTexture != null)
        {
            m_LoggedInvalidSDFSource = false;
            return true;
        }

        if (!m_LoggedInvalidSDFSource)
        {
            Debug.LogWarning($"SDF source '{m_SDFTexture.name}' does not expose the expected members (mode, worldToSDFTexCoords, sdf, voxelBounds).", this);
            m_LoggedInvalidSDFSource = true;
        }

        return false;
    }

    static bool TryReadMember<T>(object target, string name, out T value)
    {
        value = default;
        if (target == null)
            return false;

        Type type = target.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null && property.GetIndexParameters().Length == 0)
        {
            object propertyValue = property.GetValue(target);
            if (propertyValue is T tValue)
            {
                value = tValue;
                return true;
            }
        }

        FieldInfo field = type.GetField(name, flags);
        if (field != null)
        {
            object fieldValue = field.GetValue(target);
            if (fieldValue is T tValue)
            {
                value = tValue;
                return true;
            }
        }

        return false;
    }

    static bool IsModeNone(object modeValue)
    {
        if (modeValue == null)
            return true;

        if (modeValue is Enum enumValue)
            return Convert.ToInt32(enumValue) == 0;

        if (modeValue is int intValue)
            return intValue == 0;

        if (modeValue is string stringValue)
            return string.Equals(stringValue, "None", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    void OnDestroy()
    {
        ReleaseComputeBuffer(m_SpheresCB);
        ReleaseComputeBuffer(m_FallbackSpheresCB);
    }

    static void CreateComputeBuffer(ref ComputeBuffer cb, int length, int stride)
    {
        if (cb != null && cb.count == length && cb.stride == stride)
            return;

        ReleaseComputeBuffer(cb);
        cb = new ComputeBuffer(length, stride);
    }

    static void ReleaseComputeBuffer(ComputeBuffer cb)
    {
        if (cb != null)
            cb.Release();
    }

#if UNITY_EDITOR
    void OnEnable() => UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload; 
    void OnDisable() => UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
    void OnBeforeAssemblyReload() => OnDestroy();
#endif
}
