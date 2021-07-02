using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LndAgent
{
    //Utiles 
    protected NavGrid nav;

    protected int[] xMoves = { 0, 1, 1, 1, 0, -1, -1, -1}; //Deplacement sur X en fonction de la rotation
    protected int[] zMoves = { 1, 1, 0, -1, -1, -1, 0, 1}; //Deplacement sur X en fonction de la rotation

    protected float gridSizeX;
    protected float gridSizeZ;
    protected float gridUnitSize;

    public int xPos;
    public int zPos;
    public float height;
    public int direction;
    public float accumNoTurn = 0;
    public int stepNum = 0;

    protected Vector3 positionF;
    protected Vector3 positionL;
    protected Vector3 positionR;
    protected Vector3 positionB;

    public void Init(int xStart, int zStart, int heightStart, int directionStart, NavGrid grid, float gridUnitSize)
    {
        direction = directionStart;
        xPos = xStart;
        zPos = zStart;
        height = heightStart;
        nav = grid;
        gridSizeX = nav.Cells.GetUpperBound(0) + 1;
        gridSizeZ = nav.Cells.GetUpperBound(0) + 1;
        this.gridUnitSize = gridUnitSize;
    }

    public virtual void InitParams(ScriptableObject param)
    {

    }


    //Retourne un delta de fitness
    public virtual void UpdatePerception()
    { 
    }

    public virtual float TakeDecision()
    {
        return 0.0f;
    }

    public virtual void debugGizmo(Vector3 origin)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(origin + new Vector3(xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2 + height, zPos * gridUnitSize + gridUnitSize / 2.0f), new Vector3(gridUnitSize * 0.8f, gridUnitSize * 0.8f, gridUnitSize * 0.8f));
        Gizmos.color = Color.green;


        Vector3 offset = new Vector3(xMoves[direction], 0, zMoves[direction]);
        
        positionF = origin +
            new Vector3(xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, zPos * gridUnitSize + gridUnitSize / 2.0f) +
            offset * gridUnitSize * 0.55f;

        Gizmos.DrawCube(positionF, new Vector3(gridUnitSize * 0.2f, gridUnitSize * 0.2f, gridUnitSize * 0.2f));
                
        /*positionF = origin +
            new Vector3(xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, zPos * gridUnitSize + gridUnitSize / 2.0f) +
            offset * gridUnitSize;

        positionL = origin +
            new Vector3(xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, zPos * gridUnitSize + gridUnitSize / 2.0f) +
            offset[(direction + 3) % 4] * gridUnitSize;

        positionR = origin +
        new Vector3(xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, zPos * gridUnitSize + gridUnitSize / 2.0f) +
        offset[(direction + 1) % 4] * gridUnitSize;

        positionB = origin +
        new Vector3(xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, zPos * gridUnitSize + gridUnitSize / 2.0f) +
        offset[(direction + 2) % 4] * gridUnitSize;*/
    }

    public virtual void debugGui(Vector3 origin)
    {

    }

    public float Step()
    {
        stepNum++;
        UpdatePerception();
        return TakeDecision();
    }
}
