using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimpleGridWFC;

public class NavGrid 
{
    public struct Cell
    {
        //Toutes ces valeurs sont avec la rotation du module appliquée...
        public int lastTimeInside;
        public int id;
        public float XPHeightOutDiff; //Diff de hauteur pour changer de cube
        public float XNHeightOutDiff;
        public float ZPHeightOutDiff;
        public float ZNHeightOutDiff;       
        public float HeightOutXP; //Hauteur de notre sortie dans cette direction
        public float CanReachFromInsideXP;
        public float HeightCenterXP; //Hauteur centrale moyenne de la prochaine case
        public float HeightOutXN; 
        public float CanReachFromInsideXN;
        public float HeightCenterXN; 
        public float HeightOutZP; 
        public float CanReachFromInsideZP;
        public float HeightCenterZP;
        public float HeightOutZN; 
        public float CanReachFromInsideZN;
        public float HeightCenterZN;
        public int xPos;
        public int zPos;
        public float height; //Moyenne des hauteurs atteignables
        public NavGrid grid;

        //La dir0 c'est ZPos, le forward
        public void GetValuesInDir(int dir, out float HeightOut, out float HeightCenter, out float HeightDiffToEnter, out float canReachFromInside, out int lastTimeInside)
        {

            //Direction Z (0)
            HeightOut = HeightOutZP;
            HeightCenter = HeightCenterZP;
            HeightDiffToEnter = ZPHeightOutDiff;
            canReachFromInside = CanReachFromInsideZP;
            lastTimeInside = 0;

            if(zPos+1 <= grid.Cells.GetUpperBound(1))
                lastTimeInside = grid.Cells[xPos, zPos+1].lastTimeInside;
                        
            switch (dir)
            {
                case 1:
                    HeightOut = HeightOutXP;
                    HeightCenter = HeightCenterXP;
                    HeightDiffToEnter = XPHeightOutDiff;
                    canReachFromInside = CanReachFromInsideXP;
                    if (xPos + 1 <= grid.Cells.GetUpperBound(0))
                        lastTimeInside = grid.Cells[xPos+1, zPos].lastTimeInside;
                    break;
                case 2:
                    HeightOut = HeightOutZN;
                    HeightCenter = HeightCenterZN;
                    HeightDiffToEnter = ZNHeightOutDiff;
                    canReachFromInside = CanReachFromInsideZN;
                    if (zPos - 1 >= 0)
                        lastTimeInside = grid.Cells[xPos, zPos - 1].lastTimeInside;
                    break;
                case 3:
                    HeightOut = HeightOutXN;
                    HeightCenter = HeightCenterXN;
                    HeightDiffToEnter = XNHeightOutDiff;
                    canReachFromInside = CanReachFromInsideXN;
                    if (xPos - 1 >= 0)
                        lastTimeInside = grid.Cells[xPos-1, zPos].lastTimeInside;
                    break;
            }
        }
    }

    public Cell[,] Cells;

