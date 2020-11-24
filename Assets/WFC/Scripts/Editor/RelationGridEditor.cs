using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(RelationGrid))]
public class RelationGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RelationGrid myScript = (RelationGrid)target;
        if (GUILayout.Button("Scan and fill with air"))
        {
            myScript.ScanAndFillAirEditor();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }
    }
}

