using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagGenerateLevel : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            WFCManager manager = GetComponentInParent<WFCManager>();
            manager.TryGenerateNewLevel();      
        }
    }
}
