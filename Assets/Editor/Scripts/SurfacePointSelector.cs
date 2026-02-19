// Prompt: unity create tool that can select and visualize points on a selected object surface

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Unity Editor tool to select points on a GameObject's surface
/// and visualize them in the Scene view.
/// </summary>
public class SurfacePointSelector : EditorWindow
{
    private GameObject targetObject;
    private List<Vector3> points = new List<Vector3>();
    private bool isPicking = false;

    [MenuItem("Tools/Surface Point Selector")]
    public static void ShowWindow()
    {
        GetWindow<SurfacePointSelector>("Surface Point Selector");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Select Target Object", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target", targetObject, typeof(GameObject), true);

        EditorGUILayout.Space();

        if (GUILayout.Button(isPicking ? "Stop Picking" : "Start Picking"))
        {
            isPicking = !isPicking;
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Clear Points"))
        {
            points.Clear();
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Points Count: {points.Count}");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (isPicking && targetObject != null)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.gameObject == targetObject)
                    {
                        points.Add(hit.point);
                        e.Use();
                        Repaint();
                    }
                }
            }
        }

        // Draw points as small spheres
        Handles.color = Color.red;
        foreach (var p in points)
        {
            Handles.SphereHandleCap(0, p, Quaternion.identity, HandleUtility.GetHandleSize(p) * 0.05f, EventType.Repaint);
        }
    }
}
