namespace NeatGameAI.Games.Base
{
    public interface IGame
    {
        int WindowWidth { get; }
        int WindowHeight { get; }
        int Score { get; }
        bool IsGameOver { get; }
        int[] GameMoves { get; }

        int[][] GetCurrentState(out bool over);
        void MakeMove(int move);
        void RestartGame();
    }
}
