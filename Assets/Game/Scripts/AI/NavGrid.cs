using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimpleGridWFC;

public class NavGrid 
{
    public float maxHeight = 0;
    public struct Cell
    {
        //Toutes ces valeurs sont avec la rotation du module appliquée...
        public int lastTimeInside;
        public int id;
        public float XPHeightOutDiff; //Diff de hauteur pour changer de cube
        public float XNHeightOutDiff;
        public float ZPHeightOutDiff;
        public float ZNHeightOutDiff;     
        
        public float HeightOutXP; //Hauteur de notre sortie dans cette direction XP is for XPLUS
        public float CanReachFromInsideXP;
        public float HeightCenterNextXP; //Hauteur centrale moyenne de la prochaine case
        public float DistWallXP;

        public float HeightOutXN; 
        public float CanReachFromInsideXN;
        public float HeightCenterNextXN;
        public float DistWallXN;

        public float HeightOutZP; 
        public float CanReachFromInsideZP;
        public float HeightCenterNextZP;
        public float DistWallZP;

        public float HeightOutZN; 
        public float CanReachFromInsideZN;
        public float HeightCenterNextZN;
        public float DistWallZN;


        public int xPos;
        public int zPos;
        public float height; //Moyenne des hauteurs atteignables
        public float distWallMean;
        public float distWallMeanOfSq;
        //public float distToNearestWall; //Distance au mur le plus proche
        //public float tmpDistToNearestWall; //Distance au mur le plus proche pendant le calcul (pour plusieurs hauteurs)

        public NavGrid grid;

