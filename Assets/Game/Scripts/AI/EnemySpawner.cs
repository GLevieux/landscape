using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Transform EnemyPrefab;
    private Transform m_enemy;
    public void Start()
    {
        m_enemy = Instantiate<Transform>(EnemyPrefab, transform.position + Vector3.up, transform.rotation);
    }

    public void OnDestroy()
    {
        if(m_enemy != null)
            Destroy(m_enemy.gameObject);
    }
}
