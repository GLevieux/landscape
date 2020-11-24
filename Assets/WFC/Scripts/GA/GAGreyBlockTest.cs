


//#define LOGGER

//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using GeneticSharp.Domain.Chromosomes;
//using GeneticSharp.Domain.Fitnesses;
//using GeneticSharp.Domain.Randomizations;
//using UnityEngine;


//using static SimpleGridWFC;
//using static RelationGrid;
//using Debug = UnityEngine.Debug;
//using System.Security.AccessControl;

//public class GAGreyBlockTest : GAScript
//{
//    public int sizeZone = 2;

//    public GAGreyBlockTest(GAConfig gaConfig, WFCConfig wfcConfig, bool forceSeed = false, int seed = 0) : base(gaConfig, wfcConfig, forceSeed, seed)
//    {
//        //Override nbZones
//        gaConfig.nbZones = Mathf.CeilToInt((float)gridSize.x / sizeZone) * Mathf.CeilToInt((float)gridSize.y / sizeZone);

//        //a test avec size 1
//    }

//    protected override IFitness getFitnessClass()
//    {
//        return new WFCFitness(wfcConfig, gaConfig.nbZones, sizeZone, gridSize, forceSeed, randomSeed);
//    }

//    protected override IChromosome getChromosomeClass()
//    {
//        return new WFCChromosome(gaConfig.nbZones, wfcConfig.uniqueTilesInGrid.Count, wfcConfig.uniqueGreyBlockInGrid.Count);
//    }

//    protected override string getNameClass()
//    {
//        return this.GetType().Name;
//    }

//    protected override void generationRan()
//    {
//#if LOGGER
//        //Log to csv all fitness
//        IList<IChromosome> tempChro = m_ga.Population.CurrentGeneration.Chromosomes;
//        for (int i = 0; i < tempChro.Count; i++)
//        {
//            Logger.CSV("G" + m_ga.GenerationsNumber.ToString(), tempChro[i].Fitness.ToString());
//        }
//#endif

//        Debug.Log("----------------------------------------------------------------------");
//        var fitnessDebug = ((WFCChromosome)m_ga.BestChromosome).Fitness;
//        var gridResult = ((WFCChromosome)m_ga.BestChromosome).gridResult;
//        var zonesResult = ((WFCChromosome)m_ga.BestChromosome).Zones;

//        debug = "Generation:" + m_ga.GenerationsNumber + " - Fitness:" + fitnessDebug;
//        Debug.Log(debug);
//#if LOGGER
//        Logger.Log(debug);
//#endif

//        bestFitness = fitnessDebug;
//        bestResult = gridResult;
//        bestZones = zonesResult;
//    }

//    public class WFCChromosome : ChromosomeBase
//    {
//        private readonly int m_numberOfZones;
//        private readonly int m_numberOfAssets;
//        private readonly int m_numberOfGreyblocks;

//        //Nbzones equivalent nb genes
//        public WFCChromosome(int numberOfZones, int numberOfAssets, int numberOfGreyblocks) : base(numberOfZones)
//        {
//            m_numberOfZones = numberOfZones;
//            m_numberOfAssets = numberOfAssets;//useless
//            m_numberOfGreyblocks = numberOfGreyblocks;

//            CreateGenes();
//        }

//        //Only id asset is controlled by genetic
//        public override Gene GenerateGene(int geneIndex)
//        {
//            return new Gene(RandomizationProvider.Current.GetInt(0, m_numberOfGreyblocks));
//        }

//        public override IChromosome CreateNew()
//        {
//            return new WFCChromosome(m_numberOfZones,m_numberOfAssets, m_numberOfGreyblocks);
//        }

//        //Fitness results
//        public List<SimpleGridWFC.Zone> Zones { get; internal set; }
//        public Module[,] gridResult { get; internal set; }

//        public override IChromosome Clone()//useful?
//        {
//            var clone = base.Clone() as WFCChromosome;
//            clone.Zones = new List<SimpleGridWFC.Zone>(Zones);
//            clone.gridResult = this.gridResult;

//            return clone;
//        }
//    }

//    public class WFCFitness : IFitness
//    {
//        private WFCConfig m_generalConfig;
//        private int m_numberOfZones;
//        private Vector2Int m_gridSize;
//        private bool m_forceSeed;
//        private int m_randomSeed;
//        private int m_sizeZone;

//        public WFCFitness(WFCConfig config, int numberOfZones, int sizeZone, Vector2Int gridSize, bool forceSeed, int randomSeed)
//        {
//            m_generalConfig = config;
//            m_numberOfZones = numberOfZones;
//            m_gridSize = gridSize;

//            m_forceSeed = forceSeed;
//            m_randomSeed = randomSeed;

//            m_sizeZone = sizeZone;
//        }

