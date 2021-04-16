using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentMoinsBete : LndAgent
{
    //Reward et drives
    public float noveltyDrive = 1.0f;
    public float heightUpDrive = 0.8f;
    public float heightDownDrive = -0.2f;
    public float safetyGainDrive = 0.5f;

    public float noveltyReward = 1.0f;
    public float heightUpReward = 0.8f;
    public float heightDownReward = -0.2f;
    public float safetyReward = 0.5f;

    public float reachabilityF;
    public float reachabilityP;
    public float reachabilityN;
    public float reachabilityB;

    public float noveltyF;
    public float noveltyP;
    public float noveltyN;
    public float noveltyB;

    public float heightGainF;
    public float heightGainP;
    public float heightGainN;
    public float heightGainB;

    public float safetyGainF;
    public float safetyGainP;
    public float safetyGainN;
    public float safetyGainB;

    public float desirabilityF;
    public float desirabilityP;
    public float desirabilityN;
    public float desirabilityB;

    public float happinessF;
    public float happinessP;
    public float happinessN;
    public float happinessB;

    public override void InitParams(ScriptableObject param)
    {
        ConfigAgentFlowCurieux config = (ConfigAgentFlowCurieux)param;
        heightDownDrive = config.heightDownDrive;
        heightUpDrive = config.heightUpDrive;
        safetyGainDrive = config.safetyGainDrive;
        noveltyDrive = config.noveltyDrive;
        heightDownReward = config.heightDownReward;
        heightUpReward = config.heightUpReward;
        safetyReward = config.safetyReward;
        noveltyReward = config.noveltyReward;
    }

    //Retourne un delta de fitness
    public override void UpdatePerception()
    {
        nav.Cells[xPos, zPos].lastTimeInside = stepNum;
       
    }

    public override float TakeDecision()
    {
        float fitnessStep = 0;

        

        return fitnessStep;
    }

    public override void debugGizmo(Vector3 origin)
    {
        base.debugGizmo(origin);
    }

    public override void debugGui(Vector3 origin)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(positionF + Vector3.up * 2, "" + Mathf.Round(desirabilityF * 100) / 100);
        UnityEditor.Handles.Label(positionL + Vector3.up * 2, "" + Mathf.Round(desirabilityN * 100) / 100);
        UnityEditor.Handles.Label(positionR + Vector3.up * 2, "" + Mathf.Round(desirabilityP * 100) / 100);
        UnityEditor.Handles.Label(positionB + Vector3.up * 2, "" + Mathf.Round(desirabilityB * 100) / 100);
#endif
    }
}
