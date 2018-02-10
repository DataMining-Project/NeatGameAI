namespace NeatGameAI.Games.Base
{
    public interface IGame
    {
        int WindowWidth { get; }
        int WindowHeight { get; }
        int NeuralInputsCount { get; }
        int NeuralOutputsCount { get; }
        bool HasRandomEvents { get; }        
        double Score { get; }
        bool IsGameOver { get; }
        int[] GameMoves { get; }
        char[] StateSymbols { get; }

        int[][] GetCurrentState(out bool over);
        double[] GetNeuralInputs();
        void MakeMove(int move);

        IGame NewGame();
    }
}
