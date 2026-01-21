using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MyButton))]
public class MyButtonEditor : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw button
        if (GUI.Button(position, label))
        {
            // Get the actual object instance
            var targetObject = property.serializedObject.targetObject;
            var instance = fieldInfo.GetValue(targetObject) as MyButton;

            instance?.OnButtonClicked?.Invoke();
        }

        EditorGUI.EndProperty();
    }


}
