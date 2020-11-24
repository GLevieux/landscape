using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static RelationGrid;

public class InitialGrid : MonoBehaviour
{
    //public GameObject Start;
    //public GameObject End;

    //public float probability;
    //public string prefabSelect;
    //public int prefabID;

    //    public class

    //    public List<SimpleGridWFC.Zone> listZones = new List<SimpleGridWFC.Zone>();
    //    public Transform gridAnchor;

    //    public Material buildMaterial;

    //    private bool updateZone = true;


    //    public List<Zone> getZones()
    //    {
    //        return listZones;
    //    }

    [Serializable]
    public class InitialAsset
    {
        public Vector2Int position = Vector2Int.zero;
        public int id = -1;
        public bool forceRotation = false;
        public int rot = 0;

        override
        public string ToString()
        {
            return "Asset at " + position.x + "/" + position.y + ", id: " + id + ", rot: " + ((forceRotation) ? rot + "" : "whatever");
        }
    }

    public Transform gridAnchor;

    public List<InitialAsset> initialPrefabs = new List<InitialAsset>();

    //public void scanGrid(Dictionary<string, UniqueTile> hashPrefabToUniqueTile)
    //{

    //}

    public int gridUnitSize = 1;

    public List<InitialAsset> getInitialAssets()
    {
        return initialPrefabs;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!gridAnchor)
            return;

        for (int i = 0; i < initialPrefabs.Count; i++)
        {
            InitialAsset current = initialPrefabs[i];

            Vector3 position = new Vector3(gridAnchor.position.x + current.position.x + 0.5f * gridUnitSize, 0,
                                            gridAnchor.position.z + current.position.y + 0.5f * gridUnitSize);

            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawCube(position, new Vector3(gridUnitSize, 1, gridUnitSize));

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;

            Handles.color = new Color(0, 0, 1, 1);
            Handles.Label(position, "id:" + current.id + " r:" + ((current.forceRotation) ? current.rot + "" : "-"), style);
        }
    }
#endif
}
