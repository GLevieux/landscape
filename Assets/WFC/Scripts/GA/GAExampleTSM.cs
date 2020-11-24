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

public class GeneticAlgo : MonoBehaviour
{
    private GeneticAlgorithm m_ga;
    private Thread m_gaThread;

    public int m_numberOfCities = 20;

    private void Start()
    {
        var fitness = new TspFitness(m_numberOfCities);
        var chromosome = new TspChromosome(m_numberOfCities);

        // This operators are classic genetic algorithm operators that lead to a good solution on TSP,
        // but you can try others combinations and see what result you get.
        var crossover = new OrderedCrossover();
        var mutation = new ReverseSequenceMutation();
        var selection = new RouletteWheelSelection();
        var population = new Population(50, 100, chromosome);

        m_ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
        m_ga.Termination = new TimeEvolvingTermination(System.TimeSpan.FromHours(1));

        // The fitness evaluation of whole population will be running on parallel.
        m_ga.TaskExecutor = new ParallelTaskExecutor
        {
            MinThreads = 100,
            MaxThreads = 200
        };

        // Everty time a generation ends, we log the best solution.
        m_ga.GenerationRan += delegate
        {
            var distance = ((TspChromosome)m_ga.BestChromosome).Distance;
            Debug.Log($"Generation: {m_ga.GenerationsNumber} - Distance: ${distance}");
        };

        // Starts the genetic algorithm in a separate thread.
        m_gaThread = new Thread(() => m_ga.Start());
        m_gaThread.Start();
    }

    private void OnDestroy()
    {
        // When the script is destroyed we stop the genetic algorithm and abort its thread too.
        m_ga.Stop();
        m_gaThread.Abort();
    }
}

public class TspCity
{
    public Vector2 Position { get; set; }
}

public class TspChromosome : ChromosomeBase
{
    private readonly int m_numberOfCities;
    public TspChromosome(int numberOfCities) : base(numberOfCities)
    {
        m_numberOfCities = numberOfCities;
        var citiesIndexes = RandomizationProvider.Current.GetUniqueInts(numberOfCities, 0, numberOfCities);
        for (int i = 0; i < numberOfCities; i++)
        {
            ReplaceGene(i, new Gene(citiesIndexes[i]));
        }
    }
    
    public double Distance { get; internal set; }
    
    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(RandomizationProvider.Current.GetInt(0, m_numberOfCities));
    }

    public override IChromosome CreateNew()
    {
        return new TspChromosome(m_numberOfCities);
    }

    public override IChromosome Clone()
    {
        var clone = base.Clone() as TspChromosome;
        clone.Distance = Distance;
        return clone;
    }
}

public class TspFitness : IFitness
{
    private Rect m_area;

    public TspFitness(int numberOfCities)
    {
        Cities = new List<TspCity>(numberOfCities);

        var size = 100f;
        m_area = new Rect(-size, -size, size * 2, size * 2);

        for (int i = 0; i < numberOfCities; i++)
        {
            var city = new TspCity { Position = GetCityRandomPosition() };
            Cities.Add(city);
        }
    }

    public IList<TspCity> Cities { get; private set; }

    public double Evaluate(IChromosome chromosome)
    {
        var genes = chromosome.GetGenes();
        var distanceSum = 0.0;
        var lastCityIndex = Convert.ToInt32(genes[0].Value, CultureInfo.InvariantCulture);
        var citiesIndexes = new List<int>();
        citiesIndexes.Add(lastCityIndex);

        // Calculates the total route distance.
        foreach (var g in genes)
        {
            var currentCityIndex = Convert.ToInt32(g.Value, CultureInfo.InvariantCulture);
            distanceSum += CalcDistanceTwoCities(Cities[currentCityIndex], Cities[lastCityIndex]);
            lastCityIndex = currentCityIndex;

            citiesIndexes.Add(lastCityIndex);
        }

        distanceSum += CalcDistanceTwoCities(Cities[citiesIndexes.Last()], Cities[citiesIndexes.First()]);

        var fitness = 1.0 - (distanceSum / (Cities.Count * 1000.0));

        ((TspChromosome)chromosome).Distance = distanceSum;

        // There is repeated cities on the indexes?
        var diff = Cities.Count - citiesIndexes.Distinct().Count();

        if (diff > 0)
        {
            fitness /= diff;
        }

        if (fitness < 0)
        {
            fitness = 0;
        }

        return fitness;
    }

    private Vector2 GetCityRandomPosition()
    {
        return new Vector2(
            RandomizationProvider.Current.GetFloat(m_area.xMin, m_area.xMax + 1),
            RandomizationProvider.Current.GetFloat(m_area.yMin, m_area.yMax + 1));
    }

    private static double CalcDistanceTwoCities(TspCity one, TspCity two)
    {
        return Vector2.Distance(one.Position, two.Position);
    }
}