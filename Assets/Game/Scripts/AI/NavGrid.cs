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
        public fixed bool CornerAndEdges[8]; //Si on a noté la dir comme un corner ou un edge (complexite)
        public fixed float HeightCenterNext[8]; //AVoir une idée du gain d'élévation sur les case voisines
        public fixed float CanGoToNext[8]; //Si c'est navigable
        public fixed float Visibility[8]; //Combien de cases on voit quand on regarde dans cette direction (avec un H FOV de 90)
        public float MeanVisibility; //Visibilité moyenne dans tous les sens
        public fixed float VisibleComplexity[8]; //Complexité vue dans une direction
        public float MeanVisibleComplexity; //Mesure de complexité de ce tile, étant donné ses voisins
        public float LocalComplexity; //Mesure de complexité de ce tile, étant donné ses voisins

        public int xPos;
        public int zPos;

        public NavGrid grid;

        

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

    public unsafe void Build(Module[,] modules, float stepMax = 0.5f, float agentEyeHeight = 1.2f)
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

                for (int iDir = 0; iDir < 8; iDir++)
                    cell.CornerAndEdges[iDir] = false;

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


                //Recup des hauteurs en rotation de base

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
                heightsOutRotZero[1] = System.Math.Max(heightsOutRotZero[0], heightsOutRotZero[2]);
                heightsOutRotZero[3] = System.Math.Max(heightsOutRotZero[2], heightsOutRotZero[4]);
                heightsOutRotZero[5] = System.Math.Max(heightsOutRotZero[4], heightsOutRotZero[6]);
                heightsOutRotZero[7] = System.Math.Max(heightsOutRotZero[6], heightsOutRotZero[0]);

                canReachRotZero[1] = System.Math.Min(canReachRotZero[0], canReachRotZero[2]);
                canReachRotZero[3] = System.Math.Min(canReachRotZero[2], canReachRotZero[4]);
                canReachRotZero[5] = System.Math.Min(canReachRotZero[4], canReachRotZero[6]);
                canReachRotZero[7] = System.Math.Min(canReachRotZero[6], canReachRotZero[0]);

                //On place dans le bonne direction (rotation) et on calcule la moyenne
                float sumHeight = 0;
                float sumReach = 0;
                for (int i = 0; i < 8; i++)
                {
                    cell.HeightOut[i] = heightsOutRotZero[(i + m.rotationY*6) % 8];
                    cell.CanReachFromInside[i] = canReachRotZero[(i + m.rotationY*6) % 8];
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
                for (int i = 0; i < 8; i++)
                {
                    bool found = false;
                    ref Cell voisin = ref cell.getVoisin(i, out found);
                    if (found)
                    {
                        cell.HeightOutDiffNext[i] = voisin.HeightOut[(i + 4) % 8] - cell.HeightOut[i];
                        cell.HeightCenterNext[i] = voisin.HeightCenterNext[(i + 4) % 8];

                        //Attention si on est dans un coin, alors il faut check tous ceux qui touchent le coin autres
                        cell.CanGoToNext[i] = cell.HeightOutDiffNext[i] < stepMax ? cell.CanReachFromInside[i] : 0;

                        if (cell.CanGoToNext[i] != 0 && i % 2 != 0)
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
                    else
                    {
                        cell.CanGoToNext[i] = 0; 
                    }
                }

                
            }
        }

        //Pré calcul pour la complexité : on marque les edges
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                ComputeSimpleEdges(x, z);
            }
        }

        //Calcul de la complexité
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                ref Cell cell = ref Cells[x, z];
                cell.LocalComplexity = ComputeSimpleComplexity(x, z);
            }
        }

        //Calcul de la visibilité (et de la complexité vue, utilise le calcul précédent)
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                ref Cell cell = ref Cells[x, z];
                cell.MeanVisibility = ComputeSimpleVisibility(x, z, cell.Height + agentEyeHeight);                
            }
        }
    }

    //On va simplement envoyer des rayons à 0, 11.25, 22.5, 45, 66.25, 77.5 etc degres (5*4 rayons, 5 par quadrant) et vérifier si
    //on peut naviguer le long de ce rayon en comparant les hauteurs (attention, on part bien de l'altitude du viewer
    //sans la modifier donc c'est pas tout à fait la navigation qui descendrait). Tant qu'on peut, on compte une case visible. 
    //Si la case en bloquée car plus haute, et qu'on est pas juste contre, alors on la marque comme visible et on stop (comme ca on voit sur le bortd c'est tout)
    //Si elle est plus basse on fait comme si de rien n'était et on continuer

    //Au passage on en profite pour gather la complexité dans une direction
    
    public unsafe float ComputeSimpleVisibility(int xCell, int zCell, float height)
    {
        float angleStep = (Mathf.PI/2) / 4;
        float angle = Mathf.PI/2; //On part d'en haut
        float xDir = 0;
        float zDir = 0;
        float lengthVisionMax = 30;
        float visibility = 0;
        float visibleComplexity = 0;

        ref Cell cellVis = ref Cells[xCell, zCell];
        for (int i = 0; i < 8; i++)
            cellVis.Visibility[i] = 0;

        for (int i = 0; i < 8; i++)
            cellVis.VisibleComplexity[i] = 0;

        visibleComplexity = 0;

        //On prend 16 directions, pi/8 a chaque fois
        for (int rDir16 = 0; rDir16 < 16; rDir16++)
        {
            float visibilityRay = 0;
            float complexityRay = 0;

            xDir = (float)System.Math.Cos(angle);
            zDir = (float)System.Math.Sin(angle);

            int xStart = xCell;
            int zStart = zCell;
            int xEnd = Mathf.FloorToInt(xCell + xDir * lengthVisionMax);
            int zEnd = Mathf.FloorToInt(zCell + zDir * lengthVisionMax);

            //On lance le rayon avec algo de tracé de ligne
            int y = zStart;
            int x = xStart;

            int w = xEnd - xStart;
            int h = zEnd - zStart;

            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;

            int longest = System.Math.Abs(w);
            int shortest = System.Math.Abs(h);

            if (!(longest > shortest))
            {
                longest = System.Math.Abs(h);
                shortest = System.Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            bool endRay = false;
            int numerator = longest >> 1;
            for (int i = 0; !endRay && i <= longest; i++)
            {
                //Traitement de la case x,y
                int xPrev = x;
                int yPrev = y;
                float complexity = 0;


                //Case suivante
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }

                //Passage de la case (xPrev,yPrev) à (x,y)
                //Numéro de direction en fonction de diff de coords (de 0 à 7 en partant de up) 
                int iDir = 0;
                int dxRes = x - xPrev;
                int dyRes = y - yPrev;
                if(dxRes == 0)
                {
                    if (dyRes == 1)
                        iDir = 0;
                    else if (dyRes == -1)
                        iDir = 4;
                }
                else if (dxRes == 1)
                {
                    if (dyRes == 1)
                        iDir = 1;
                    else if (dyRes == 0)
                        iDir = 2;
                    else if (dyRes == -1)
                        iDir = 3;
                }
                else if (dxRes == -1)
                {
                    if (dyRes == 1)
                        iDir = 7;
                    else if (dyRes == 0)
                        iDir = 6;
                    else if (dyRes == -1)
                        iDir = 5;
                }

                ref Cell cellPrev = ref Cells[xPrev, yPrev];
                bool found = false;
                ref Cell cellNext = ref cellPrev.getVoisin(iDir, out found);

                //Si plus de cell on stop ce ray
                if (!found)
                    break;

                //Gather la complexity
                complexity = cellNext.LocalComplexity;

                float diffWithMyBorder = cellPrev.HeightOut[iDir] - height;
                float diffWithNextBorder = cellNext.HeightOut[(iDir + 4) % 8] - height;
                
                bool isVisible = diffWithMyBorder <= 0 ? diffWithNextBorder <= 0 : false;

                if (isVisible && iDir % 2 != 0)
                {
                    //Ceux qui partagent le meme coin
                    bool foundPrev = true;
                    bool foundNext = true;
                    ref Cell prev = ref cellPrev.getVoisin((iDir + 7) % 8, out foundPrev); //i-1
                    ref Cell next = ref cellPrev.getVoisin((iDir + 1) % 8, out foundNext); //i+1

                    float prevCornerHeightDiff = prev.HeightOut[(i + 2) % 8] - height; //i-1
                    float nextCornerHeightDiff = next.HeightOut[(i + 6) % 8] - height; //i+1

                    isVisible = prevCornerHeightDiff <= 0 && nextCornerHeightDiff <= 0;
                }

                //On compte dans tous les cas (meme si c'est plus haut on dit qu'on peut voir cette case)
                //Sauf si on est juste contre
                if (!isVisible)
                    endRay = true;

                //Si on peut le voir, ou qu'on peut pas le voir et qu'on est pas juste contre
                if(isVisible || (!isVisible && visibilityRay > 0))
                {
                    visibility++;
                    visibilityRay++;
                    visibleComplexity += complexity;
                    complexityRay += complexity;
                }   
            }

            

            int rDir8 = rDir16 / 2;
            cellVis.Visibility[rDir8] += visibilityRay;
            cellVis.VisibleComplexity[rDir8] += complexityRay;
            //Si direction16 entre deux directions8, on contribue aux deux
            if (rDir16 % 2 != 0)
            {
                cellVis.Visibility[rDir8] += visibilityRay;
                cellVis.Visibility[(rDir8 + 1) % 8] += visibilityRay;

                cellVis.VisibleComplexity[rDir8] += complexityRay;
                cellVis.VisibleComplexity[(rDir8 + 1) % 8] += complexityRay; 
            }           

            //Pour tourner dans le meme sens que les indices de direction des cubes
            angle -= angleStep;
            if (angle < 0)
                angle += Mathf.PI * 2;
        }


        //Normalisation
        for (int i = 0; i < 8; i++)
        {
            cellVis.Visibility[i] /= lengthVisionMax * 3;
            cellVis.VisibleComplexity[i] /= lengthVisionMax * 3;
        }

        visibility /= lengthVisionMax * 16;
        visibleComplexity /= lengthVisionMax * 16;


        //Boost de la valeur pour avoir quelque chose plus proche de notre perception, moin linéaire
        double powerBoost = 0.3;
        for (int i = 0; i < 8; i++)
        {
            cellVis.Visibility[i] = (float)System.Math.Pow(cellVis.Visibility[i], powerBoost);
            cellVis.VisibleComplexity[i] = (float)System.Math.Pow(cellVis.VisibleComplexity[i], powerBoost);
        }

        visibility = (float)System.Math.Pow(visibility, powerBoost);
        visibleComplexity = (float)System.Math.Pow(visibleComplexity, powerBoost);


        cellVis.MeanVisibleComplexity = visibleComplexity;

        return visibility;
    }

    //On doit détecter des features saillantes. Pour ca faut faire comme un détecteurs de coins : 
    //1) on détecte les edges. c'est deja fait par la nav, c'est la différence de hauteur entre les cotés
    //   si elle dépasse un seuil, alors ce coté est bien un "bord" visible
    //2) ensuite il suffit de regarde si on a des edges en "coin" donc si, pour chaque edge, le suivant ou le précédent
    //   est aussi un edge. Si c'est le cas on a un coin, une feature.
    public unsafe void  ComputeSimpleEdges(int xCell, int zCell, float seuilEdge = 0.2f)
    {
        ref Cell cell = ref Cells[xCell, zCell];

        for (int iDir = 0; iDir < 8; iDir++)
            cell.CornerAndEdges[iDir] = false;

        //On note les borders
        if (System.Math.Abs(cell.HeightOutDiffNext[0]) > seuilEdge)
            cell.CornerAndEdges[0] = true;
        if (System.Math.Abs(cell.HeightOutDiffNext[2]) > seuilEdge)
            cell.CornerAndEdges[2] = true;
        if (System.Math.Abs(cell.HeightOutDiffNext[4]) > seuilEdge)
            cell.CornerAndEdges[4] = true;
        if (System.Math.Abs(cell.HeightOutDiffNext[6]) > seuilEdge)
            cell.CornerAndEdges[6] = true;
    }

    public unsafe float ComputeSimpleComplexity(int xCell, int zCell, float seuilEdge = 0.2f)
    {
        float complexity = 0;
        ref Cell cell = ref Cells[xCell, zCell];

        //Pour toutes les dir principales (pas les coins)
        /*for (int iDir = 0; iDir < 4; iDir++)
        {
            //Si je suis un edge
            if (System.Math.Abs(cell.HeightOutDiffNext[iDir * 2]) > seuilEdge)
            {
                cell.CornerAndEdges[iDir * 2] = true;

                //Si j'ai juste après un edge, alors coin
                if (System.Math.Abs(cell.HeightOutDiffNext[((iDir + 1) * 2) % 8]) > seuilEdge)
                {
                    cell.CornerAndEdges[((iDir*2)+1) % 8] = true;
                    complexity++;
                }
                    
                //Si j'ai juste avant un edge, alors coin
                if (System.Math.Abs(cell.HeightOutDiffNext[((iDir + 3) * 2) % 8]) > seuilEdge)
                {
                    cell.CornerAndEdges[((iDir * 2) + 7)] = true;
                    complexity++;
                }
            }
        }*/



        for (int iDirEdge = 0; iDirEdge < 4; iDirEdge++)
        {
            int iDir = iDirEdge * 2;
            int iDirNext = ((iDirEdge + 1) * 2) % 8;
            int iDirCorner = ((iDirEdge * 2) + 1) % 8;

            //Si je suis un edge et le suivant aussi
            if (cell.CornerAndEdges[iDir] && cell.CornerAndEdges[iDirNext])
            {
                cell.CornerAndEdges[iDirCorner] = true;

                //On update les cells dans les bonnes directions, qu'elles aient toutes le coin
                bool foundIDir = false;
                bool foundIDirCorner = false;
                bool foundIDirNext = false;
                ref Cell cellIDir = ref cell.getVoisin(iDir, out foundIDir);
                ref Cell cellIDirCorner = ref cell.getVoisin(iDirCorner, out foundIDirCorner);
                ref Cell cellIDirNext = ref cell.getVoisin(iDirNext, out foundIDirNext);

                if (foundIDir)   cellIDir.CornerAndEdges[(iDirCorner+2)%8] = true;
                if (foundIDirCorner) cellIDirCorner.CornerAndEdges[(iDirCorner + 4) % 8] = true;
                if (foundIDirNext) cellIDirNext.CornerAndEdges[(iDirCorner + 6) % 8] = true;

            }
        }

        /*
        bool foundUp = false;
        bool foundRight = false;
        bool foundDown = false;
        bool foundLeft = false;
        ref Cell cellUp = ref cell.getVoisin(0, out foundUp);
        ref Cell cellRight = ref cell.getVoisin(2, out foundRight);
        ref Cell cellDown = ref cell.getVoisin(4, out foundDown);
        ref Cell cellLeft = ref cell.getVoisin(6, out foundLeft);



        //Border du haut vs border haut voisins gauche droite
        bool borderUpCellLeft = (foundLeft && System.Math.Abs(cellLeft.HeightOutDiffNext[0]) > seuilEdge);
        bool borderUpCellRight = (foundRight && System.Math.Abs(cellRight.HeightOutDiffNext[0]) > seuilEdge);

        if(cell.CornerAndEdges[0] != borderUpCellLeft)
            cell.CornerAndEdges[7] = true;
        if (cell.CornerAndEdges[0] != borderUpCellRight)
            cell.CornerAndEdges[1] = true;

        //Border droit vs border droit voisins haut bas
        bool borderRightCellUp = (foundUp && System.Math.Abs(cellUp.HeightOutDiffNext[2]) > seuilEdge);
        bool borderRightCellDown = (foundDown && System.Math.Abs(cellDown.HeightOutDiffNext[2]) > seuilEdge);

        if (cell.CornerAndEdges[2] != borderRightCellUp)
            cell.CornerAndEdges[1] = true;
        if (cell.CornerAndEdges[2] != borderRightCellDown)
            cell.CornerAndEdges[3] = true;

        //Border du bas vs border bas voisins gauche droite
        bool borderDownCellLeft = (foundLeft && System.Math.Abs(cellLeft.HeightOutDiffNext[4]) > seuilEdge);
        bool borderDownCellRight = (foundRight && System.Math.Abs(cellRight.HeightOutDiffNext[4]) > seuilEdge);

        if (cell.CornerAndEdges[4] != borderDownCellLeft)
            cell.CornerAndEdges[5] = true;
        if (cell.CornerAndEdges[4] != borderDownCellRight)
            cell.CornerAndEdges[3] = true;

        //Border gauche vs border gauche voisins haut bas
        bool borderLeftCellUp = (foundUp && System.Math.Abs(cellUp.HeightOutDiffNext[6]) > seuilEdge);
        bool borderLeftCellDown = (foundDown && System.Math.Abs(cellDown.HeightOutDiffNext[6]) > seuilEdge);

        if (cell.CornerAndEdges[6] != borderLeftCellUp)
            cell.CornerAndEdges[7] = true;
        if (cell.CornerAndEdges[6] != borderLeftCellDown)
            cell.CornerAndEdges[5] = true;*/


        for (int iDir = 0; iDir < 4; iDir++)
            if (cell.CornerAndEdges[(iDir*2)+1])
                complexity++;


        return complexity/4.0f;
    }



        /*public unsafe void Build(Module[,] modules, float stepMax = 0.5f)
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
        }*/

        /*private void computeVisibility(int xStart, int zStart, Ray lRay, Ray rRay, int prof = 0) 
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

                    //Debug.Log(xScan + ", " + zScan + " l: "+ afterRayLeft + " r:"+ beforeRayRight);

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
                                //Debug.Log("BLOCKED : recurs");
                                computeVisibility(xNext, zNext, lRayNext, rRayNext, ++prof);
                                //Debug.Log("END recurs");
                            }
                        }                    
                    }

                    xScan += dirXScan;
                    zScan += dirZScan;

                    firstInDiag = false;

                } while (inRays && xScan >= 0 && xScan < sizeX && zScan >= 0 && zScan < sizeZ);
            }
        }*/
    }


