using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimpleGridWFC;

public class NavGrid 
{
    public float maxHeight = 0;

    

    public unsafe struct Cell
    {
        //Toutes ces valeurs sont avec la rotation du module appliquée...
        //La direction 0 est sur Z+

        public int lastTimeInside;
        public int id;

        //Propre à la cellule
        public float Height; //Hauteur du centre de la cellule
        public fixed float HeightOut[8]; //Hauteur du bord de la cellule
        public fixed float CanReachFromInside[8]; //Si on peut atteindre la hauteur au bord ou au coin depuis l'intérieur (exemple mur contre le bord et milieu bas)

        //Calculé en tenant compte du voisinage
        public fixed float HeightOutDiffNext[8]; //Différence de hauteur sur le bord ou le coin avec la case voisine
        public fixed float HeightCenterNext[8]; //AVoir une idée du gain d'élévation sur les case voisines
        public fixed float CanGoToNext[8]; //Si c'est navigable
        public fixed float Visibility[8]; //Combien de cases on voit quand on regarde dans cette direction (avec un H FOV de 90)

        public int xPos;
        public int zPos;

        public NavGrid grid;

        public float tempVisibility;

        public ref Cell getVoisin(int direction,  out bool found)
        {
            found = true;

            int xDir = 0;
            int zDir = 0;
            switch (direction)
            {
                case 0: xDir =  0; zDir = +1; break;
                case 1: xDir =  1; zDir = +1; break;
                case 2: xDir =  1; zDir =  0; break;
                case 3: xDir =  1; zDir = -1; break;
                case 4: xDir =  0; zDir = -1; break;
                case 5: xDir = -1; zDir = -1; break;
                case 6: xDir = -1; zDir =  0; break;
                case 7: xDir = -1; zDir = +1; break;
            }

            int xV = xPos + xDir;
            int zV = zPos + zDir;

            if (xV < 0 || zV < 0 || xV >= grid.sizeX || zV >= grid.sizeZ)
            {
                found = false;
                return ref grid.Cells[xPos, zPos]; //me retourne moi meme. Inutile mais quoi d'autre
            }

            return ref grid.Cells[xV, zV];
        }
        
    }

    public Cell[,] Cells;
    public int sizeX;
    public int sizeZ;

    public unsafe void Build(Module[,] modules, float stepMax = 0.5f)
    {

        sizeX = modules.GetUpperBound(0) + 1;
        sizeZ = modules.GetUpperBound(1) + 1;

        Cells = new Cell[sizeX, sizeZ];
        maxHeight = 0;

        float[] heightsOutRotZero = new float[8];
        float[] canReachRotZero = new float[8];

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {

                ref Cell cell = ref Cells[x, z];

                cell.lastTimeInside = -100000;
                cell.grid = this;
                cell.xPos = x;
                cell.zPos = z;

                Module m = modules[x, z];

                //Si pas de module ici (de l'air pas ex ?)
                if (m == null)
                {

                    for (int i = 0; i < 8; i++)
                    {
                        cell.HeightOut[i] = 0;
                        cell.CanReachFromInside[i] = 0;
                    }

                    cell.id = -1;
                    continue;
                }

                cell.id = m.linkedTile.id;


                //Recup des heuteurs en rotation de base

                


                //Cotés
                heightsOutRotZero[0] = m.linkedTile.pi.NavHeightZPosRot0;
                heightsOutRotZero[2] = m.linkedTile.pi.NavHeightXPosRot0;
                heightsOutRotZero[4] = m.linkedTile.pi.NavHeightZNegRot0;
                heightsOutRotZero[6] = m.linkedTile.pi.NavHeightXNegRot0;

                canReachRotZero[0] = m.linkedTile.pi.CanReachFromInsideZPos;
                canReachRotZero[2] = m.linkedTile.pi.CanReachFromInsideXPos;
                canReachRotZero[4] = m.linkedTile.pi.CanReachFromInsideZNeg;
                canReachRotZero[6] = m.linkedTile.pi.CanReachFromInsideXNeg;

                //Inférence des coins
                heightsOutRotZero[1] = System.Math.Max(heightsOutRotZero[0] , heightsOutRotZero[2]);
                heightsOutRotZero[3] = System.Math.Max(heightsOutRotZero[2] , heightsOutRotZero[4]);
                heightsOutRotZero[5] = System.Math.Max(heightsOutRotZero[4] , heightsOutRotZero[6]);
                heightsOutRotZero[7] = System.Math.Max(heightsOutRotZero[6] , heightsOutRotZero[0]);

                canReachRotZero[1] = System.Math.Min(canReachRotZero[0] , canReachRotZero[2]);
                canReachRotZero[3] = System.Math.Min(canReachRotZero[2] , canReachRotZero[4]);
                canReachRotZero[5] = System.Math.Min(canReachRotZero[4] , canReachRotZero[6]);
                canReachRotZero[7] = System.Math.Min(canReachRotZero[6] , canReachRotZero[0]);

                //On place dans le bonne direction (rotation) et on calcule la moyenne
                float sumHeight = 0;
                float sumReach = 0;
                for (int i = 0; i < 8; i++)
                {
                    cell.HeightOut[i] = heightsOutRotZero[(i + m.rotationY) % 8];
                    cell.CanReachFromInside[i] = canReachRotZero[(i + m.rotationY) % 8];
                    sumHeight += cell.CanReachFromInside[i] * cell.HeightOut[i];
                    sumReach += cell.CanReachFromInside[i];
                }
                cell.Height = sumHeight;
                if (sumReach > 0)
                    cell.Height /= sumReach;

            }
        }

