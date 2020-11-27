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
                
                switch (m.rotationY)
                {
                    case 0:
                        Cells[x, z].HeightXP = m.linkedTile.pi.NavHeightXPosRot0;
                        Cells[x, z].HeightXN = m.linkedTile.pi.NavHeightXNegRot0;
                        Cells[x, z].HeightZP = m.linkedTile.pi.NavHeightZPosRot0;
                        Cells[x, z].HeightZN = m.linkedTile.pi.NavHeightZNegRot0;
                        break;
                    case 1:
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                }
                
            }
        }
        
    }
}