    public void Build(Module[,] modules)
    {
        int sizeX = modules.GetUpperBound(0)+1;
        int sizeZ = modules.GetUpperBound(1)+1;
        Cells = new Cell[sizeX, sizeZ];

        for(int x=0;x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                Cells[x, z].lastTimeInside = 0;
                Cells[x, z].grid = this;
                Cells[x, z].xPos = x;
                Cells[x, z].zPos = z;


                Module m = modules[x, z];
                if (m == null)
                {
                    Cells[x, z].HeightOutXP = 0;
                    Cells[x, z].HeightOutXN = 0;
                    Cells[x, z].HeightOutZP = 0;
                    Cells[x, z].HeightOutZN = 0;

                    Cells[x, z].HeightCenterXP = 0;
                    Cells[x, z].HeightCenterXN = 0;
                    Cells[x, z].HeightCenterZP = 0;
                    Cells[x, z].HeightCenterZN = 0;

                    Cells[x, z].CanReachFromInsideXP = 1;
                    Cells[x, z].CanReachFromInsideZN = 1;
                    Cells[x, z].CanReachFromInsideXN = 1;
                    Cells[x, z].CanReachFromInsideZP = 1;

                    Cells[x, z].id = - 1;
                    continue;
                }

                Cells[x, z].id = modules[x, z].linkedTile.id;

                //On applique la rotation
                switch (m.rotationY)
                {
                    case 0:
                        Cells[x, z].HeightOutXP = m.linkedTile.pi.NavHeightXPosRot0;
                        Cells[x, z].HeightOutZN = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightOutXN = m.linkedTile.pi.NavHeightXNegRot0;
                        Cells[x, z].HeightOutZP = m.linkedTile.pi.NavHeightZPosRot0;

                        Cells[x, z].CanReachFromInsideXP = m.linkedTile.pi.CanReachFromInsideXPos;
                        Cells[x, z].CanReachFromInsideZN = m.linkedTile.pi.CanReachFromInsideZNeg;
                        Cells[x, z].CanReachFromInsideXN = m.linkedTile.pi.CanReachFromInsideXNeg;
                        Cells[x, z].CanReachFromInsideZP = m.linkedTile.pi.CanReachFromInsideZPos;
                        break;
                    case 1:
                        Cells[x, z].HeightOutXP = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightOutZN = m.linkedTile.pi.NavHeightXPosRot0; 
                        Cells[x, z].HeightOutXN = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightOutZP = m.linkedTile.pi.NavHeightXNegRot0;

                        Cells[x, z].CanReachFromInsideXP = m.linkedTile.pi.CanReachFromInsideZPos;
                        Cells[x, z].CanReachFromInsideZN = m.linkedTile.pi.CanReachFromInsideXPos;
                        Cells[x, z].CanReachFromInsideXN = m.linkedTile.pi.CanReachFromInsideZNeg;
                        Cells[x, z].CanReachFromInsideZP = m.linkedTile.pi.CanReachFromInsideXNeg;
                        break;
                    case 2:
                        Cells[x, z].HeightOutXP = m.linkedTile.pi.NavHeightXNegRot0;
                        Cells[x, z].HeightOutZN = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightOutXN = m.linkedTile.pi.NavHeightXPosRot0; 
                        Cells[x, z].HeightOutZP = m.linkedTile.pi.NavHeightZNegRot0;

                        Cells[x, z].CanReachFromInsideXP = m.linkedTile.pi.CanReachFromInsideXNeg;
                        Cells[x, z].CanReachFromInsideZN = m.linkedTile.pi.CanReachFromInsideZPos; 
                        Cells[x, z].CanReachFromInsideXN = m.linkedTile.pi.CanReachFromInsideXPos;
                        Cells[x, z].CanReachFromInsideZP = m.linkedTile.pi.CanReachFromInsideZNeg; 
                        break;
                    case 3:
                        Cells[x, z].HeightOutXP = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightOutZN = m.linkedTile.pi.NavHeightXNegRot0; 
                        Cells[x, z].HeightOutXN = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightOutZP = m.linkedTile.pi.NavHeightXPosRot0;

                        Cells[x, z].CanReachFromInsideXP = m.linkedTile.pi.CanReachFromInsideZNeg;
                        Cells[x, z].CanReachFromInsideZN = m.linkedTile.pi.CanReachFromInsideXNeg; 
                        Cells[x, z].CanReachFromInsideXN = m.linkedTile.pi.CanReachFromInsideZPos; 
                        Cells[x, z].CanReachFromInsideZP = m.linkedTile.pi.CanReachFromInsideXPos; 
                        break;
                }
                
            }
        }

        //On a les bonnes hauteurs appliquées avec les rotation, on peut calculer la nav
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                //Voisin XNeg
                if (x == 0)
                {
                    Cells[x, z].XNHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterXN = float.MaxValue;
                }
                else
                {
                    Cells[x, z].XNHeightOutDiff = Cells[x - 1, z].HeightOutXP - Cells[x, z].HeightOutXN;
                    Cells[x, z].HeightCenterXN = Cells[x - 1, z].height;
                }

                //Voisin XPos
                if (x == sizeX - 1)
                {
                    Cells[x, z].XPHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterXP = float.MaxValue;
                }
                else
                {
                    Cells[x, z].XPHeightOutDiff = Cells[x + 1, z].HeightOutXN - Cells[x, z].HeightOutXP;
                    Cells[x, z].HeightCenterXP = Cells[x + 1, z].height;
                }

                //Voisin ZNeg
                if (z == 0)
                {
                    Cells[x, z].ZNHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterZN = float.MaxValue;
                }
                else
                {
                    Cells[x, z].ZNHeightOutDiff = Cells[x, z - 1].HeightOutZP - Cells[x, z].HeightOutZN;
                    Cells[x, z].HeightCenterZN = Cells[x, z - 1].height;
                }

                //Voisin XPos
                if (z == sizeX - 1)
                {
                    Cells[x, z].ZPHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterZP = float.MaxValue;
                }
                else
                {
                    Cells[x, z].ZPHeightOutDiff = Cells[x, z + 1].HeightOutZN - Cells[x, z].HeightOutZP;
                    Cells[x, z].HeightCenterZP = Cells[x, z + 1].height;
                }


                //Calcul de la hauteur
                float sumReachability = 0;
                float sumHeight = 0;

                sumHeight += Cells[x, z].CanReachFromInsideXN * Cells[x, z].HeightOutXN;
                sumHeight += Cells[x, z].CanReachFromInsideXP * Cells[x, z].HeightOutXP;
                sumHeight += Cells[x, z].CanReachFromInsideZN * Cells[x, z].HeightOutZN;
                sumHeight += Cells[x, z].CanReachFromInsideZP * Cells[x, z].HeightOutZP;

                sumReachability += Cells[x, z].CanReachFromInsideXN;
                sumReachability += Cells[x, z].CanReachFromInsideXP;
                sumReachability += Cells[x, z].CanReachFromInsideZN;
                sumReachability += Cells[x, z].CanReachFromInsideZP;

                Cells[x, z].height = sumHeight;
                if (sumReachability > 0)
                {
                    Cells[x, z].height /= sumReachability;
                }                
            }
        }
    }
}

