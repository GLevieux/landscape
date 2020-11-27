using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimpleGridWFC;

public class NavGrid 
{
    public struct Cell
    {
        //Toutes ces valeurs sont avec la rotation du module appliquée...
        public float XPDiff;
        public float XNDiff;
        public float ZPDiff;
        public float ZNDiff;

        public float HeightXP; 
        public float CanReachFromInsideXP; 
        public float HeightXN; 
        public float CanReachFromInsideXN;
        public float HeightZP; 
        public float CanReachFromInsideZP;
        public float HeightZN; 
        public float CanReachFromInsideZN;
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
                Module m = modules[x, z];
                if (m == null)
                {
                    Cells[x, z].HeightXP = 0;
                    Cells[x, z].HeightXN = 0;
                    Cells[x, z].HeightZP = 0;
                    Cells[x, z].HeightZN = 0;
                    continue;
                }
                
                //On applique la rotation
                switch (m.rotationY)
                {
                    case 0:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightXPosRot0;
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightXNegRot0;
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightZPosRot0;

                        Cells[x, z].CanReachFromInsideXP = m.linkedTile.pi.CanReachFromInsideXPos;
                        Cells[x, z].CanReachFromInsideZN = m.linkedTile.pi.CanReachFromInsideZNeg;
                        Cells[x, z].CanReachFromInsideXN = m.linkedTile.pi.CanReachFromInsideXNeg;
                        Cells[x, z].CanReachFromInsideZP = m.linkedTile.pi.CanReachFromInsideZPos;
                        break;
                    case 1:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightXPosRot0; 
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightXNegRot0;

                        Cells[x, z].CanReachFromInsideXP = m.linkedTile.pi.CanReachFromInsideZPos;
                        Cells[x, z].CanReachFromInsideZN = m.linkedTile.pi.CanReachFromInsideXPos;
                        Cells[x, z].CanReachFromInsideXN = m.linkedTile.pi.CanReachFromInsideZNeg;
                        Cells[x, z].CanReachFromInsideZP = m.linkedTile.pi.CanReachFromInsideXNeg;
                        break;
                    case 2:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightXNegRot0;
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightXPosRot0; 
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightZNegRot0;

                        Cells[x, z].CanReachFromInsideXP = m.linkedTile.pi.CanReachFromInsideXNeg;
                        Cells[x, z].CanReachFromInsideZN = m.linkedTile.pi.CanReachFromInsideZPos; 
                        Cells[x, z].CanReachFromInsideXN = m.linkedTile.pi.CanReachFromInsideXPos;
                        Cells[x, z].CanReachFromInsideZP = m.linkedTile.pi.CanReachFromInsideZNeg; 
                        break;
                    case 3:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightXNegRot0; 
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightXPosRot0;

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
                    Cells[x, z].XNDiff = float.MaxValue;
                }
                else
                {
                    Cells[x, z].XNDiff = Cells[x - 1, z].HeightXP - Cells[x, z].HeightXN;
                }

                //Voisin XPos
                if (x == sizeX - 1)
                {
                    Cells[x, z].XPDiff = float.MaxValue;
                }
                else
                {
                    Cells[x, z].XPDiff = Cells[x + 1, z].HeightXN - Cells[x, z].HeightXP;
                }

                //Voisin ZNeg
                if (z == 0)
                {
                    Cells[x, z].ZNDiff = float.MaxValue; 
                }
                else
                {
                    Cells[x, z].ZNDiff = Cells[x, z - 1].HeightZP - Cells[x, z].HeightZN;
                }

                //Voisin XPos
                if (z == sizeX - 1)
                {
                    Cells[x, z].ZPDiff = float.MaxValue; 
                }
                else
                {
                    Cells[x, z].ZPDiff = Cells[x, z + 1].HeightZN - Cells[x, z].HeightZP;
                }
            }
        }
    }
}

