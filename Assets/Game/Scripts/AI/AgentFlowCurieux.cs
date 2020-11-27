using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentFlowCurieux
{
    public int xPos;
    public int zPos;
    public int direction;
    public float accumNoTurn = 0;
    public int stepNum = 0;

    NavGrid nav;

    int[] xMoves = { 0, 1, 0, -1 }; //Deplacement sur X en fonction de la rotation
    int[] zMoves = { 1, 0, -1, 0 }; //Deplacement sur X en fonction de la rotation

    public void Init(int xStart, int zStart, int directionStart, NavGrid grid)
    {
        direction = directionStart;
        xPos = xStart;
        zPos = zStart;
        nav = grid;
    }
    

    //Retourne un delta de fitness
    public float Step()
    {
        float fitnessStep = 0;
        
        stepNum++;
        nav.Cells[xPos, zPos].lastTimeInside = stepNum;

        float Height = 0;
        float HeightDiff = 0;
        float CanReach = 0;
        int lastTimeInside = 0;

        float HeightP = 0;
        float HeightDiffP = 0;
        float CanReachP = 0;
        int lastTimeInsideP = 0;

        float HeightN = 0;
        float HeightDiffN = 0;
        float CanReachN = 0;
        int lastTimeInsideN = 0;

        int directionP = (direction + 1) % 4;
        int directionN = ((direction - 1) % 4 + 4) % 4;

        nav.Cells[xPos, zPos].GetValuesInDir(direction, out Height, out HeightDiff, out CanReach, out lastTimeInside);
        nav.Cells[xPos, zPos].GetValuesInDir(directionP, out HeightP, out HeightDiffP, out CanReachP, out lastTimeInsideP);
        nav.Cells[xPos, zPos].GetValuesInDir(directionN, out HeightN, out HeightDiffN, out CanReachN, out lastTimeInsideN);

                     
        float reachability = CanReach * ((HeightDiff < 0.05f) ? 2.0f : ((HeightDiff < 0.6f) ? 0.5f : 0));
        float reachabilityP = CanReachP * ((HeightDiffP < 0.05f) ? 2.0f : ((HeightDiffP < 0.6f) ? 0.5f : 0));
        float reachabilityN = CanReachN * ((HeightDiffN < 0.05f) ? 2.0f : ((HeightDiffN < 0.6f) ? 0.5f : 0));

        float novelty = (stepNum - lastTimeInside) / 200.0f;
        float noveltyP = (stepNum - lastTimeInsideP) / 200.0f;
        float noveltyN = (stepNum - lastTimeInsideN) / 200.0f;

        float desirabilityFront = reachability * novelty * 1.2f;
        float desirabilityP = reachabilityP * noveltyP;
        float desirabilityN = reachabilityN * noveltyN;


        float desirability = 0;
        //Aller tout droit
        if(desirabilityFront > desirabilityP && desirabilityFront > desirabilityN)
        {
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

            //Si faut faire demitour
            if (desirabilityP == 0 && desirabilityN == 0)
            {
                accumNoTurn = 0;
                direction = (direction + 2) % 4;
            }
        }

        accumNoTurn += desirabilityFront;
        fitnessStep = accumNoTurn;

        xPos += xMoves[direction];
        zPos += zMoves[direction];

        /*if (HeightDiff < 0.05f)
        {
            accumNoTurn += CanReach / 2.0f + ((lastTimeInside == 0 || (stepNum - lastTimeInside) > 20) ? 1.0f : 0.0f);
            fitnessStep = accumNoTurn;
        }
        else if (HeightDiff < 0.6f)
        {
            accumNoTurn += CanReach / 3.0f + ((lastTimeInside == 0 || (stepNum - lastTimeInside) > 20) ? 1.0f : 0.0f);
            fitnessStep = accumNoTurn;
        }

        if (HeightDiff > 0.6 || CanReach == 0)
        {
            accumNoTurn /= 2;

            //On se trouve une direction cool
            

            bool choiceMade = false;
            if (lastTimeInsideP != lastTimeInsideN)
            {
                if (lastTimeInsideP < lastTimeInsideN && reachabilityP > 0)
                {
                    direction = directionP;
                    choiceMade = true;
                }
                    

                if (lastTimeInsideP > lastTimeInsideN && reachabilityN > 0)
                {
                    direction = directionN; //in cas of neg 
                    choiceMade = true;
                }
                    
            }
            
            if(!choiceMade)
            {
                if (reachabilityP > reachabilityN)
                {
                    direction = directionP;
                }
                else
                {
                    direction = directionN;
                }
            }


            //Si faut faire demitour
            if (reachabilityP == 0 && reachabilityN == 0)
            {
                accumNoTurn = 0;
                direction = (direction + 2) % 4;
            }

            //direction = (direction + RandomUtility.NextDouble() > 0.5f ? 1 : -1) % 4;

        }
        else
        {
            xPos += xMoves[direction];
            zPos += zMoves[direction];
        }     */

        return fitnessStep;
    }
}
