using NeatGameAI.Games.Base;
using NeatGameAI.Games.Breakout;
using NeatGameAI.Games.Evolution;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Text;
using System.Threading;

namespace NeatGameAI.CUI
{
    class Program
    {
        static NeatEvolutionAlgorithm<NeatGenome> neatAlgo;

        static void Main(string[] args)
        {
            TrainBreakoutGame();
        }

        private static void TrainBreakoutGame()
        {
            GameExperiment experiment = new GameExperiment(new BreakoutGame());
            experiment.Initialize("Breakout");

            neatAlgo = experiment.CreateEvolutionAlgorithm();
            neatAlgo.UpdateEvent += new EventHandler(NeatAlgoUpdateEvent);

            neatAlgo.StartContinue();

            Console.ReadLine();

            neatAlgo.Stop();

            // Get best genome
            var genome = neatAlgo.CurrentChampGenome;
            var genomeDecoder = experiment.CreateGenomeDecoder();
            var phenome = genomeDecoder.Decode(genome);

            var score = ShowAIPlay(phenome);
            Console.WriteLine($"Fitness: {score}");

            Console.ReadKey();
        }

        private static double ShowAIPlay(IBlackBox network)
        {
            var game = new BreakoutGame();

            Console.WindowWidth = game.WindowWidth;
            Console.WindowHeight = game.WindowHeight + 5;

            while (!game.IsGameOver)
            {
                // Get game state
                var gameState = game.GetCurrentState(out _);

                PrintGameState(game, gameState);

                var nnInputs = game.GetNeuralInputs();

                // Clear the network
                network.ResetState();

                // Convert the game board into an input array for the network
                for (int i = 0; i < nnInputs.Length; i++)
                {
                    network.InputSignalArray[i] = nnInputs[i];
                }

                // Activate the network
                network.Activate();

                // Find the best move
                int maxIndex = 0;
                double max = double.MinValue;
                for (int i = 0; i < 3; i++)
                {
                    double score = network.OutputSignalArray[i];

                    if (max < score)
                    {
                        max = score;
                        maxIndex = i;
                    }
                }

                // Make move
                int move = game.GameMoves[maxIndex];
                game.MakeMove(move);
            }

            return game.Score;
        }

        static void PrintGameState(IGame game, int[][] gameState)
        {
            Thread.Sleep(50);
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < game.WindowHeight; i++)
            {
                var line = new StringBuilder();
                for (int j = 0; j < game.WindowWidth; j++)
                {
                    int objectValue = gameState[i][j];

                    switch (objectValue)
                    {
                        case 0:
                            line.Append(" ");
                            break;
                        case 1:
                        case 2:
                        case 3:
                            line.Append("█");
                            break;
                        default:
                            break;
                    }
                }
                Console.WriteLine(line.ToString());
            }
        }

        static void NeatAlgoUpdateEvent(object sender, EventArgs e)
        {
            var algo = (NeatEvolutionAlgorithm<NeatGenome>)sender;

            Console.WriteLine(string.Format("gen={0:N0} bestFitness={1:N6}", algo.CurrentGeneration, algo.Statistics._maxFitness));
        }
    }
}
