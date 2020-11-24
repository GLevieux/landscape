using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateEffectManager : MonoBehaviour
{
    public Material generationMaterial;
    // Start is called before the first frame update
    public void LevelGenerated()
    {
        addTransformToChildren(transform);
    }

    public void addTransformToChildren(Transform tr)
    {
        foreach (Transform t in tr)
        {
            if (t.GetComponent<Renderer>() != null)
            {
                t.gameObject.AddComponent<GenerateEffect>();
                t.GetComponent<GenerateEffect>().generationMaterial = generationMaterial;
                t.GetComponent<GenerateEffect>().init();
            }
            addTransformToChildren(t);
        }
    }
}
