using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static SimpleGridWFC;

public class ZoneGrid : MonoBehaviour
{
    public Transform gridAnchor;
    public Material buildMaterial;

    public List<SimpleGridWFC.Zone> listZones = new List<SimpleGridWFC.Zone>();

    //private int gridUnitSize = 1;

    //public void Start()
    //{
    //    if(!gridAnchor && this.transform.GetComponentInParent<WFCManager>())
    //    {
    //        gridAnchor = this.transform.parent.transform;
    //    }
    //}

    public void setZones(List<Zone> list)
    {
        listZones = list;
#if !UNITY_EDITOR
        updateZone();
#endif
    }

    public List<Zone> getZones()
    {
        return listZones;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!gridAnchor)
            return;

        for (int i = 0; i < listZones.Count; i++)
        {
            Zone current = listZones[i];

            Vector3 position = new Vector3(gridAnchor.position.x + current.origin.x + 0.5f * current.size.x, 0,
                                            gridAnchor.position.z + current.origin.y + 0.5f * current.size.y);

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(position, new Vector3(current.size.x, 1, current.size.y));

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;

            Handles.color = new Color(0, 0, 1, 1);
            Handles.Label( position, "id:" + current.assetID + " p:" + current.probabilityBoost, style);
        }
    }

#else

    List<GameObject> existingZones = new List<GameObject>();

    private void updateZone()
    {
        foreach (GameObject g in existingZones)
        {
            GameObject.Destroy(g);
        }

        for (int i = 0; i < listZones.Count; i++)
        {
            Zone current = listZones[i];

            Vector3 position = new Vector3(gridAnchor.position.x + current.origin.x + 0.5f * current.size.x, 0,
                                            gridAnchor.position.z + current.origin.y + 0.5f * current.size.y);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(current.size.x, 1, current.size.y);
            if (buildMaterial != null)
                cube.GetComponent<Renderer>().material = buildMaterial;
            else
                cube.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 1);
            cube.transform.position = position;

            GameObject text = new GameObject();
            //text.transform.SetParent(cube.transform, false);
            text.transform.position = position;
            TextMesh t = text.AddComponent<TextMesh>();
            t.text = "id:" + current.assetID + " p:" + current.probabilityBoost;
            t.characterSize = 0.08f;
            t.fontSize = 30;

            if(current.size.x == 00 || current.size.y == 0)
            {
                t.color = new Color(0, 0, 0);
            }

            t.transform.localEulerAngles += new Vector3(90, 0, 0);
            //t.transform.localPosition += new Vector3(-0.45f, 0.6f, 0.1f);

            existingZones.Add(cube);
            existingZones.Add(text);
        }
    }
#endif
}
