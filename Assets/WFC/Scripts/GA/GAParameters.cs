using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GAScript;

public class GAParameters : MonoBehaviour
{
    //Public Parameters
    public bool launchGA = false;
    public GAConfig gaConfig = new GAConfig();

    void Start()
    {
        var res = this.GetComponents<GAScript>();
        if(res.Length == 0 || res.Length > 1)
        {
            Debug.LogError(this + " => Critical failure: Need ONE GAScript!");
        }
    }
}
