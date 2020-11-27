using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimpleGridWFC;

public class NavGrid 
{
    struct Cell
    {
        public bool XP;
        public bool XN;
        public bool ZP;
        public bool ZN;
        public float HeightXP; //Avec la rotation du module appliquée...
        public float HeightXN; //Avec la rotation du module appliquée...
        public float HeightZP; //Avec la rotation du module appliquée...
        public float HeightZN; //Avec la rotation du module appliquée...
    }

    Cell[,] Cells;

    public void Build(Module[,] modules, float stepHeight = 0.05f)
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
                        break;
                    case 1:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightXPosRot0; 
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightXNegRot0; 
                        break;
                    case 2:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightXNegRot0;
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightXPosRot0; 
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightZNegRot0; 
                        break;
                    case 3:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightZNegRot0;
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightXNegRot0; 
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightZPosRot0; 
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightXPosRot0; 
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
                    Cells[x, z].XN = false;
                }
                else
                {
                    Cells[x, z].XN = Mathf.Abs(Cells[x - 1, z].HeightXP - Cells[x, z].HeightXN) < stepHeight;
                }

                //Voisin XPos
                if (x == sizeX - 1)
                {
                    Cells[x, z].XP = false;
                }
                else
                {
                    Cells[x, z].XP = Mathf.Abs(Cells[x + 1, z].HeightXN - Cells[x, z].HeightXP) < stepHeight;
                }

                //Voisin ZNeg
                if (z == 0)
                {
                    Cells[x, z].ZN = false;
                }
                else
                {
                    Cells[x, z].ZN = Mathf.Abs(Cells[x, z-1].HeightZP - Cells[x, z].HeightZN) < stepHeight;
                }

                //Voisin XPos
                if (z == sizeX - 1)
                {
                    Cells[x, z].ZP = false;
                }
                else
                {
                    Cells[x, z].ZP = Mathf.Abs(Cells[x, z+1].HeightZN - Cells[x, z].HeightZP) < stepHeight;
                }
            }
        }
    }
}
