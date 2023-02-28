using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(RemoteImageReceiver))]
[CanEditMultipleObjects]

public class RemoteImageReceiverUI : Editor
{
    SerializedProperty remoteImageViewerPrefab;
    SerializedProperty testImage;
    // Start is called before the first frame update
    void OnEnable()
    {
        remoteImageViewerPrefab = serializedObject.FindProperty("remoteImageViewerPerfab");
        testImage = serializedObject.FindProperty("testImage");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(remoteImageViewerPrefab);
        EditorGUILayout.PropertyField(testImage);
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("Load Image"))
        {
            RemoteImageReceiver remoteImageReceiver = (RemoteImageReceiver)target;
            remoteImageReceiver.AddRemoteImage(remoteImageReceiver.testImage);
        }
    }
}
#endif