        //La dir0 c'est ZPos, le forward
        public void GetValuesInDir(int dir, 
            out float heightOut, //Hauteur de sortie de mon cube si je vais dans cette direction
            out float heightCenterNext, //Hauteur du centre du prochain cube dans cette direction
            out float heightBorderDiffToNext, //Différence de hauteur pour aller dans le prochain cube
            out float canReachNext, //A quel point je peut atteindre le prochain cube (Min des difficulté à traverser pour les deux cubes dans cette direction)
            out int lastTimeInsideNext,
            out float distWallMeanNext,
            out float distWallMeanOfSqNext) //Moyenne dans la prochaine case de la distance au mur
        {

            //Direction Z (0)
            heightOut = HeightOutZP;
            heightCenterNext = HeightCenterNextZP;
            heightBorderDiffToNext = ZPHeightOutDiff;
            canReachNext = CanReachFromInsideZP;
            lastTimeInsideNext = 0;
            distWallMeanNext = 0;
            distWallMeanOfSqNext = 0;

            if (zPos + 1 <= grid.Cells.GetUpperBound(1))
            {
                lastTimeInsideNext = grid.Cells[xPos, zPos + 1].lastTimeInside;
                canReachNext = Mathf.Min(canReachNext,grid.Cells[xPos, zPos + 1].CanReachFromInsideZN);
                distWallMeanNext = grid.Cells[xPos, zPos + 1].distWallMean;
                distWallMeanOfSqNext = grid.Cells[xPos, zPos + 1].distWallMeanOfSq;
            }
                        
            switch (dir)
            {
                case 1:
                    heightOut = HeightOutXP;
                    heightCenterNext = HeightCenterNextXP;
                    heightBorderDiffToNext = XPHeightOutDiff;
                    canReachNext = CanReachFromInsideXP;
                    if (xPos + 1 <= grid.Cells.GetUpperBound(0))
                    { 
                        lastTimeInsideNext = grid.Cells[xPos+1, zPos].lastTimeInside;
                        canReachNext = Mathf.Min(canReachNext, grid.Cells[xPos+1, zPos].CanReachFromInsideXN);
                        distWallMeanNext = grid.Cells[xPos + 1, zPos].distWallMean;
                        distWallMeanOfSqNext = grid.Cells[xPos + 1, zPos].distWallMeanOfSq;
                    }
            break;
                case 2:
                    heightOut = HeightOutZN;
                    heightCenterNext = HeightCenterNextZN;
                    heightBorderDiffToNext = ZNHeightOutDiff;
                    canReachNext = CanReachFromInsideZN;
                    if (zPos - 1 >= 0)
                    { 
                        lastTimeInsideNext = grid.Cells[xPos, zPos - 1].lastTimeInside;
                        canReachNext = Mathf.Min(canReachNext, grid.Cells[xPos, zPos - 1].CanReachFromInsideZP);
                        distWallMeanNext = grid.Cells[xPos, zPos - 1].distWallMean;
                        distWallMeanOfSqNext = grid.Cells[xPos, zPos - 1].distWallMeanOfSq;
                    }
                    break;
                case 3:
                    heightOut = HeightOutXN;
                    heightCenterNext = HeightCenterNextXN;
                    heightBorderDiffToNext = XNHeightOutDiff;
                    canReachNext = CanReachFromInsideXN;
                    if (xPos - 1 >= 0)
                    { 
                        lastTimeInsideNext = grid.Cells[xPos-1, zPos].lastTimeInside;
                        canReachNext = Mathf.Min(canReachNext, grid.Cells[xPos-1, zPos].CanReachFromInsideXP);
                        distWallMeanNext = grid.Cells[xPos-1, zPos].distWallMean;
                        distWallMeanOfSqNext = grid.Cells[xPos - 1, zPos].distWallMeanOfSq;
                    }
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
        maxHeight = 0;

        for (int x=0;x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                Cells[x, z].lastTimeInside = -100000;
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

                    Cells[x, z].HeightCenterNextXP = 0;
                    Cells[x, z].HeightCenterNextXN = 0;
                    Cells[x, z].HeightCenterNextZP = 0;
                    Cells[x, z].HeightCenterNextZN = 0;

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

                if (Cells[x, z].HeightOutXN > maxHeight)
                    maxHeight = Cells[x, z].HeightOutXN;
                if (Cells[x, z].HeightOutXP > maxHeight)
                    maxHeight = Cells[x, z].HeightOutXP;
                if (Cells[x, z].HeightOutZN > maxHeight)
                    maxHeight = Cells[x, z].HeightOutZN;
                if (Cells[x, z].HeightOutZP > maxHeight)
                    maxHeight = Cells[x, z].HeightOutZP;
            }
        }

        //Liens entre hauteurs
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                //Voisin XNeg
                if (x == 0)
                {
                    Cells[x, z].XNHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterNextXN = float.MaxValue;
                }
                else
                {
                    Cells[x, z].XNHeightOutDiff = Cells[x - 1, z].HeightOutXP - Cells[x, z].HeightOutXN;
                    Cells[x, z].HeightCenterNextXN = Cells[x - 1, z].height;
                }

                //Voisin XPos
                if (x == sizeX - 1)
                {
                    Cells[x, z].XPHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterNextXP = float.MaxValue;
                }
                else
                {
                    Cells[x, z].XPHeightOutDiff = Cells[x + 1, z].HeightOutXN - Cells[x, z].HeightOutXP;
                    Cells[x, z].HeightCenterNextXP = Cells[x + 1, z].height;
                }

                //Voisin ZNeg
                if (z == 0)
                {
                    Cells[x, z].ZNHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterNextZN = float.MaxValue;
                }
                else
                {
                    Cells[x, z].ZNHeightOutDiff = Cells[x, z - 1].HeightOutZP - Cells[x, z].HeightOutZN;
                    Cells[x, z].HeightCenterNextZN = Cells[x, z - 1].height;
                }

                //Voisin XPos
                if (z == sizeX - 1)
                {
                    Cells[x, z].ZPHeightOutDiff = float.MaxValue;
                    Cells[x, z].HeightCenterNextZP = float.MaxValue;
                }
                else
                {
                    Cells[x, z].ZPHeightOutDiff = Cells[x, z + 1].HeightOutZN - Cells[x, z].HeightOutZP;
                    Cells[x, z].HeightCenterNextZP = Cells[x, z + 1].height;
                }


                
            }

