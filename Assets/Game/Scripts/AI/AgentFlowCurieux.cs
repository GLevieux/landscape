using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentFlowCurieux : LndAgent
{
    //Reward et drives
    public float noveltyDrive = 1.0f;
    public float heightUpDrive = 0.8f;
    public float heightDownDrive = -0.2f;
    public float safetyGainDrive = 0.5f;
    public float complexityGainDrive = 0.5f;


    public float noveltyReward = 1.0f;
    public float heightUpReward = 0.8f;
    public float heightDownReward = -0.2f;
    public float safetyReward = 0.5f;
    public float complexityReward = 0.5f;

    private float safety = 0;
    private float complexity = 0;

    public override void InitParams(ScriptableObject param)
    {
        ConfigAgentFlowCurieux config = (ConfigAgentFlowCurieux)param;
        heightDownDrive = config.heightDownDrive;
        heightUpDrive = config.heightUpDrive;
        safetyGainDrive = config.safetyGainDrive;
        complexityGainDrive = config.complexityGainDrive;
        noveltyDrive = config.noveltyDrive;
        heightDownReward = config.heightDownReward;
        heightUpReward = config.heightUpReward;
        safetyReward = config.safetyReward;
        noveltyReward = config.noveltyReward;
        safetyReward = config.safetyReward;
        complexityReward = config.complexityReward;
    }

    

    //Retourne un delta de fitness
    public unsafe override void UpdatePerception()
    {
        nav.Cells[xPos, zPos].lastTimeInside = stepNum;
        
        height = nav.Cells[xPos, zPos].Height;
        safety = 1 - (nav.Cells[xPos, zPos].MeanVisibility);
        complexity = nav.Cells[xPos, zPos].MeanVisibleComplexity;

        //On calcule la désiratbilité de toutes les direction
        ref NavGrid.Cell cell = ref nav.Cells[xPos, zPos];

        for (int iDir = 0; iDir < 8; iDir++)
        {
            desirability[iDir] = -float.MaxValue;

            //Si on peut pas y aller c'est réglé
            if (cell.CanGoToNext[iDir] < 0.2f)
                continue;

            bool found = false;
            ref NavGrid.Cell nextCell = ref cell.getVoisin(iDir, out found);
            if (found)
            {
                desirability[iDir] = noveltyDrive * Mathf.Clamp((stepNum - nextCell.lastTimeInside) / 200.0f, -1, 1);
                desirability[iDir] += safetyGainDrive * (1.0f/nextCell.MeanVisibility - safety);
                desirability[iDir] += complexityGainDrive * (nextCell.MeanVisibleComplexity - complexity);
            }
        }
    }

    private float[] desirability = new float[] { 0, 0, 0, 0, 0, 0, 0, 0 };
    public unsafe override float TakeDecision()
    {
        int dirMaxDesirability = -1;
        float maxDesirability = -float.MaxValue;

        for (int iDirOffset = 0; iDirOffset < 8; iDirOffset++)
        {
            int iDir = (direction + iDirOffset) % 8;
            if (desirability[iDir] > maxDesirability)
            {
                dirMaxDesirability = iDir;
                maxDesirability = desirability[iDir];
            }
        }                
                
        int xPosNext = xPos;
        int zPosNext = zPos;

        ref NavGrid.Cell cell = ref nav.Cells[xPos, zPos];
        
        float nextCellLastTimeInside = cell.lastTimeInside;
        float meanVisibility = cell.MeanVisibility;
        float meanVisibleComplexity = cell.MeanVisibleComplexity;

        bool found = true;
        ref NavGrid.Cell nextCellInDir = ref cell.getVoisin(dirMaxDesirability, out found);

        if (dirMaxDesirability >= 0)
        {
            direction = dirMaxDesirability;
            xPosNext = xPos + xMoves[direction];
            zPosNext = zPos + zMoves[direction];

            nextCellLastTimeInside = nextCellInDir.lastTimeInside;
            meanVisibility = nextCellInDir.MeanVisibility;
            meanVisibleComplexity = nextCellInDir.MeanVisibleComplexity;
        }
              

        float fitnessStep = 0;
       
        if (found)
        {
            fitnessStep += noveltyReward * Mathf.Clamp((stepNum - nextCellLastTimeInside) / 200.0f, -1, 1); 
            fitnessStep += safetyReward * (1.0f - meanVisibility);
            fitnessStep += complexityReward * meanVisibleComplexity;

            xPos = xPosNext;
            zPos = zPosNext;

            height = nav.Cells[xPos, zPos].Height;
            safety = 1 - (nav.Cells[xPos, zPos].MeanVisibility);
            complexity = nav.Cells[xPos, zPos].MeanVisibleComplexity;
        }        

        return fitnessStep;
    }

    public override void debugGizmo(Vector3 origin)
    {
        base.debugGizmo(origin);
    }

    public override void debugGui(Vector3 origin)
    {
#if UNITY_EDITOR
        /*UnityEditor.Handles.Label(positionF + Vector3.up * 2, "" + Mathf.Round(desirabilityF * 100) / 100);
        UnityEditor.Handles.Label(positionL + Vector3.up * 2, "" + Mathf.Round(desirabilityN * 100) / 100);
        UnityEditor.Handles.Label(positionR + Vector3.up * 2, "" + Mathf.Round(desirabilityP * 100) / 100);
        UnityEditor.Handles.Label(positionB + Vector3.up * 2, "" + Mathf.Round(desirabilityB * 100) / 100);*/
#endif
    }
}
