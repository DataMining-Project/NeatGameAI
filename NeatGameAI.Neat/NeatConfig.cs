namespace NeatGameAI.Neat
{
    public class NeatConfig
    {
        // General
        public int InputNodesCount { get; set; }
        public int OutputNodesCount { get; set; }
        public int PopulationSize { get; set; }
        public double ElitismPercentange { get; set; }
        public double CrossoverPercentage { get; set; }

        // Probabilities
        public double WeightMutateProbability { get; set; }
        public double WeightProbability { get; set; }
        public double PerturbProbability { get; set; }
        public double PerterbEpsilon { get; set; }
        public double NodeMutateProbability { get; set; }
        public double ConnectionMutateProbability { get; set; }
        public double BiasConnectionMutateProbability { get; set; }        

        public double EnableMutateProbability { get; set; }
        public double DisableMutateProbability { get; set; }

        // Utility
        public int FindRandomEnabledConnectionMaxAttepts { get; set; }
        public int FindTwoNodesToConnectMaxAttempts { get; set; }

        public NeatConfig(int inputNodesCount, int outputNodesCount)
        {
            InputNodesCount = inputNodesCount;
            OutputNodesCount = outputNodesCount;
            PopulationSize = 50;
            ElitismPercentange = 0.2;
            CrossoverPercentage = 0.75;
            
            WeightMutateProbability = 0.9;
            WeightProbability = 0.4;
            PerturbProbability = 0.9;
            PerterbEpsilon = 0.1;
            NodeMutateProbability = 0.3;
            ConnectionMutateProbability = 0.5;
            BiasConnectionMutateProbability = 0.2;

            EnableMutateProbability = 0.2;
            DisableMutateProbability = 0.4;

            FindRandomEnabledConnectionMaxAttepts = 100000;
            FindTwoNodesToConnectMaxAttempts = 100;
        }
    }
}
