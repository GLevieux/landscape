#define LOGGER


using System;
using System.Collections.Generic;
using System.Threading;
using GeneticSharp.Domain;
using static SimpleGridWFC;
using System.Diagnostics;
using UnityEngine;
using static RelationGrid;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using Debug = UnityEngine.Debug;
using System.Collections;

public abstract class GAScript : MonoBehaviour
{
    protected WFCConfig wfcConfig;
    protected GAConfig gaConfig;

    protected GeneticAlgorithm m_ga;
    protected Thread m_gaThread;

    protected int randomSeed = 0;
    protected bool forceSeed = false;

    protected Stopwatch stopwatch;

    protected ManualResetEvent pauseThread = new ManualResetEvent(true);

    [System.Serializable]
    public class GAConfig
    {
        //General parameters
        public int populationMin = 40;
        public int populationMax = 60;
        public int nbGeneration = 1000;
        public float crossSelectProbability = 0.7f;
        public float crossProbability = 0.5f;
        public float mutationProbability = 0.03f;
        public bool allGenesMutable = false;
        public bool runInSeparateThread = true;

        public enum RunType
        {
            FULL_PARALLEL,
            CAN_STILL_PLAY_PARALLEL,
            SEQUENTIAL
        }
        public RunType runType = RunType.CAN_STILL_PLAY_PARALLEL;

        [HideInInspector]
        public int gridUnitSize = 1;

        [Header("Debug")]
        [ReadOnly]
        public int nbZones = 0;//override by subclass

        //override
        public string ToString(string className = "")
        {
            return "Genetic Algorithm: " + className + "\n"
                    + "Population is (" + populationMin + ", " + populationMax + ") \n"
                    + "nbGeneration is " + nbGeneration + "\n"
                    + "crossSeleectProbability is " + crossSelectProbability + "\n"
                    + "crossProbability is " + crossProbability + "\n"
                    + "mutationProbability is " + mutationProbability + "\n"
                    + "all genes mutable is " + allGenesMutable + "\n"
                    + "nbZones is " + nbZones + "\n";
        }
    }

    public override string ToString()
    {
        return gaConfig.ToString(getNameClass());
    }

    protected Vector2Int gridSize;

    public virtual void init(GAConfig gaConfig, WFCConfig wfcConfig, bool forceSeed, int seed)
    {
        bestFitness = -float.MaxValue;

        if (wfcConfig == null)
            this.wfcConfig = new WFCConfig();
        else
            this.wfcConfig = wfcConfig;

        if (gaConfig == null)
            this.gaConfig = new GAConfig();
        else
            this.gaConfig = gaConfig;

        this.forceSeed = forceSeed;
        this.randomSeed = seed;

        stopwatch = new Stopwatch();

        //Temp
        gridSize = new Vector2Int(wfcConfig.gridSize, wfcConfig.gridSize);//Vector2Int gridSize a transferer dans le wfcconfig (verif si l'algo marche dans ce cas) => permettrait d'avoir des rectangles
    }

    public TimeSpan getElapsedTime()
    {
        return stopwatch.Elapsed;
    }

    public void StopGA()
    {
        Debug.Log("Stop GA Asked");
        if (m_ga == null)
            return;

        // When the script is destroyed we stop the genetic algorithm and abort its thread too.
        m_ga.Stop();
        if(m_gaThread != null)
            m_gaThread.Abort();

        gaEnded = true;
    }

    public void PauseGA(bool pause)
    {
        if (m_ga == null)
            return;        

        if (pause)
        {
            pauseThread.Reset();
        }            
        else
        {
            pauseThread.Set();
        }
            
    }

    public bool isRunning()
    {
        return m_ga.IsRunning;
    }

    //Last best result (last generation iteration)
    protected Module[,] lastBestResult = null;
    protected double? lastBestFitness = 0;
    protected List<Zone> lastBestZones;

    //Cest result of all time (last generation iteration)
    protected Module[,] bestResult = null;
    protected double? bestFitness = 0;
    protected List<Zone> bestZones;

    public Module[,] getResult()
    {
        return bestResult;
    }

    //Debug string (for build)
    public string debug { get; protected set; }

    [HideInInspector]
    public bool gaEnded = false;

    public List<Zone> getZonesResult()
    {
        return bestZones;
    }


    private List<GameObject> tileInstanciated = new List<GameObject>();
    
