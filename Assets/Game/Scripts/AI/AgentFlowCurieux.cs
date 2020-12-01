using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentFlowCurieux
{
    public int xPos;
    public int zPos;
    public float height;
    public int direction;
    public float accumNoTurn = 0;
    public int stepNum = 0;

    NavGrid nav;

    int[] xMoves = { 0, 1, 0, -1 }; //Deplacement sur X en fonction de la rotation
    int[] zMoves = { 1, 0, -1, 0 }; //Deplacement sur X en fonction de la rotation

    public void Init(int xStart, int zStart, int heightStart,  int directionStart, NavGrid grid)
    {
        direction = directionStart;
        xPos = xStart;
        zPos = zStart;
        height = heightStart;
        nav = grid;
    }
    

    //Retourne un delta de fitness
    public float Step()
    {
        float fitnessStep = 0;
        
        stepNum++;
        nav.Cells[xPos, zPos].lastTimeInside = stepNum;
        height = nav.Cells[xPos, zPos].height;

        float HeightOutF = 0;
        float HeightCenterF = 0;
        float HeightDiffF = 0;
        float CanReachF = 0;
        int lastTimeInsideF = 0;

        float HeightOutP = 0;
        float HeightCenterP = 0;
        float HeightDiffP = 0;
        float CanReachP = 0;
        int lastTimeInsideP = 0;

        float HeightOutN = 0;
        float HeightCenterN = 0;
        float HeightDiffN = 0;
        float CanReachN = 0;
        int lastTimeInsideN = 0;

        int directionP = (direction + 1) % 4;
        int directionN = ((direction - 1) % 4 + 4) % 4;

        nav.Cells[xPos, zPos].GetValuesInDir(direction, out HeightOutF, out HeightCenterF, out HeightDiffF, out CanReachF, out lastTimeInsideF);
        nav.Cells[xPos, zPos].GetValuesInDir(directionP, out HeightOutP, out HeightCenterP, out HeightDiffP, out CanReachP, out lastTimeInsideP);
        nav.Cells[xPos, zPos].GetValuesInDir(directionN, out HeightOutN, out HeightCenterN, out HeightDiffN, out CanReachN, out lastTimeInsideN);

                     
        float reachability  = CanReachF  * ((Mathf.Abs(HeightDiffF)  < 0.05f) ? 2.0f : ((Mathf.Abs(HeightDiffF)  < 0.6f) ? 0.5f : 0));
        float reachabilityP = CanReachP * ((Mathf.Abs(HeightDiffP) < 0.05f) ? 2.0f : ((Mathf.Abs(HeightDiffP) < 0.6f) ? 0.5f : 0));
        float reachabilityN = CanReachN * ((Mathf.Abs(HeightDiffN) < 0.05f) ? 2.0f : ((Mathf.Abs(HeightDiffN) < 0.6f) ? 0.5f : 0));

        float novelty = (stepNum - lastTimeInsideF) / 200.0f;
        float noveltyP = (stepNum - lastTimeInsideP) / 200.0f;
        float noveltyN = (stepNum - lastTimeInsideN) / 200.0f;

        float heightGain = Mathf.Max(0, HeightCenterF - height);
        float heightGainP = Mathf.Max(0, HeightCenterP - height);
        float heightGainN = Mathf.Max(0, HeightCenterN - height);

        float desirabilityFront = reachability * (novelty + heightGain) * 1.2f;
        float desirabilityP = reachabilityP * (noveltyP + heightGainP);
        float desirabilityN = reachabilityN * (noveltyN + heightGainN);

        float desirability = 0;

        //Si faut faire demitour
        if (desirabilityP <= float.Epsilon && desirabilityN <= float.Epsilon && desirabilityFront <= float.Epsilon)
        {
            accumNoTurn = 0;
            direction = (direction + 2) % 4;
        } 
        else if (desirabilityFront > desirabilityP && desirabilityFront > desirabilityN)
        {
            //Aller tout droit
            desirability = desirabilityFront;
        } 
        else 
        {
            accumNoTurn /= 2;

            if (desirabilityP > desirabilityN)
            {
                direction = directionP;
                desirability = desirabilityP;
            }
            else
            {
                direction = directionN;
                desirability = desirabilityN;
            }
        }

        accumNoTurn += desirability;
        fitnessStep = accumNoTurn;

        int xPosNext = xPos + xMoves[direction];
        int zPosNext = zPos + zMoves[direction];

        //Si ca nous fait sortir ou que c'est nul d'avancer
        if (desirability == 0 || xPosNext < 0 || xPosNext > nav.Cells.GetUpperBound(0) || zPosNext < 0 || zPosNext > nav.Cells.GetUpperBound(0))
            return fitnessStep;

        xPos = xPosNext;
        zPos = zPosNext;
        height = nav.Cells[xPos, zPos].height;

        return fitnessStep;
    }
}
