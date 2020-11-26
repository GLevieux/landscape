using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleNavigability : MonoBehaviour
{
    public float NavHeightZPos = -1;
    public float NavHeightZNeg = -1;
    public float NavHeightXPos = -1;
    public float NavHeightXNeg = -1;

    public void OnDrawGizmosSelected()
    {
        float unitSize = GetComponent<PrefabInstance>().gridUnitSize;
        Vector2Int size = GetComponent<PrefabInstance>().size;

        Vector3 coinXZZero = transform.position - new Vector3(unitSize / 2.0f, 0, unitSize / 2.0f);
        Vector3 tailleModule = new Vector3(unitSize * size.x, 0, unitSize * size.y);
        Vector3 tailleModuleDemi = new Vector3(unitSize * size.x / 2.0f, 0, unitSize * size.y / 2.0f);


        Gizmos.color = NavHeightXPos < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + new Vector3(tailleModule.x, NavHeightXPos, tailleModuleDemi.z),0.2f);
        Gizmos.color = NavHeightXNeg < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + new Vector3(0, NavHeightXNeg, tailleModuleDemi.z), 0.2f);
        Gizmos.color = NavHeightZPos < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + new Vector3(tailleModuleDemi.x, NavHeightZPos, tailleModule.z), 0.2f);
        Gizmos.color = NavHeightZNeg < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + new Vector3(tailleModuleDemi.x, NavHeightZNeg, 0), 0.2f);
    }
}
