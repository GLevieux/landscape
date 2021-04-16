﻿#define LOGGER


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

public class GATestFlow : GAScript
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
        return new WFCFitness(wfcConfig, gaConfig.nbZones, sizeZone, gridSize, forceSeed, randomSeed, IdPlayerStart, typeAgent, configAgent);
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

        //On calcule, pour chaque zone de la map(chaque gene) l'entropie
        Gene[] genes = tempChro[0].GetGenes();
        float[] counts = new float[UniqueTile.getLastId() + 1];
        float[] entropies = new float[genes.Length];

        for (int g = 0; g < genes.Length; g++)
        {
            for (int i = 0; i < counts.Length; i++)
                counts[i] = 0;

            for (int c = 0; c < tempChro.Count; c++)
            {
                int assetID = Convert.ToInt32(tempChro[c].GetGene(g).Value, CultureInfo.InvariantCulture);
                counts[assetID / 4]++;  //CHANGEMENT ASSET ROTATION
            }

            for (int i = 0; i < counts.Length; i++)
                counts[i] /= (float)tempChro.Count;

            entropies[g] = 0;
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > float.Epsilon)
                    entropies[g] += -(float)(counts[i] * Math.Log(counts[i], 2));

            Logger.CSVEntropyZone("G" + m_ga.GenerationsNumber.ToString(), entropies[g].ToString());
        }

        //On calcule, pour chaque module
        for (int g = 0; g < genes.Length; g++)
        {
            for (int i = 0; i < counts.Length; i++)
                counts[i] = 0;

            for (int c = 0; c < tempChro.Count; c++)
            {
                Module[,] modules = ((WFCChromosome)tempChro[c]).gridResult;
                if (modules != null)
                {
                    int gridSize = modules.GetUpperBound(0) + 1;
                    if (modules[g % gridSize, g / gridSize] != null)
                    {
                        int assetID = Convert.ToInt32(modules[g % gridSize, g / gridSize].linkedTile.id, CultureInfo.InvariantCulture);
                        counts[assetID]++;
                    }
                }
            }

            for (int i = 0; i < counts.Length; i++)
                counts[i] /= (float)tempChro.Count;

            entropies[g] = 0;
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > float.Epsilon)
                    entropies[g] += -(float)(counts[i] * Math.Log(counts[i], 2));

            Logger.CSVEntropyModule("G" + m_ga.GenerationsNumber.ToString(), entropies[g].ToString());
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
            return new Gene(RandomizationProvider.Current.GetInt(0, (UniqueTile.getLastId() + 1) * 4));
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

    public enum TypeAgent
    {
        AGENT_FLOW_CURIEUX,
        AGENT_TOTO
    };
    public TypeAgent typeAgent;

    [Tooltip("Attention au type de config, doit etre compatible avec l'agent")]
    public ScriptableObject configAgent;

    public int IdPlayerStart = -1;



    public class WFCFitness : IFitness
    {
        private WFCConfig m_generalConfig;
        private int m_numberOfZones;
        private Vector2Int m_gridSize;
        private bool m_forceSeed;
        private int m_randomSeed;
        private int m_sizeZone;
        private int idPlayerStart = -1;

        private ScriptableObject configAgent;
        private TypeAgent typeAgent;

        public WFCFitness(WFCConfig config, int numberOfZones, int sizeZone, Vector2Int gridSize, bool forceSeed, int randomSeed, int IdPlayerStart, TypeAgent typeAgent,
             ScriptableObject configAgent)
        {
            m_generalConfig = config;
            m_numberOfZones = numberOfZones;
            m_gridSize = gridSize;

            m_forceSeed = forceSeed;
            m_randomSeed = randomSeed;

            m_sizeZone = sizeZone;

            idPlayerStart = IdPlayerStart;

            this.configAgent = configAgent;
            this.typeAgent = typeAgent;

        }

        /*public double Evaluate(IChromosome chromosome)
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
                assetID = MathUtility.Clamp(assetID, 0, UniqueTile.getLastId() * 4);
                int rotationY = assetID % 4;
                assetID = assetID / 4;

                currentZone.origin = new Vector2Int(originX, originY);
                currentZone.size = new Vector2Int(sizeX, sizeY);
                currentZone.probabilityBoost = probability;
                currentZone.assetID = assetID;
                currentZone.rotationY = rotationY;

                Zones.Add(currentZone);
            }

            ((WFCChromosome)chromosome).Zones = Zones;//new List<SimpleGridWFC.Zone>(Zones);

            if (m_forceSeed)
            {
                RandomUtility.setLocalSeed(m_randomSeed);
            }

            SimpleGridWFC wfc = new SimpleGridWFC(m_generalConfig);
            wfc.setListZones(Zones);
            wfc.launchWFC();
            bool error = wfc.checkWFCForError();

            if (wfc.isWFCFailed() || error)
                return -float.MaxValue;

            //Sauve le résultat
            Module[,] modules = wfc.getModuleResult(true);
            ((WFCChromosome)chromosome).gridResult = modules;

            NavGrid nav = new NavGrid();
            nav.Build(modules);

            //On estime le flow de la grille

            //On trouve le point de départ
            int gridSize = modules.GetUpperBound(0) + 1;
            int xStart = -1;
            int zStart = -1;
            int dirStart = 0;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    if (modules[x, z] != null && modules[x, z].linkedTile.id == idPlayerStart)
                    {
                        xStart = x;
                        zStart = z;
                        dirStart = modules[x, z].rotationY;
                        break;
                    }
                }
            }

            //Pas de start, c'est nul
            if (xStart < 0)
            {
                return -float.MaxValue;
            }

            //On part du départ et on voit comme ca avance tout droit
            float fitness = 0.0f;

            LndAgent agent = null;
            switch (typeAgent)
            {
                case TypeAgent.AGENT_FLOW_CURIEUX:
                    agent = new AgentFlowCurieux();
                    break;
                case TypeAgent.AGENT_TOTO:
                    agent = new AgentToto();
                    break;
            }

            agent.Init(xStart, zStart, 0, dirStart, nav, m_generalConfig.gridUnitSize);
            agent.InitParams(configAgent);

            for (int i = 0; i < 30000; i++)
                fitness += agent.Step() / 30000.0f;


            //if (fitness > -float.Epsilon && fitness < float.Epsilon)
            //{
            //    Debug.Log("What ?");

            //    agent.Init(xStart, zStart, 0, dirStart, nav, m_generalConfig.gridUnitSize);
            //    agent.InitParams(configAgent);

            //    for (int i = 0; i < 30000; i++)
            //        fitness += agent.Step() / 30000.0f;

            //}

            return fitness;
        }*/


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

            int entryID = 1;
            int flagObjectiveID = 0;//28
            int airID = 5;//27
            int ennemySpawnId = 2;

            List<int> walkables = new List<int>()
            {
                entryID, flagObjectiveID, airID, ennemySpawnId
            };

            int nbEntry = wfc.GetNbAssetInGrid(entryID);
            int nbFlag = wfc.GetNbAssetInGrid(flagObjectiveID);

            float fitness = 0.0f;

            /*if (nbEntry > 1)
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
            }*/

             ((WFCChromosome)chromosome).gridResult = wfc.getModuleResult(true);

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


            return path.Count;
        }
    }

}


