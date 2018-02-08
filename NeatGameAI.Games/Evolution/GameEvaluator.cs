using NeatGameAI.Games.Base;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace NeatGameAI.Games.Evolution
{
    public class GameEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        private IGame game;
        private ulong evalCount;
        private bool stopConditionSatisfied;

        public ulong EvaluationCount { get => evalCount; }

        public bool StopConditionSatisfied { get => stopConditionSatisfied; }

        public GameEvaluator(IGame game)
        {
            this.game = game;
        }        

        public FitnessInfo Evaluate(IBlackBox network)
        {
            var game = this.game.NewGame();

            while (!game.IsGameOver)
            {
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

            return new FitnessInfo(game.Score, game.Score);
        }

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
        }
    }
}