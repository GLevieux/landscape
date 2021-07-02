


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

public class GASimpleTest : GAScript
{

    //-------------------------

    //Public Parameters
    [Header("Assets Wanted")]
    public List<int> assetsWanted = new List<int>();
    [Header("Zone parameters")]
    public int customNbZones = 10;
    public int zoneSizeLimiter = 3;

    public override string ToString()
    {
        string aw = "";

        foreach (int i in assetsWanted)
        {
            aw += i.ToString() + "\n";
        }

        return base.ToString()
                 + "----" + "\n"
                 + "Custom nb zone is " + customNbZones + "\n"
                 + "Zone size limiter is " + zoneSizeLimiter + "\n"
                 + "* Assets Wanted (count: " + assetsWanted.Count + ") are :\n" + aw + "\n";
    }

    //-------------------------

    public override void init(GAConfig gaConfig, WFCConfig wfcConfig, bool forceSeed = false, int seed = 0)
    {
        base.init(gaConfig, wfcConfig, forceSeed, seed);
        //Override nbZones
        gaConfig.nbZones = customNbZones;
    }

    protected override IFitness getFitnessClass()
    {
        return new WFCFitness(wfcConfig, assetsWanted, zoneSizeLimiter, gaConfig.nbZones, gridSize, forceSeed, randomSeed);
    }

    protected override IChromosome getChromosomeClass()
    {
        return new WFCChromosome(gaConfig.nbZones, gridSize, zoneSizeLimiter);
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
        var assetsWantedCreated = ((WFCChromosome)m_ga.BestChromosome).assetsWantedCreated;
        var nbInstanceAssetsCreated = ((WFCChromosome)m_ga.BestChromosome).nbInstanceAssetsCreated;
        var gridResult = ((WFCChromosome)m_ga.BestChromosome).gridResult;
        var zonesResult = ((WFCChromosome)m_ga.BestChromosome).Zones;

        //Debug.Log($"Generation: {m_ga.GenerationsNumber} - Fitness: ${fitnessDebug} - Assets: ${assetsWantedCreated} - NbAssets: ${nbInstanceAssetsCreated}");
        debug = "Generation:" + m_ga.GenerationsNumber + " - Fitness:" + fitnessDebug + " - Assets:" + assetsWantedCreated + " - NbAssets:" + nbInstanceAssetsCreated;
        Debug.Log(debug);
#if LOGGER
        Logger.Log("New generation", Logger.LogType.TITLE);
        Logger.Log(debug);
#endif


//            for(int i = 0; i < zonesResult.Count; i++)//foreach(var z in zonesResult)
//            {
//                Zone z = zonesResult[i];
//                z.ShowDebug();
//#if LOGGER
//                Logger.Log(i + ": " + z.ToString());
//#endif
//            }



        //if (fitnessDebug > bestFitness)
        {
            lastBestFitness = fitnessDebug;
            lastBestResult = gridResult;
            lastBestZones = zonesResult;
        }

        if (fitnessDebug > bestFitness)
        {
            bestFitness = fitnessDebug;
            bestResult = gridResult;
            bestZones = zonesResult;
        }

    }

    public class WFCChromosome : ChromosomeBase
    {
        private readonly Vector2Int m_gridSize;
        private readonly int m_numberOfZones;
        private readonly int m_zoneSizeLimiter;

        public WFCChromosome(int numberOfZones, Vector2Int gridSize, int zoneSizeLimiter) : base(numberOfZones * 6)//6 values for one zone
        {
            //possibilité de donner des zones de départ
            m_gridSize = gridSize;
            m_numberOfZones = numberOfZones;
            m_zoneSizeLimiter = zoneSizeLimiter;
            CreateGenes();
        }

        public override Gene GenerateGene(int geneIndex)
        {
            switch (geneIndex % 6)//utiliser deterministic random ici aussi ? vu qu'on force seed un random local thread pour le wfc,
                                  //autant laisser ici ce random qui est utilisé de tte facon dans les autres classes de la librairie GA
            {
                //Vector Origin
                case 0:
                    return new Gene(RandomizationProvider.Current.GetInt(0, m_gridSize.x));
                case 1:
                    return new Gene(RandomizationProvider.Current.GetInt(0, m_gridSize.y));
                //case 0:
                //    return new Gene(0);
                //case 1:
                //    return new Gene(0);
                //Vector Size
                case 2:
                    return new Gene(RandomizationProvider.Current.GetInt(0, m_gridSize.x / m_zoneSizeLimiter + 1 ));
                case 3:
                    return new Gene(RandomizationProvider.Current.GetInt(0, m_gridSize.y / m_zoneSizeLimiter + 1 ));
                //case 2:
                //    return new Gene(1);
                //case 3:
                //    return new Gene(1);
                //Probability
                //case 4:
                //    return new Gene(RandomizationProvider.Current.GetFloat(0, 5000));
                case 4:
                    return new Gene(50000);
                //Asset ID
                //case 5:
                //    return new Gene(5);
                case 5:
                    return new Gene(RandomizationProvider.Current.GetInt(0, UniqueTile.getLastId() + 1));

                default:
                    return new Gene(0);
            }
        }