//        public double Evaluate(IChromosome chromosome)
//        {
//            var genes = chromosome.GetGenes();

//            List<SimpleGridWFC.Zone> Zones = new List<SimpleGridWFC.Zone>(m_numberOfZones);

//            for (int i = 0; i < m_numberOfZones; i++)
//            {
//                Gene g = genes[i];

//                SimpleGridWFC.Zone currentZone = new SimpleGridWFC.Zone();

//                int originX, originY, sizeX = m_sizeZone, sizeY = m_sizeZone;

//                int nbZonesX = Mathf.CeilToInt((float)m_gridSize.x / m_sizeZone);

//                originX = (i % nbZonesX) * m_sizeZone;
//                originY = (i / nbZonesX) * m_sizeZone;

//                float probability = 50000f;

//                int assetID = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
//                assetID = MathUtility.Clamp(assetID, 0, UniqueTile.getLastId());

//                currentZone.origin = new Vector2Int(originX, originY);
//                currentZone.size = new Vector2Int(sizeX, sizeY);
//                currentZone.probabilityBoost = probability;
//                currentZone.assetID = assetID;

//                Zones.Add(currentZone);
//            }

//            ((WFCChromosome)chromosome).Zones = new List<SimpleGridWFC.Zone>(Zones);

//            if (m_forceSeed)
//            {
//                RandomUtility.setLocalSeed(m_randomSeed);
//            }

//            List<UniqueTile> listGreyBlock = m_generalConfig.uniqueGreyBlockInGrid;
//            List<UniqueTile> listNormalAssets = m_generalConfig.uniqueTilesInGrid;

//            //First pass
//            m_generalConfig.uniqueTilesInGrid = listGreyBlock;/// Attention ne marchera qu'en single thread car generalConfig est partagé en référence
//            m_generalConfig.listInitialTags = new List<ForceTag>();

//            SimpleGridWFC wfc = new SimpleGridWFC(m_generalConfig);
//            wfc.setListZones(Zones);
//            wfc.launchWFC();

//            if (wfc.isWFCFailed())
//                return 0.0f;

//            //Second pass
//            List<ForceTag> tags = wfc.getTags();

//            m_generalConfig.listInitialTags = tags;
//            m_generalConfig.useForceTag = true;
//            m_generalConfig.uniqueTilesInGrid = listNormalAssets;

//            wfc.setListZones(new List<Zone>());
//            wfc.launchWFC();

//            if (wfc.isWFCFailed())
//                return 0.0f;

//            Module[,] res = wfc.getModuleResult();
//            ((WFCChromosome)chromosome).gridResult = res;

//            float fitness = 0.0f;

//            List<string> moduleChecked = new List<string>();

//            for(int i = 0; i < m_gridSize.x; i++)
//            {
//                for (int j = 0; j < m_gridSize.y; j++)
//                {
//                    int neighBoorCount = 0;

//                    Module m = res[i, j];
//                    Module mRight, mLeft, mTop, mBottom;

//                    if(m.linkedTile.pi.prefabTag != PrefabInstance.PrefabTag.OneLevelBlock)
//                    {
//                        continue;
//                    }

//                    if (i + 1 < m_gridSize.x)
//                    {
//                        mRight = res[i + 1, j];

//                        if (!moduleChecked.Contains(((i + 1) + "," + j).ToString()) && mRight.linkedTile.pi.prefabTag == m.linkedTile.pi.prefabTag)
//                        {
//                            neighBoorCount++;
//                        }
//                    }

//                    if (i - 1 > 0)
//                    {
//                        mLeft = res[i - 1, j];

//                        if (!moduleChecked.Contains(((i - 1) + "," + j).ToString()) && mLeft.linkedTile.pi.prefabTag == m.linkedTile.pi.prefabTag)
//                        {
//                            neighBoorCount++;
//                        }
//                    }

//                    if (j + 1 < m_gridSize.y)
//                    {
//                        mTop = res[i, j + 1];

//                        if (!moduleChecked.Contains((i + "," + (j + 1)).ToString()) && mTop.linkedTile.pi.prefabTag == m.linkedTile.pi.prefabTag)
//                        {
//                            neighBoorCount++;
//                        }
//                    }

//                    if (j - 1 > 0)
//                    {
//                        mBottom = res[i, j - 1];

//                        if (!moduleChecked.Contains((i + "," + (j - 1)).ToString()) && mBottom.linkedTile.pi.prefabTag == m.linkedTile.pi.prefabTag)
//                        {
//                            neighBoorCount++;
//                        }
//                    }

//                    moduleChecked.Add(i + "," + j);

//                    fitness += neighBoorCount * 1.0f;
//                }
//            }

//            return fitness;//wfc.GetNbAssetInGrid(5);
//        }
//    }


//}