            computeVisibility();
        }
    }

    /**
     * SDF directionnel Simple raymarch 
     */
    void computeVisibility()
    {
        int sizeX = Cells.GetUpperBound(0) + 1;
        int sizeZ = Cells.GetUpperBound(1) + 1;
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                computeVisibilityCell(x, z);
            }
        }
    }

    void computeVisibilityCell(int cx, int cz)
    {
        int sizeX = Cells.GetUpperBound(0) + 1;
        int sizeZ = Cells.GetUpperBound(1) + 1;
        float heightCheck = Cells[cx, cz].height;

        //On balance un rayon sur x+
        Cells[cx, cz].DistWallXP = 0;
        float visibility = 1;
        for (int x = cx; x < sizeX; x++)
        {
            //Si on a avancé d'au moins une case
            if (x > cx)
            {
                //Rayon s'est bloqué
                if (heightCheck + 0.5f < Cells[x, cz].HeightOutXN)
                {
                    visibility = 0;
                    break;
                }

                //Le rayon a fait la demi case jusqu'au centre
                Cells[cx, cz].DistWallXP += 0.5f;
            }

            //Jusqu'ou on voit dans la case depuis le centre
            if (heightCheck + 0.5f < Cells[x, cz].HeightOutXP)
            {
                visibility = 0;
                break;
            }

            Cells[cx, cz].DistWallXP += 0.5f;
        }

        //Si on voit encore
        /*if (visibility > 0)
            Cells[cx, cz].DistWallXP = float.MaxValue;*/


        //On balance un rayon sur x-
        Cells[cx, cz].DistWallXN = 0;
        visibility = 1;
        for (int x = cx; x >= 0; x--)
        {
            //Si on a avancé d'au moins une case
            if (x < cx)
            {
                //Rayon s'est bloqué
                if (heightCheck + 0.5f < Cells[x, cz].HeightOutXP)
                {
                    visibility = 0;
                    break;
                }

                //Le rayon a fait la demi case jusqu'au centre
                Cells[cx, cz].DistWallXN += 0.5f;
            }

            //Jusqu'ou on voit dans la case depuis le centre
            if (heightCheck + 0.5f < Cells[x, cz].HeightOutXN)
            {
                visibility = 0;
                break;
            }

            Cells[cx, cz].DistWallXN += 0.5f;
        }

        //Si on voit encore
        /*if (visibility > 0)
            Cells[cx, cz].DistWallXN = float.MaxValue;*/


        //On balance un rayon sur z+
        Cells[cx, cz].DistWallZP = 0;
        visibility = 1;
        for (int z = cz; z < sizeZ; z++)
        {
            //Si on a avancé d'au moins une case
            if (z > cz)
            {
                //Rayon s'est bloqué
                if (heightCheck + 0.5f < Cells[cx, z].HeightOutZN)
                {
                    visibility = 0;
                    break;
                }

                //Le rayon a fait la demi case jusqu'au centre
                Cells[cx, cz].DistWallZP += 0.5f;
            }

            //Jusqu'ou on voit dans la case depuis le centre
            if (heightCheck + 0.5f < Cells[cx, z].HeightOutZP)
            {
                visibility = 0;
                break;
            }

            Cells[cx, cz].DistWallZP += 0.5f;
        }

        //Si on voit encore
        /*if (visibility > 0)
            Cells[cx, cz].DistWallZP = float.MaxValue;*/


        
        //On balance un rayon sur Z-
        Cells[cx, cz].DistWallZN = 0;
        visibility = 1;
        for (int z = cz; z >= 0; z--)
        {
            //Si on a avancé d'au moins une case
            if (z < cz)
            {
                //Rayon s'est bloqué
                if (heightCheck + 0.5f < Cells[cx, z].HeightOutZP)
                {
                    visibility = 0;
                    break;
                }

                //Le rayon a fait la demi case jusqu'au centre
                Cells[cx, cz].DistWallZN += 0.5f;
            }

            //Jusqu'ou on voit dans la case depuis le centre
            if (heightCheck + 0.5f < Cells[cx, z].HeightOutZN)
            {
                visibility = 0;
                break;
            }

            Cells[cx, cz].DistWallZN += 0.5f;
        }

        //Si on voit encore
        /*if (visibility > 0)
            Cells[cx, cz].DistWallZN = float.MaxValue;*/

        Cells[cx, cz].distWallMean = Cells[cx, cz].DistWallXN + Cells[cx, cz].DistWallXP + Cells[cx, cz].DistWallZN + Cells[cx, cz].DistWallZP;
        Cells[cx, cz].distWallMean /= 4.0f;

        Cells[cx, cz].distWallMeanOfSq = Mathf.Pow(Cells[cx, cz].DistWallXN,0.5f) +
             Mathf.Pow(Cells[cx, cz].DistWallXP, 0.5f)+
             Mathf.Pow(Cells[cx, cz].DistWallZN, 0.5f)+
             Mathf.Pow(Cells[cx, cz].DistWallZP, 0.5f);
        Cells[cx, cz].distWallMeanOfSq /= 4.0f;
    }


    /**
     * SDF ITERATIF
     */

   /* void computeSDF()
    {
        computeSdfAtHeight(0);
        computeSdfAtHeight(2);
        computeSdfAtHeight(4);
    }


    void initSdf(float height)
    {
        int sizeX = Cells.GetUpperBound(0) + 1;
        int sizeZ = Cells.GetUpperBound(1) + 1;
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                if (Cells[x, z].HeightOutXN > (height + 0.5f) ||
                   Cells[x, z].HeightOutXP > (height + 0.5f) ||
                   Cells[x, z].HeightOutZN > (height + 0.5f) ||
                   Cells[x, z].HeightOutZP > (height + 0.5f))
                    Cells[x, z].tmpDistToNearestWall = 0;
                else
                    Cells[x, z].tmpDistToNearestWall = float.MaxValue;
            }
        }
    }

    void computeSdfAtHeight(float height)
    {
        initSdf(height);

        for (int i = 0; i < 10; i++)
        {
            int numPassX = 0;
            while (passSdf(true, numPassX))
                numPassX++;
            int numPassY = 0;
            while (passSdf(false, numPassY))
                numPassY++;

            if (numPassY == 0 && numPassY == 0)
                break;
        }

        assignSdfValue(height);
    }

    //Pour tenir compte de la hauteur
    void assignSdfValue(float height)
    {
        int sizeX = Cells.GetUpperBound(0) + 1;
        int sizeZ = Cells.GetUpperBound(1) + 1;
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                if (Cells[x, z].height > (height - 0.5) &&
                    Cells[x, z].height < (height + 0.5f))
                    Cells[x, z].distToNearestWall = Cells[x, z].tmpDistToNearestWall;
            }
        }
    }
    

    bool passSdf(bool horizontal, int numPass)
    {
        bool changed = false;

        int sizeX = Cells.GetUpperBound(0) + 1;
        int sizeZ = Cells.GetUpperBound(1) + 1;
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                if (horizontal)
                {
                    float xp = float.MaxValue;
                    float xn = float.MaxValue;
                    //On compare les voisins
                    if (x > 0)
                        xn = Cells[x - 1, z].tmpDistToNearestWall + numPass * 2 + 1;
                    if (x < sizeX-1)
                        xp = Cells[x + 1, z].tmpDistToNearestWall + numPass * 2 + 1;

                    float d = Mathf.Min(xp, xn);
                    if(d < Cells[x, z].tmpDistToNearestWall)
                    {
                        Cells[x, z].tmpDistToNearestWall = d;
                        changed = true;
                    }
                }
                else
                {
                    float zp = float.MaxValue;
                    float zn = float.MaxValue;
                    //On compare les voisins
                    if (z > 0)
                        zn = Cells[x, z - 1].tmpDistToNearestWall + numPass * 2 + 1;
                    if (z < sizeZ - 1)
                        zp = Cells[x, z + 1].tmpDistToNearestWall + numPass * 2 + 1;

                    float d = Mathf.Min(zp, zn);
                    if (d < Cells[x, z].tmpDistToNearestWall)
                    {
                        Cells[x, z].tmpDistToNearestWall = d;
                        changed = true;
                    }
                }
            }
        }

        return changed;
    }*/
}

