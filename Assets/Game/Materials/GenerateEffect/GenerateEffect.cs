using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateEffect : MonoBehaviour
{
    public Material generationMaterial;
    private Material[] baseMaterials;
    private float endTime = 0;
    public void init()
    {
        baseMaterials = GetComponent<Renderer>().materials;
        Material [] newMaterials = new Material[baseMaterials.Length];

        endTime = Time.time + 2.0f;

        for (int i=0;i< newMaterials.Length; i++)
        {
            bool emissive = false;

            if (baseMaterials[i].HasProperty("_EmissiveColor"))
            {
                Color c = baseMaterials[i].GetColor("_EmissiveColor");
                if(c.maxColorComponent > 0)
                {
                    emissive = true;
                    newMaterials[i] = new Material(generationMaterial);
                    newMaterials[i].SetColor("colorEmit", c);
                }
                
               
            }                
            
            if(!emissive)
            {
                newMaterials[i] = new Material(generationMaterial);
                newMaterials[i].SetColor("colorEmit", Color.black);
            }

            
            newMaterials[i].SetFloat("startTime", Time.time);
            newMaterials[i].SetFloat("endTime", endTime);
        }

        GetComponent<Renderer>().materials = newMaterials;

    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time > endTime + 0.800f)
        {
            GetComponent<Renderer>().materials = baseMaterials;
            this.enabled = false;
        }
    }
}
