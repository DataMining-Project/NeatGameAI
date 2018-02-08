using System;
using System.Collections.Generic;
using System.Linq;

namespace NeatGameAI.Neat
{
    public class Population
    {
        private static Random random = new Random();

        public NeatConfig Config { get; set; }
        public List<Genome> Genomes { get; set; }
        public int Generation { get; set; }
        public double TopFitness { get; set; }
        public  FitnessEvaluator FitnessEvaluator { get; set; }

        public Population(NeatConfig config, FitnessEvaluator fitnessEvaluator)
        {
            Config = config;
            FitnessEvaluator = fitnessEvaluator;
            Generation = 0;
            TopFitness = 0;
        }

        public void InitializePopulation()
        {
            for (int i = 0; i < Config.PopulationSize; i++)
            {
                Genomes.Add(new Genome(Config));
            }
        }

        public void BreedNextGeneration()
        {
            var newPopulation = new List<Genome>();

            // Add elites to the new population
            int elitesCount = (int)(Genomes.Count * Config.ElitismPercentange);
            for (int i = 0; i < elitesCount; i++)
            {
                newPopulation.Add(Genomes[i]);
            }

            // Breed children
            for (int i = 0; i < Genomes.Count; i++)
            {
                int p1Index = 0, p2Index = 0;
                while (p1Index == p2Index)
                {
                    p1Index = random.Next(0, Genomes.Count);
                    p2Index = random.Next(0, Genomes.Count);
                }

                var child = Genome.Crossover(Genomes[p1Index], Genomes[p2Index]);
                child.Mutate();
                newPopulation.Add(child);
            }

            // Evaluate new generation Fitness
            foreach (var genome in Genomes)
            {
                FitnessEvaluator(genome);
            }

            // Sort new popilation and delete weaker genomes
            newPopulation.Sort((x, y) => y.Fitness.CompareTo(x.Fitness));
            Genomes = newPopulation.Take(Config.PopulationSize).ToList();

            TopFitness = Genomes[0].Fitness;

            Generation++;
        }
    }
}
