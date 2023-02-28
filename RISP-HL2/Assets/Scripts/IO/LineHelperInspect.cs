using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(LineHelper))]
[CanEditMultipleObjects]

public class LineHelperInspect : Editor
{
    SerializedProperty linePrefab;
    SerializedProperty lineWidth;
    // Start is called before the first frame update
    void OnEnable()
    {
        linePrefab = serializedObject.FindProperty("linePrefab");
        lineWidth = serializedObject.FindProperty("lineWidth");
    }

    void AddTestLine()
    {
        LineHelper lineHelper = (LineHelper)target;
        Vector3[] testVector = new Vector3[2];
        testVector[0] = new Vector3(0, 0, 0);
        testVector[1] = new Vector3(1, 0, 0);
        lineHelper.AddNewLine(testVector, Color.red);
    }
    void ClearTestLines()
    {
        LineHelper lineHelper = (LineHelper)target;
        lineHelper.ClearLines();
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(linePrefab);
        EditorGUILayout.PropertyField(lineWidth);
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("Add test line"))
        {
            AddTestLine();
        }
        if (GUILayout.Button("Clear lines"))
        {
            ClearTestLines();
        }
    }
}
#endif