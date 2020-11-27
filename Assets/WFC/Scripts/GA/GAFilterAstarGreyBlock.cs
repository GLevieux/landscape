#define LOGGER


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Randomizations;
using UnityEngine;

using System.Threading;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using static SimpleGridWFC;
using static RelationGrid;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;
using NesScripts.Controls.PathFind;

public class GAFilterAstarGreyBlock : GAScript
{
    //-------------------------

    //Public Parameters
    [Header("Zone Coverage")]
    [Tooltip("Genetic Search with Astar: longest path with zone coverage")]
    public int sizeZone = 3;//used to divide grid into zone of this size


    public override string ToString()
    {
        return base.ToString()
                 + "-------"
                 + "Zone coverage size is " + sizeZone + "\n";
    }

    //-------------------------

    public override void init(GAConfig gaConfig, WFCConfig wfcConfig, bool forceSeed = false, int seed = 0)
    {
        base.init(gaConfig, wfcConfig, forceSeed, seed);
        //Override nbZones
        gaConfig.nbZones = Mathf.CeilToInt((float)gridSize.x / sizeZone) * Mathf.CeilToInt((float)gridSize.y / sizeZone);
    }

    protected override IFitness getFitnessClass()
    {
        return new WFCFitness(wfcConfig, gaConfig.nbZones, sizeZone, gridSize, forceSeed, randomSeed);
    }

    protected override IChromosome getChromosomeClass()
    {
        return new WFCChromosome(gaConfig.nbZones);
    }

    protected override string getNameClass()
    {
        return this.GetType().Name;
    }

    protected override void generationRan()
    {
#if LOGGER
        //Log to csv all fitness
        IList<IChromosome> tempChro = m_ga.Population.CurrentGeneration.Chromosomes;
        for (int i = 0; i < tempChro.Count; i++)
        {
            Logger.CSV("G" + m_ga.GenerationsNumber.ToString(), tempChro[i].Fitness.ToString());
        }
#endif

        Debug.Log("----------------------------------------------------------------------");
        var fitnessDebug = ((WFCChromosome)m_ga.BestChromosome).Fitness;
        var gridResult = ((WFCChromosome)m_ga.BestChromosome).gridResult;
        var zonesResult = ((WFCChromosome)m_ga.BestChromosome).Zones;

        debug = "Generation:" + m_ga.GenerationsNumber + " - Fitness:" + fitnessDebug;
        Debug.Log(debug);
#if LOGGER
        //Logger.Log("New generation", Logger.LogType.TITLE);
        Logger.Log(debug);
#endif

        bestFitness = fitnessDebug;
        bestResult = gridResult;
        bestZones = zonesResult;
    }

    public class WFCChromosome : ChromosomeBase
    {
        private readonly int m_numberOfZones;

        //Nbzones equivalent nb genes
        public WFCChromosome(int numberOfZones) : base(numberOfZones)
        {
            m_numberOfZones = numberOfZones;
            CreateGenes();
        }

        //Only id asset is controlled by genetic
        public override Gene GenerateGene(int geneIndex)
        {
            return new Gene(RandomizationProvider.Current.GetInt(0, UniqueTile.getLastId() + 1));
        }

        public override IChromosome CreateNew()
        {
            return new WFCChromosome(m_numberOfZones);
        }

        //Fitness results
        public List<SimpleGridWFC.Zone> Zones { get; internal set; }
        public Module[,] gridResult { get; internal set; }

        public override IChromosome Clone()
        {
            var clone = base.Clone() as WFCChromosome;
            clone.Zones = new List<SimpleGridWFC.Zone>(Zones);
            clone.gridResult = this.gridResult;

            return clone;
        }
    }



    public class WFCFitness : IFitness
    {
        private WFCConfig m_generalConfig;
        private int m_numberOfZones;
        private Vector2Int m_gridSize;
        private bool m_forceSeed;
        private int m_randomSeed;
        private int m_sizeZone;

        public WFCFitness(WFCConfig config, int numberOfZones, int sizeZone, Vector2Int gridSize, bool forceSeed, int randomSeed)
        {
            m_generalConfig = config;
            m_numberOfZones = numberOfZones;
            m_gridSize = gridSize;

            m_forceSeed = forceSeed;
            m_randomSeed = randomSeed;

            m_sizeZone = sizeZone;
        }

