using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(WFCManager))]
public class WFCManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WFCManager manager = (WFCManager)target;
        if (GUILayout.Button("Scan grids"))
        {
            manager.ScanGridsEditor();
        }
    }
}

