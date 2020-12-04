using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPlayerOnStart : MonoBehaviour
{
    private static bool teleportAlreadyDone = false; //Au cas ou on en ait plusieurs teleports
    bool DoItOnlyOnce = false;
    public void Start()
    {
        if (teleportAlreadyDone && DoItOnlyOnce)
        {
            Debug.Log("Already done");
            return;
        }
            

        teleportAlreadyDone = true;
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        player.position = transform.position;
        player.rotation = transform.rotation;
    }


}