        public double Evaluate(IChromosome chromosome)
        {
            var genes = chromosome.GetGenes();

            List<SimpleGridWFC.Zone> Zones = new List<SimpleGridWFC.Zone>(m_numberOfZones);

            for (int i = 0; i < m_numberOfZones; i++)
            {
                Gene g = genes[i];

                SimpleGridWFC.Zone currentZone = new SimpleGridWFC.Zone();

                int originX, originY, sizeX = m_sizeZone, sizeY = m_sizeZone;

                int nbZonesX = Mathf.CeilToInt((float)m_gridSize.x / m_sizeZone);

                originX = (i % nbZonesX) * m_sizeZone;
                originY = (i / nbZonesX) * m_sizeZone;

                float probability = 50000f;

                int assetID = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
                assetID = MathUtility.Clamp(assetID, 0, UniqueTile.getLastId());

                currentZone.origin = new Vector2Int(originX, originY);
                currentZone.size = new Vector2Int(sizeX, sizeY);
                currentZone.probabilityBoost = probability;
                currentZone.assetID = assetID;

                Zones.Add(currentZone);
            }

            ((WFCChromosome)chromosome).Zones = new List<SimpleGridWFC.Zone>(Zones);

            if (m_forceSeed)
            {
                RandomUtility.setLocalSeed(m_randomSeed);
            }

            SimpleGridWFC wfc = new SimpleGridWFC(m_generalConfig);
            wfc.setListZones(Zones);
            wfc.launchWFC();

            if (wfc.isWFCFailed())
                return 0.0f;

            //Je veux une entrée et un drapeau ! 

            //SM_Prop_Fence_02 = entry, id 1
            //id 28 is FlagBlue
            //id 27 is air

            int entryID = 1; //m_generalConfig.hashPrefabToUniqueTile["SM_Prop_Fence_02"];
            int flagObjectiveID = 6;//28
            int airID = 5;//27

            List<int> walkables = new List<int>()
            {
                entryID, flagObjectiveID, airID
            };

            int nbEntry = wfc.GetNbAssetInGrid(entryID);
            int nbFlag = wfc.GetNbAssetInGrid(flagObjectiveID);

            float fitness = 0.0f;

            if (nbEntry > 1)
            {
                fitness += 0.05f;
            }

            if (nbEntry == 1)
            {
                fitness += 0.3f;
            }

            if (nbFlag > 1)
            {
                fitness += 0.05f;
            }

            if (nbFlag == 1)
            {
                fitness += 0.3f;
            }

            Module[,] res = wfc.getModuleResultFiltered();
            ((WFCChromosome)chromosome).gridResult = res;

            if (wfc.GetNbAssetInGrid(entryID) != 1 || wfc.GetNbAssetInGrid(flagObjectiveID) != 1)
            {
                return fitness;
            }

            Vector2Int entryPos = wfc.GetPositionFirst(entryID);
            Vector2Int flagPos = wfc.GetPositionFirst(flagObjectiveID);



            //int[][] map = wfc.getPathGrid();
            //int[] start = new int[2] { entryPos.x, entryPos.y };
            //int[] end = new int[2] { flagPos.x, flagPos.y };

            //List<Vector2> pathToObjective = new Astar(map, start, end, "nope").result;

            // create the tiles map
            bool[,] tilesmap = wfc.getBoolPathGrid(walkables);

            // create a grid
            NesScripts.Controls.PathFind.Grid grid = new NesScripts.Controls.PathFind.Grid(tilesmap);

            // create source and target points
            Point _from = new Point(entryPos.x, entryPos.y);
            Point _to = new Point(flagPos.x, flagPos.y);

            // get path
            // path will either be a list of Points (x, y), or an empty list if no path is found.
            List<Point> path = Pathfinding.FindPath(grid, _from, _to, Pathfinding.DistanceType.Manhattan);

            fitness += path.Count;


            //Filter pass

            int totalMalus = 0;

            for (int i = 0; i < m_generalConfig.gridSize; i++)
            {
                for (int j = 0; j < m_generalConfig.gridSize; j++)
                {
                    if (res[i, j] == null)
                        continue;

                    Module m = res[i, j];

                    int negativeScore = 0;//-4 max

                    if (m.linkedTile.id != airID)//check air cases//or can be a tag directly like walkable
                        continue;

                    //4 sides of 3 cases to check
                    Module left = null, topLeft = null, top = null, topRight = null,
                        right = null, bottomRight = null, bottom = null, bottomLeft = null;

                    //Left
                    if(i - 1 >= 0)
                    {
                        left = res[i - 1, j];
                    }
                    //TopLeft
                    if (i - 1 >= m_generalConfig.gridSize && j + 1 < m_generalConfig.gridSize)
                    {
                        topLeft = res[i - 1, j + 1];
                    }
                    //Top
                    if (j + 1 < m_generalConfig.gridSize)
                    {
                        top = res[i, j + 1];
                    }
                    //TopRight
                    if (i + 1 < m_generalConfig.gridSize && j + 1 < m_generalConfig.gridSize)
                    {
                        topRight = res[i + 1, j + 1];
                    }
                    //Right
                    if (i + 1 < m_generalConfig.gridSize)
                    {
                        right = res[i + 1, j];
                    }
                    //BottomRight
                    if (i + 1 < m_generalConfig.gridSize && j - 1 >= 0)
                    {
                        bottomRight = res[i + 1, j - 1];
                    }
                    //Bottom
                    if (j - 1 >= 0)
                    {
                        bottom = res[i, j - 1];
                    }
                    //BottomLeft
                    if (i - 1 >= 0 && j - 1 >= 0)
                    {
                        bottomLeft = res[i - 1, j - 1];
                    }

                    //a test avec si != type de tag de la case actuelle

                    if(left != null && topLeft != null && top != null)
                    {
                        if (left.linkedTile.id != airID && topLeft.linkedTile.id != airID && top.linkedTile.id != airID)
                            negativeScore--;
                    }

                    if (top != null && topRight != null && right != null)
                    {
                        if (top.linkedTile.id != airID && topRight.linkedTile.id != airID && right.linkedTile.id != airID)
                            negativeScore--;
                    }

                    if (right != null && bottomRight != null && bottom != null)
                    {
                        if (right.linkedTile.id != airID && bottomRight.linkedTile.id != airID && bottom.linkedTile.id != airID)
                            negativeScore--;
                    }

                    if (bottom != null && bottomLeft != null && left != null)
                    {
                        if (bottom.linkedTile.id != airID && bottomLeft.linkedTile.id != airID && left.linkedTile.id != airID)
                            negativeScore--;
                    }

                    totalMalus += negativeScore;
                }
            }

            fitness /= (float) (-totalMalus);

            return fitness;
        }
    }
}
