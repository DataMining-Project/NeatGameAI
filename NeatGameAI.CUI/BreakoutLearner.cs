using NeatGameAI.Games.Base;
using NeatGameAI.Games.Breakout;
using NeatGameAI.Neat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeatGameAI.CUI
{
    public class BreakoutLearner
    {
        private IGame game;
        private int inputsCount;
        private int outputsCount;

        public BreakoutLearner()
        {
            game = new BreakoutGame();
            inputsCount = 6;
            outputsCount = game.GameMoves.Length;
        }

        public void Learn(int generations)
        {
            var config = new NeatConfig(inputsCount, outputsCount);
            var population = new Population(config, FitnessEvaluator);
            population.InitializePopulation();

            for (int i = 0; i < generations; i++)
            {
                population.BreedNextGeneration();
                var topFitness = population.TopFitness;
                Console.WriteLine($"Generation {i}, Best fitness: {topFitness}");
            }
        }

        public void FitnessEvaluator(Genome genome)
        {
            game.RestartGame();

            double inputValuesCount = 4.0;
            var nnInputs = new double[inputsCount];

            while (!game.IsGameOver)
            {
                // Get game state
                var gameState = game.GetCurrentState(out _);


                // Flattened array
                var flatArray = gameState.SelectMany(a => a).ToList();

                // Convert state data to neural network input data

                nnInputs[0] = 0;

                int fBlockIndex = flatArray.IndexOf(1);
                int bIndex = flatArray.IndexOf(2);
                int pIndex = flatArray.IndexOf(3);

                nnInputs[0] = (double)(fBlockIndex / game.WindowHeight) / game.WindowHeight;
                nnInputs[1] = (double)(fBlockIndex / game.WindowWidth) / game.WindowWidth;
                nnInputs[2] = (double)(bIndex / game.WindowHeight) / game.WindowHeight;
                nnInputs[3] = (double)(bIndex / game.WindowWidth) / game.WindowWidth;
                nnInputs[4] = (double)(pIndex / game.WindowHeight) / game.WindowHeight;
                nnInputs[5] = (double)(pIndex / game.WindowWidth) / game.WindowWidth;

                // Get outputs from neural network
                double[] outputs = genome.EvaluateNetwork(nnInputs);

                // Get highest output(the move AI made)
                double max = double.MinValue;
                int maxIndex = 0;
                for (int i = 0; i < outputsCount; i++)
                {
                    if (outputs[i] > max)
                    {
                        max = outputs[i];
                        maxIndex = i;
                    }
                }

                // Make move
                int move = game.GameMoves[maxIndex];
                game.MakeMove(move);
            }

            genome.Fitness = game.Score;
        }
    }
}
