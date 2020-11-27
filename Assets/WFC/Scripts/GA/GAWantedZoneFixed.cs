//#define LOGGER

using System;
using System.Collections.Generic;
using System.Globalization;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Randomizations;
using UnityEngine;


using static SimpleGridWFC;
using static RelationGrid;
using Debug = UnityEngine.Debug;

public class GAWantedZoneFixed : GAScript
{
    //-------------------------

    //Public Parameters
    [Header("Zone Coverage")]
    public int sizeZone = 3;//used to divide grid into zone of this size
    [Header("Assets Wanted")]
    public List<int> assetsWanted = new List<int>();

    public override string ToString()
    {
        string aw = "";

        foreach (int i in assetsWanted)
        {
            aw += i.ToString() + "\n";
        }

        return base.ToString()
                 + "----" + "\n"
                 + "Zone coverage size is " + sizeZone + "\n"
                 + "* Assets Wanted (count: " + assetsWanted.Count + ") are :\n" + aw + "\n";
    }

    //-------------------------

    public override void init(GAConfig gaConfig, WFCConfig wfcConfig, bool forceSeed = false, int seed = 0)
    {
        base.init(gaConfig, wfcConfig, forceSeed, seed);
        //Override nbZones
        gaConfig.nbZones = Mathf.CeilToInt((float)gridSize.x / sizeZone) * Mathf.CeilToInt((float)gridSize.y / sizeZone);
    }

    protected override IFitness getFitnessClass()//passer gaconfig au final sera plus simple
    {
        return new WFCFitness(wfcConfig, assetsWanted, gaConfig.nbZones, sizeZone, gridSize, forceSeed, randomSeed); ;
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

        WFCChromosome c = ((WFCChromosome)m_ga.BestChromosome);

        Debug.Log("Pop size : " + m_ga.Population.CurrentGeneration.Chromosomes.Count);

        var fitnessDebug = c.Fitness;
        var assetsWantedCreated = c.assetsWantedCreated;
        var nbInstanceAssetsCreated = c.nbInstanceAssetsCreated;
        var gridResult = c.gridResult;
        var zonesResult = c.Zones;

        debug = "Generation:" + m_ga.GenerationsNumber + " - Fitness:" + fitnessDebug + " - Assets:" + assetsWantedCreated + " - NbAssets:" + nbInstanceAssetsCreated;
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
            //GetInt(min inclusive, max exclusive)
            //return new Gene(28);
            return new Gene(RandomizationProvider.Current.GetInt(0, UniqueTile.getLastId() + 1));
        }

        public override IChromosome CreateNew()
        {
            return new WFCChromosome(m_numberOfZones);
        }

        //Fitness results
        public List<SimpleGridWFC.Zone> Zones { get; internal set; }
        public int assetsWantedCreated { get; internal set; }
        public int nbInstanceAssetsCreated { get; internal set; }
        public Module[,] gridResult { get; internal set; }

        public override IChromosome Clone()//useful?
        {
            var clone = base.Clone() as WFCChromosome;
            clone.Zones = new List<SimpleGridWFC.Zone>(Zones);
            clone.assetsWantedCreated = this.assetsWantedCreated;
            clone.nbInstanceAssetsCreated = this.nbInstanceAssetsCreated;
            clone.gridResult = this.gridResult;

            return clone;
        }
    }

    public class WFCFitness : IFitness
    {
        private WFCConfig generalConfig;
        private List<int> m_assetsWanted;
        private int m_numberOfZones;
        private Vector2Int m_gridSize;
        private bool m_forceSeed;
        private int m_randomSeed;
        private int m_sizeZone;

        public WFCFitness(WFCConfig config, List<int> assetsWanted, int numberOfZones, int sizeZone, Vector2Int gridSize, bool forceSeed, int randomSeed)
        {
            generalConfig = config;
            m_numberOfZones = numberOfZones;
            m_assetsWanted = assetsWanted;
            m_gridSize = gridSize;

            m_forceSeed = forceSeed;
            m_randomSeed = randomSeed;

            m_sizeZone = sizeZone;
        }

   

        public double Evaluate(IChromosome chromosome)
        {
            Gene[] genes = chromosome.GetGenes();

            List<SimpleGridWFC.Zone> Zones = new List<SimpleGridWFC.Zone>();

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

            //GUILLAUME : un new de trop ?
            ((WFCChromosome)chromosome).Zones = Zones;//new List<SimpleGridWFC.Zone>(Zones);

            if (m_forceSeed)
            {
                RandomUtility.setLocalSeed(m_randomSeed);
            }

            SimpleGridWFC wfc = new SimpleGridWFC(generalConfig);
            wfc.setListZones(Zones);
            wfc.launchWFC();

            if (wfc.isWFCFailed())
                return 0.0f;

            //Fitness based only on assets wanted
            int assetsWantedCreated = 0;
            int nbInstanceAssetsCreated = 0;
            for (int i = 0; i < m_assetsWanted.Count; i++)
            {
                int res = wfc.GetNbAssetInGrid(m_assetsWanted[i]);
                nbInstanceAssetsCreated += res;
                assetsWantedCreated += (res > 0) ? 1 : 0;
            }

            ((WFCChromosome)chromosome).assetsWantedCreated = assetsWantedCreated;
            ((WFCChromosome)chromosome).nbInstanceAssetsCreated = nbInstanceAssetsCreated;
            ((WFCChromosome)chromosome).gridResult = wfc.getModuleResultFiltered();

            return (float)nbInstanceAssetsCreated / (m_assetsWanted.Count - assetsWantedCreated + 1);
        }
    }


}