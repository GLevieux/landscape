using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(RelationGrid))]
public class RelationGridEditor : Editor
{
    public void OnSceneGUI()
    {
        RelationGrid myScript = (RelationGrid)target;

        if (myScript.agent != null)
        {
            myScript.agent.debugGui(myScript.transform.position);
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RelationGrid myScript = (RelationGrid)target;

        if (GUILayout.Button("Toggle Prefab Instance Gizmos"))
        {
            myScript.TogglePrefabInstanceGizmos();
        }

        if (GUILayout.Button("Scan and fill with air"))
        {
            myScript.ScanAndFillAirEditor();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }        

        if (GUILayout.Button("Build and show nav grid"))
        {
            myScript.BuildAndShowNavEditor();
        }

        if (GUILayout.Button("Show agent"))
        {
            myScript.StartAgentEditor();
        }

        if(myScript.agent != null)
        {
            if (GUILayout.Button("Step agent"))
            {
                myScript.StepAgentEditor();
            }

            if (GUILayout.Button("Update agent perception"))
            {
                myScript.UpdateAgentEditor();
            }

            if (GUILayout.Button("Kill agent"))
            {
                myScript.KillAgentEditor();
            }

            if (GUILayout.Button("Evaluate Level"))
            {
                myScript.EvaluateLevelEditor();
            }
        }
    }
}

