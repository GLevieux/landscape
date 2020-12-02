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

    //Calcul a chaque step
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

    public float noveltyBoost = 1.0f;
    public float heightBoost = 0.8f;
    public float safetyBoost = 0.5f;

    //Utiles 
    NavGrid nav;

    int[] xMoves = { 0, 1, 0, -1 }; //Deplacement sur X en fonction de la rotation
    int[] zMoves = { 1, 0, -1, 0 }; //Deplacement sur X en fonction de la rotation

    float gridSizeX;
    float gridSizeZ;

    public void Init(int xStart, int zStart, int heightStart, int directionStart, NavGrid grid)
    {
        direction = directionStart;
        xPos = xStart;
        zPos = zStart;
        height = heightStart;
        nav = grid;
        gridSizeX = nav.Cells.GetUpperBound(0) + 1;
        gridSizeZ = nav.Cells.GetUpperBound(0) + 1;
    }


    //Retourne un delta de fitness
    public void UpdatePerception()
    {
        stepNum++;
        nav.Cells[xPos, zPos].lastTimeInside = stepNum;
        height = nav.Cells[xPos, zPos].height;
        float safety = 1 - (nav.Cells[xPos, zPos].distWallMean / Mathf.Max(gridSizeX, gridSizeZ));

        float heightOutF = 0;
        float heightCenterNextF = 0;
        float heightBorderDiffToNextF = 0;
        float canReachNextF = 0;
        int lastTimeInsideNextF = 0;
        float meanDistWallNextF = 0;

        float heightOutP = 0;
        float heightCenterNextP = 0;
        float heightBorderDiffToNextP = 0;
        float canReachNextP = 0;
        int lastTimeInsideNextP = 0;
        float meanDistWallNextP = 0;

        float heightOutN = 0;
        float heightCenterNextN = 0;
        float heightBorderDiffToNextN = 0;
        float canReachNextN = 0;
        int lastTimeInsideNextN = 0;
        float meanDistWallNextN = 0;

        float heightOutB = 0;
        float heightCenterNextB = 0;
        float heightBorderDiffToNextB = 0;
        float canReachNextB = 0;
        int lastTimeInsideNextB = 0;
        float meanDistWallNextB = 0;

        int directionP = (direction + 1) % 4;
        int directionN = (direction + 3) % 4;
        int directionB = (direction + 2) % 4;

        nav.Cells[xPos, zPos].GetValuesInDir(direction, out heightOutF, out heightCenterNextF, out heightBorderDiffToNextF, out canReachNextF, out lastTimeInsideNextF, out meanDistWallNextF);
        nav.Cells[xPos, zPos].GetValuesInDir(directionP, out heightOutP, out heightCenterNextP, out heightBorderDiffToNextP, out canReachNextP, out lastTimeInsideNextP, out meanDistWallNextP);
        nav.Cells[xPos, zPos].GetValuesInDir(directionN, out heightOutN, out heightCenterNextN, out heightBorderDiffToNextN, out canReachNextN, out lastTimeInsideNextN, out meanDistWallNextN);
        nav.Cells[xPos, zPos].GetValuesInDir(directionB, out heightOutB, out heightCenterNextB, out heightBorderDiffToNextB, out canReachNextB, out lastTimeInsideNextB, out meanDistWallNextB);

        //Peut on atteindre la suivante
        reachabilityF = canReachNextF * ((Mathf.Abs(heightBorderDiffToNextF) < 0.05f) ? 2.0f : ((Mathf.Abs(heightBorderDiffToNextF) < 0.6f) ? 0.5f : 0));
        reachabilityP = canReachNextP * ((Mathf.Abs(heightBorderDiffToNextP) < 0.05f) ? 2.0f : ((Mathf.Abs(heightBorderDiffToNextP) < 0.6f) ? 0.5f : 0));
        reachabilityN = canReachNextN * ((Mathf.Abs(heightBorderDiffToNextN) < 0.05f) ? 2.0f : ((Mathf.Abs(heightBorderDiffToNextN) < 0.6f) ? 0.5f : 0));
        reachabilityB = canReachNextB * ((Mathf.Abs(heightBorderDiffToNextB) < 0.05f) ? 2.0f : ((Mathf.Abs(heightBorderDiffToNextB) < 0.6f) ? 0.5f : 0));

        reachabilityF /= 2.0f;
        reachabilityP /= 2.0f;
        reachabilityN /= 2.0f;
        reachabilityB /= 2.0f;

        //Est elle nouvelle
        noveltyF = (stepNum - lastTimeInsideNextF) / 200.0f;
        noveltyP = (stepNum - lastTimeInsideNextP) / 200.0f;
        noveltyN = (stepNum - lastTimeInsideNextN) / 200.0f;
        noveltyB = (stepNum - lastTimeInsideNextB) / 200.0f;

        //Gagne t'on de la hauteur
        heightGainF = Mathf.Max(-1, heightCenterNextF - height) / nav.maxHeight;
        heightGainP = Mathf.Max(-1, heightCenterNextP - height) / nav.maxHeight;
        heightGainN = Mathf.Max(-1, heightCenterNextN - height) / nav.maxHeight;
        heightGainB = Mathf.Max(-1, heightCenterNextB - height) / nav.maxHeight;

        //Gagne t'on de la sureté (de l'espace)
        safetyGainF = (1 - (meanDistWallNextF / Mathf.Max(gridSizeX, gridSizeZ))) - safety;
        safetyGainP = (1 - (meanDistWallNextP / Mathf.Max(gridSizeX, gridSizeZ))) - safety;
        safetyGainN = (1 - (meanDistWallNextN / Mathf.Max(gridSizeX, gridSizeZ))) - safety;
        safetyGainB = (1 - (meanDistWallNextB / Mathf.Max(gridSizeX, gridSizeZ))) - safety;

        desirabilityF = reachabilityF * (noveltyF * noveltyBoost + heightGainF * heightBoost + safetyGainF * safetyBoost);
        desirabilityP = reachabilityP * (noveltyP * noveltyBoost + heightGainP * heightBoost + safetyGainP * safetyBoost);
        desirabilityN = reachabilityN * (noveltyN * noveltyBoost + heightGainN * heightBoost + safetyGainN * safetyBoost);
        desirabilityB = reachabilityB * (noveltyB * noveltyBoost + heightGainB * heightBoost + safetyGainB * safetyBoost);

        if (reachabilityF < float.Epsilon)
            desirabilityF = -float.MaxValue;
        if (reachabilityP < float.Epsilon)
            desirabilityP = -float.MaxValue;
        if (reachabilityN < float.Epsilon)
            desirabilityN = -float.MaxValue;
        if (reachabilityB < float.Epsilon)
            desirabilityB = -float.MaxValue;
    }

    public float TakeDecision()
    {
        float fitnessStep = 0;

        float desirability = 0;
        float seuilTourne = 0.05f;
        float seuilDemitour = 0.1f;

        int directionP = (direction + 1) % 4;
        int directionN = (direction + 3) % 4;
        int directionB = (direction + 2) % 4;

        //Si faut faire demitour
        if (desirabilityB > desirabilityF + seuilDemitour &&
            desirabilityB > desirabilityP + seuilDemitour &&
            desirabilityB > desirabilityN + seuilDemitour)
        {
            accumNoTurn = 0;
            direction = (direction + 2) % 4;
        }
        else if (desirabilityF > desirabilityP - seuilTourne &&
                 desirabilityF > desirabilityN - seuilTourne)
        {
            //Aller tout droit
            desirability = desirabilityF;
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
        if (desirability == 0 || xPosNext < 0 || xPosNext > gridSizeX - 1 || zPosNext < 0 || zPosNext > gridSizeZ - 1)
            return fitnessStep;

        xPos = xPosNext;
        zPos = zPosNext;
        height = nav.Cells[xPos, zPos].height;

        return fitnessStep;
    }

    public float Step()
    {
        UpdatePerception();
        return TakeDecision();
    }
}
