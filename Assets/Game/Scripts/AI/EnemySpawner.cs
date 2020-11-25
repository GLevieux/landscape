using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Transform EnemyPrefab;
    public void Start()
    {
        Instantiate<Transform>(EnemyPrefab, transform.position + Vector3.up, transform.rotation);
    }

}