        //On a les cells initialisées, on peut consolider les infos tenant compte des voisins
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                ref Cell cell = ref Cells[x, z];
                
                //Pour chaque direction
                for(int i = 0; i < 8; i++)
                {
                    bool found = false;
                    ref Cell voisin = ref cell.getVoisin(i, out found);
                    if (found)
                    {
                        cell.HeightOutDiffNext[i] = voisin.HeightOut[(i + 4) % 8] - cell.HeightOut[i];
                        cell.HeightCenterNext[i] = voisin.HeightCenterNext[(i + 4) % 8];

                        //Attention si on est dans un coin, alors il faut check tous ceux qui touchent le coin autres
                        cell.CanGoToNext[i] = cell.HeightOutDiffNext[i] < stepMax ? cell.CanReachFromInside[i] : 0;
                        if ( i % 2 != 0)
                        {
                            

                            //Ceux qui partagent le meme coin
                            bool foundPrev = true;
                            bool foundNext = true;
                            ref Cell prev = ref cell.getVoisin((i + 7) % 8, out foundPrev); //i-1
                            ref Cell next = ref cell.getVoisin((i + 1) % 8, out foundNext); //i+1

                            float prevCornerHeightDiff = prev.HeightOut[(i + 2) % 8] - cell.HeightOut[i]; //i-1
                            float nextCornerHeightDiff = next.HeightOut[(i + 6) % 8] - cell.HeightOut[i]; //i+1
                                                        
                            cell.CanGoToNext[i] = System.Math.Min(cell.CanGoToNext[i], prevCornerHeightDiff < stepMax ? cell.CanReachFromInside[i] : 0);
                            cell.CanGoToNext[i] = System.Math.Min(cell.CanGoToNext[i], nextCornerHeightDiff < stepMax ? cell.CanReachFromInside[i] : 0);
                        }                        
                    }
                }
            }
        }

        computeVisibility(0, 0, new Ray(Vector3.zero, Vector3.forward), new Ray(Vector3.zero, Vector3.right));
    }

    private void computeVisibility(int xStart, int zStart, Ray lRay, Ray rRay, int prof = 0) 
    {
        //On fait que le premier quadrant
        //On traite pas la hauteur pour le moment

        int dirXScan = +1;
        int dirZScan = -1;
        bool occluderFound = false;

        for (int z = zStart; z < sizeZ; z++)
        {  
            int xScan = xStart;
            int zScan = z;

            bool inRays = true;
            bool afterRayLeft = false;
            bool beforeRayRight = true;
            bool firstInDiag = true;
            do 
            {   
                bool visible = false;
                
                if (!afterRayLeft)
                {
                    Vector3 dir;
                    dir.x = xScan - lRay.origin.x;
                    dir.y = 0;
                    dir.z = zScan - lRay.origin.z;
                    //Debug.Log(lRay.direction + " * " + dir + " : " + Vector3.Cross(lRay.direction, dir).y);
                    afterRayLeft = Vector3.Cross(lRay.direction, dir).y >= 0 ? true : false;
                }
                else
                {
                    Vector3 dir;
                    dir.x = xScan - rRay.origin.x;
                    dir.y = 0;
                    dir.z = zScan - rRay.origin.z;
                    beforeRayRight = Vector3.Cross(dir,rRay.direction).y >= 0 ? true : false;
                    inRays = beforeRayRight;
                }

                Debug.Log(xScan + ", " + zScan + " l: "+ afterRayLeft + " r:"+ beforeRayRight);

                Cells[xScan, zScan].tempVisibility = afterRayLeft && beforeRayRight ? 1 : 0;

                //On prendra ensuite la hauter du cote (ou coin) qui pointe le plus vers moi et voila
                if(Cells[xStart, zStart].Height < Cells[xScan, zScan].Height)
                { 
                    Cells[xScan, zScan].tempVisibility = 0;

                    int xNext = xScan + dirXScan;
                    int zNext = zScan + dirZScan;

                    if (firstInDiag)
                    {
                        lRay = new Ray(lRay.origin, new Vector3(xNext - dirXScan / 2.0f, 0, zNext - dirZScan / 2.0f) - lRay.origin);
                    }
                    else if (xNext < 0 || xNext >= sizeX || zNext < 0 || zNext >= sizeZ)
                    {
                        rRay = new Ray(rRay.origin, new Vector3(xNext + dirXScan / 2.0f, 0, zNext + dirZScan / 2.0f) - rRay.origin); 
                    }
                    else
                    {                       
                        Ray lRayNext = new Ray(lRay.origin, new Vector3(xNext - dirXScan / 2.0f, 0, zNext - dirZScan / 2.0f) - lRay.origin);
                        Ray rRayNext = new Ray(rRay.origin, new Vector3(xNext + dirXScan / 2.0f, 0, zNext + dirZScan / 2.0f) - rRay.origin);
                        occluderFound = true;

                        if (xNext >= 0 && xNext < sizeX && zNext >= 0 && zNext < sizeZ)
                        {
                            Debug.Log("BLOCKED : recurs");
                            computeVisibility(xNext, zNext, lRayNext, rRayNext, ++prof);
                            Debug.Log("END recurs");
                        }
                    }                    
                }

                xScan += dirXScan;
                zScan += dirZScan;

                firstInDiag = false;

            } while (inRays && xScan >= 0 && xScan < sizeX && zScan >= 0 && zScan < sizeZ);
        }
    }
}

