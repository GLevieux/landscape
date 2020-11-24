using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class RandomSky : MonoBehaviour
{
    public Volume volume;
    public Transform mainDirectionalLight;
    private Quaternion nextSunRotation = Quaternion.identity;
    private float timeChange;

    private float GroundHue = 1;
    private float SkyHue = 1;
    private float HorizHue = 1;
    private float ZenithHue = 1;

    private float NextGroundHue = 1;
    private float NextSkyHue = 1;
    private float NextHorizHue = 1;
    private float NextZenithHue = 1;
    
    public void Blackout(bool blackout)
    {
        Exposure exp;
        if (volume.profile.TryGet<Exposure>(out exp))
        {
            exp.compensation.overrideState = blackout;
            exp.compensation.value = -20;
        }
    }

    public void Randomize()
    {
        StartCoroutine("changeSky");
    }

    IEnumerator changeSky()
    {
        yield return new WaitForSeconds(0.5f);
        PhysicallyBasedSky sky;
        if (volume.profile.TryGet<PhysicallyBasedSky>(out sky))
        {
            NextGroundHue = Random.Range(0.0f, 1.0f);
            NextSkyHue = GroundHue + 0.3f;
            if (NextSkyHue > 1.0f)
                NextSkyHue -= 1.0f;
            NextHorizHue = GroundHue + 0.5f;
            if (NextHorizHue > 1.0f)
                NextHorizHue -= 1.0f;
            NextZenithHue = GroundHue + 0.7f;
            if (NextZenithHue > 1.0f)
                NextZenithHue -= 1.0f;

            nextSunRotation = Quaternion.AngleAxis(Random.value * 180, Vector3.right);

            float lerpdelta = 1.0f;
            mainDirectionalLight.rotation = nextSunRotation;
            sky.groundTint.value = Color.HSVToRGB(Mathf.Lerp(GroundHue, NextGroundHue, lerpdelta), 0.9f, 0.9f);
            sky.groundTint.overrideState = true;
            sky.horizonTint.value = Color.HSVToRGB(Mathf.Lerp(HorizHue, NextHorizHue, lerpdelta), 0.9f, 0.9f);
            sky.horizonTint.overrideState = true;
            sky.zenithTint.value = Color.HSVToRGB(Mathf.Lerp(ZenithHue, NextZenithHue, lerpdelta), 0.9f, 0.9f);
            sky.zenithTint.overrideState = true;

        }

        yield return new WaitForSeconds(0.2f);
        Blackout(false);
    }

    
    public void LevelGenerated()
    {
        Randomize();
    }

    public void PreStartGeneration()
    {
        Blackout(true);
    }

}
