using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using NeatGameAI.Games.Base;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

namespace NeatGameAI.Games.Evolution
{
    public class GameExperiment
    {
        NeatEvolutionAlgorithmParameters algoParams;
        NeatGenomeParameters neatGenomeParams;
        string name;
        int populationSize;
        int specieCount;
        NetworkActivationScheme activationScheme;
        string complexityRegulationStr;
        int? complexityThreshold;
        ParallelOptions parallelOptions;
        IGame game;

        public IPhenomeEvaluator<IBlackBox> PhenomeEvaluator { get => new GameEvaluator(game); }

        public int InputCount { get => game.NeuralInputsCount; }

        public int OutputCount { get => game.NeuralOutputsCount; }

        /// <summary>
        /// Defines whether all networks should be evaluated every generation, or only new (child) networks.
        /// </summary>
        public bool EvaluateParents { get => game.HasRandomEvents; }

        public string Name { get => name; }

        public int DefaultPopulationSize { get => populationSize; }

        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters { get => algoParams; }

        public NeatGenomeParameters NeatGenomeParameters { get => neatGenomeParams; }

        public GameExperiment(IGame game)
        {
            this.game = game;
        }

        public void Initialize(string name)
        {
            this.name = name;
            populationSize = 150;
            specieCount = 10;
            activationScheme = NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(2);
            complexityRegulationStr = "Absolute";
            complexityThreshold = 50;
            parallelOptions = new ParallelOptions();

            algoParams = new NeatEvolutionAlgorithmParameters();
            algoParams.SpecieCount = specieCount;
            neatGenomeParams = new NeatGenomeParameters();
        }

        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, new NeatGenomeFactory(InputCount, OutputCount, neatGenomeParams));
        }

        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, false);
        }

        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new NeatGenomeFactory(InputCount, OutputCount, neatGenomeParams);
        }

        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }

        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome2 factory with our neat genome2 parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Create evolution algorithm.
            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }

        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weight difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);

            // Create speciation strategy.
            ISpeciationStrategy<NeatGenome> speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, parallelOptions);

            // Create complexity regulation strategy.
            IComplexityRegulationStrategy complexityRegulationStrategy;

            if (!Enum.TryParse<ComplexityCeilingType>(complexityRegulationStr, out ComplexityCeilingType ceilingType))
                complexityRegulationStrategy = new NullComplexityRegulationStrategy();
            else
                complexityRegulationStrategy = new DefaultComplexityRegulationStrategy(ceilingType, complexityThreshold.Value);

            // Create the evolution algorithm.
            NeatEvolutionAlgorithm <NeatGenome> neatAlgo = new NeatEvolutionAlgorithm<NeatGenome>(algoParams, speciationStrategy, complexityRegulationStrategy);

            // Create genome2 decoder.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = new NeatGenomeDecoder(activationScheme);

            // Create a genome2 list evaluator. This packages up the genome2 decoder with the genome2 evaluator.
            IGenomeListEvaluator<NeatGenome> genomeListEvaluator = new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, PhenomeEvaluator, parallelOptions);

            // Wrap the list evaluator in a 'selective' evaluator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
            // that were in the population in previous generations (elite genomes). This is determined by examining each genome2's evaluation info object.
            if (!EvaluateParents)
                genomeListEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(genomeListEvaluator,
                                         SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());

            // Initialize the evolution algorithm.
            neatAlgo.Initialize(genomeListEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return neatAlgo;
        }

        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new NeatGenomeDecoder(activationScheme);
        }
    }
}