    public void ShowResult(Vector3 instanceCoordinates, bool showOnlyLastBest)
    {
        
        Module[,] res = bestResult;
        if (showOnlyLastBest)
            res = lastBestResult;

        if (res == null)
            return;

        foreach (GameObject g in tileInstanciated)
        {
            GameObject.Destroy(g);
        }

        for (int i = 0; i < wfcConfig.gridSize; i++)
        {
            for (int j = 0; j < wfcConfig.gridSize; j++)
            {
                if (res[i, j] == null)
                    continue;


                Module m = res[i, j];
                UniqueTile ut = m.linkedTile;
                PrefabInstance pi = ut.pi;

                if (ut.parent != null && !ut.parent.subpartPos[ut.id].Equals(Vector3Int.zero))//prevent spawing multiple assets for one bigtile
                {
                    continue;
                }

                if (ut.pi.doNotShowInCreatedLevel)
                {
                    continue;
                }

                GameObject go = GameObject.Instantiate(pi.prefab,
                                                    new Vector3(instanceCoordinates.x + i * gaConfig.gridUnitSize + (float)gaConfig.gridUnitSize / 2, 0, instanceCoordinates.z + j * gaConfig.gridUnitSize + (float)gaConfig.gridUnitSize / 2),
                                                    Quaternion.Euler(0f, 90f * m.rotationY, 0f));
                go.transform.SetParent(this.transform);
                tileInstanciated.Add(go);


                //yield return new WaitForEndOfFrame();

            }
        }

        //yield return null;
    }

    protected abstract IFitness getFitnessClass();
    protected abstract IChromosome getChromosomeClass();
    protected abstract void generationRan();
    protected abstract string getNameClass();

    public void launchGA()
    {
        var fitness = getFitnessClass();
        var chromosome = getChromosomeClass();

        // This operators are classic genetic algorithm operators that lead to a good solution on TSP,
        // but you can try others combinations and see what result you get.
        var crossover = new MyTwoPointCrossover(0,1, gaConfig.crossProbability); //Les deux premiers params sont ensuite remplacés, inutiles dans le constructeur...
        var mutation = new MyUniformMutation(gaConfig.allGenesMutable);
        var selection = new EliteSelection();
        
        var population = new Population(gaConfig.populationMin, gaConfig.populationMax, chromosome);

        gaConfig.gridUnitSize = wfcConfig.gridUnitSize;

        m_ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
        m_ga.Termination = new GenerationNumberTermination(gaConfig.nbGeneration);//new TimeEvolvingTermination(System.TimeSpan.FromMinutes(10));

        m_ga.CrossoverProbability = gaConfig.crossSelectProbability;
        m_ga.MutationProbability = gaConfig.mutationProbability;
        


        switch (gaConfig.runType)
        {
            case GAConfig.RunType.FULL_PARALLEL:
                m_ga.TaskExecutor = new ParallelTaskExecutor//The fitness evaluation of whole population will be running on parallel.
                {
                    MinThreads = Environment.ProcessorCount,
                    MaxThreads = Environment.ProcessorCount*2
                };
                break;
            case GAConfig.RunType.CAN_STILL_PLAY_PARALLEL:
                m_ga.TaskExecutor = new MyParallelTaskExecutor//The fitness evaluation of whole population will be running on parallel.
                {
                    MinThreads = Environment.ProcessorCount/2,
                    MaxThreads = Environment.ProcessorCount/2
                };
                break;
            case GAConfig.RunType.SEQUENTIAL:
                m_ga.TaskExecutor = new LinearTaskExecutor();
                break;
            default:
                m_ga.TaskExecutor = new LinearTaskExecutor();
                break;
        }
       


#if LOGGER
        Logger.Log("Start GA", Logger.LogType.TITLE);
        Logger.Log("WFC Config", Logger.LogType.TITLE);
        Logger.Log(wfcConfig.ToString());
        Logger.Log("GA Config", Logger.LogType.TITLE);
        Logger.Log(this.ToString());
        Logger.Log("GA Generations", Logger.LogType.TITLE);
#endif

        // Everty time a generation ends, we log the best solution.
        m_ga.GenerationRan += delegate
        {
            pauseThread.WaitOne();
            generationRan();            
        };

        m_ga.Stopped += delegate
        {
            Debug.Log("stopped ga");
        };

        m_ga.TerminationReached += delegate
        {
            Debug.Log("ended ga");

            gaEnded = true;

            // Stop timing.
            stopwatch.Stop();

            //Log last zones
            for (int i = 0; i < bestZones.Count; i++)
            {
                Zone z = bestZones[i];
                z.ShowDebug();
#if LOGGER
                Logger.Log(i + ": " + z.ToString());
#endif
            }


            Debug.Log("Genetic WFC, NbGeneration:" + gaConfig.nbGeneration + " => Time elapsed: " + stopwatch.Elapsed);
#if LOGGER
            Logger.Log("Genetic WFC, NbGeneration:" + gaConfig.nbGeneration + " => Time elapsed: " + stopwatch.Elapsed);
#endif
        };

        if (gaConfig.runInSeparateThread)
        {
            // Starts the genetic algorithm in a separate thread.
            m_gaThread = new Thread(() => m_ga.Start());
            
            // Start stopwatch.
            stopwatch.Start();

            m_gaThread.Start();
        }
        else
        {
            // Start stopwatch.
            stopwatch.Start();
            m_ga.Start();
        }
        

        

        
    }

    public int getGenerationNumber()
    {
        return m_ga.GenerationsNumber;
    }
}
