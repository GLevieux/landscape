using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BuildNavmesh : MonoBehaviour
{
    public NavMeshSurface m_navSurface;
    public void LevelGenerated()
    {   
        m_navSurface.BuildNavMesh();
    }

    /*public void Update()
    {
        if(Input.GetKeyDown(KeyCode.N))
            m_navSurface.BuildNavMesh();
    }*/
}
    