        public override IChromosome CreateNew()
        {
            return new WFCChromosome(m_numberOfZones, m_gridSize, m_zoneSizeLimiter);
        }

        public List<SimpleGridWFC.Zone> Zones { get; internal set; }
        public int assetsWantedCreated { get; internal set; }
        public int nbInstanceAssetsCreated { get; internal set; }
        public Module[,] gridResult { get; internal set; }

        public override IChromosome Clone()
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
        private int m_zoneSizeLimiter;

        public WFCFitness(WFCConfig config, List<int> assetsWanted, int zoneSizeLimiter, int numberOfZones, Vector2Int gridSize, bool forceSeed, int randomSeed)
        {
            generalConfig = config;
            m_numberOfZones = numberOfZones;
            m_assetsWanted = assetsWanted;
            m_zoneSizeLimiter = zoneSizeLimiter;
            m_gridSize = gridSize;

            m_forceSeed = forceSeed;
            m_randomSeed = randomSeed;

        }

        public double Evaluate(IChromosome chromosome)
        {
            var genes = chromosome.GetGenes();

            //Mathf.Ceil()

            List<SimpleGridWFC.Zone> Zones = new List<SimpleGridWFC.Zone>(m_numberOfZones);

            for (int i = 0; i < m_numberOfZones; i++)
            {
                SimpleGridWFC.Zone currentZone = new SimpleGridWFC.Zone();//Zones[i / 6];

                int originX = 0, originY = 0, sizeX = 0, sizeY = 0;
                float probability = 0.0f;
                int assetID = 0;

                //Normal
                for (int j = 6 * i; j < 6 * i + 6; j++)
                {
                    Gene g = genes[j];

                    switch (j % 6)
                    {
                        //Vector Origin
                        case 0:
                            originX = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
                            originX = MathUtility.Clamp(originX, 0, m_gridSize.x);
                            break;
                        case 1:
                            originY = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
                            originY = MathUtility.Clamp(originY, 0, m_gridSize.y);
                            break;
                        //Vector Size
                        case 2:
                            sizeX = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
                            sizeX = MathUtility.Clamp(sizeX, 0, m_gridSize.x / m_zoneSizeLimiter);
                            break;
                        case 3:
                            sizeY = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
                            sizeY = MathUtility.Clamp(sizeY, 0, m_gridSize.y / m_zoneSizeLimiter);
                            break;
                        //Probability
                        case 4:
                            probability = (float)Convert.ToDouble(g.Value, CultureInfo.InvariantCulture);
                            if (probability < 0)
                                probability = 0;
                            break;
                        //Asset ID
                        case 5:
                            assetID = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
                            assetID = MathUtility.Clamp(assetID, 0, UniqueTile.getLastId());
                            break;

                        default:
                            Debug.Log("Unknown gene");
                            break;
                    }
                }

                //Forced random
                //for (int j = 6 * i; j < 6 * i + 6; j++)
                //{

                //    switch (j % 6)
                //    {
                //        //Vector Origin
                //        case 0:
                //            originX = RandomizationProvider.Current.GetInt(0, m_gridSize.x - 1);
                //            break;
                //        case 1:
                //            originY = RandomizationProvider.Current.GetInt(0, m_gridSize.y - 1);
                //            break;
                //        //Vector Size
                //        case 2:
                //            sizeX = RandomizationProvider.Current.GetInt(0, m_gridSize.x / zoneSizeLimiter);
                //            break;
                //        case 3:
                //            sizeY = RandomizationProvider.Current.GetInt(0, m_gridSize.y / zoneSizeLimiter);
                //            break;
                //        //Probability
                //        case 4:
                //            probability = 60000f;
                //            break;
                //        //Asset ID
                //        case 5:
                //            assetID = RandomizationProvider.Current.GetInt(0, UniqueTile.getLastId());
                //            break;

                //        default:
                //            Debug.Log("Unknown gene");
                //            break;
                //    }
                //}

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

            SimpleGridWFC wfc = new SimpleGridWFC(generalConfig);
            wfc.setListZones(Zones);
            wfc.launchWFC();

            if (wfc.isWFCFailed())
                return 0.0f;

            //Fitness v2

            //if (wfc.GetNbAssetInGrid(1) != 1)//1 is fence entry, we want 1 entry
            //{
            //    return 0.0f;
            //}

            float fitness = 0.0f;

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
            ((WFCChromosome)chromosome).gridResult = wfc.getModuleResult(true);

            fitness = (float)nbInstanceAssetsCreated / (m_assetsWanted.Count - assetsWantedCreated + 1);//1.0f - ((float) m_assetsWanted.Count / assetsWantedCreated);

            //nouvelle fitness avec astar

            //int[][] map = wfc.getPathGrid();
            //int[] start = new int[2] { 0, 0 };
            //int[] end = new int[2] { 5, 5 };
            //List<Vector2> path = new Astar(map, start, end, "DiagonalFree").result;

            //19 is center barracks (0,0), we want the maximum of them at the centre of the camp
            //Calculer la distance par rapport au centre de la grille, plus c'est court, le moins de malus
            //une fonction position moyenne des assets du meme type, est-ce que ca revient au meme ??

            //tester si 19 est présent avant ? la c'est 0,0 normalement
            //Vector2 avgPosBarracks = wfc.GetAveragePositionAssetInGrid(5);
            //Vector2 centerGrid = new Vector2((float)(m_gridSize.x - 1) / 2, (float)(m_gridSize.x - 1) / 2);
            //float avgDist = Vector2.Distance(avgPosBarracks, centerGrid);
            //if (avgDist > 1.0f)
            //    fitness /= avgDist;

            //More air => less fitness, 27 is air, we want a maximum assets spawned
            //int nbAir = wfc.GetNbAssetInGrid(27);
            //int nbSlot = m_gridSize.x * m_gridSize.y;
            //float ratioAir = (float) nbAir / nbSlot;
            //ratioAir += 0.3f;//limit the influence of this ratio
            //if(ratioAir < 1)
            //    fitness *= ratioAir;
            //if (nbAir > 0)
            //    fitness *= (float)1.0f / nbAir;

            return fitness;
        }
    }


}




/* // Simple fitness v1 => based on how many asset wanted are spawned
if(m_forceSeed)
{
    RandomUtility.setLocalSeed(m_randomSeed);
}

SimpleGridWFC wfc = new SimpleGridWFC(generalConfig);
wfc.setListZones(Zones);
wfc.launchWFC();

if (wfc.isWFCFailed())
    return 0.0f;

float fitness = 0.0f;

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

fitness = (float) nbInstanceAssetsCreated / (m_assetsWanted.Count - assetsWantedCreated + 1);//1.0f - ((float) m_assetsWanted.Count / assetsWantedCreated);

return fitness;
*/





//    float maxWidth = 998f;
//    float maxHeight = 680f;
//    var chromosome = new FloatingPointChromosome(
//        new double[] { 0, 0, 0, 0 },
//        new double[] { maxWidth, maxHeight, maxWidth, maxHeight },
//        new int[] { 10, 10, 10, 10 },
//        new int[] { 0, 0, 0, 0 });

//    var population = new Population(50, 100, chromosome);

//    var fitness = new FuncFitness((c) =>
//        {
//            var fc = c as FloatingPointChromosome;
//            var values = fc.ToFloatingPoints();
//            var x1 = values[0];
//            var y1 = values[1];
//            var x2 = values[2];
//            var y2 = values[3];
//            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
//        });

//    var selection = new EliteSelection();
//    var crossover = new UniformCrossover(0.5f);
//    var mutation = new FlipBitMutation();
//    var termination = new FitnessStagnationTermination(100);

//    var ga = new GeneticAlgorithm(
//        population,
//        fitness,
//        selection,
//        crossover,
//        mutation);

//    ga.Termination = termination;

//    Console.WriteLine("Generation: (x1, y1), (x2, y2) = distance");

//    var latestFitness = 0.0;


//    ga.GenerationRan += (sender, e) =>
//    {
//        var bestChromosome = ga.BestChromosome as FloatingPointChromosome;
//        var bestFitness = bestChromosome.Fitness.Value;

//        if (bestFitness != latestFitness)
//        {
//            latestFitness = bestFitness;
//            var phenotype = bestChromosome.ToFloatingPoints();
//            Console.WriteLine(
//                "Generation {0,2}: ({1},{2}),({3},{4}) = {5}",
//                ga.GenerationsNumber,
//                phenotype[0],
//                phenotype[1],
//                phenotype[2],
//                phenotype[3],
//                bestFitness
//            );
//        }
//    };

//    ga.Start();


//    Console.ReadKey();
//}

