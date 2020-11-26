using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPlayerOnStart : MonoBehaviour
{
    public void Start()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        player.position = transform.position;
        player.rotation = transform.rotation;
    }


}
