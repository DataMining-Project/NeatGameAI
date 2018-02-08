using NeatGameAI.Games.Base;
using NeatGameAI.Games.Breakout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeatGameAI.GameTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var game = new BreakoutGame();
            Console.WindowWidth = game.WindowWidth;
            Console.WindowHeight = game.WindowHeight + 5;
            GameLoop(game);
        }

        static void GameLoop(IGame game)
        {
            bool gameOver = false;
            while(!gameOver)
            {
                PrintGameState(game, game.GetCurrentState(out gameOver));
                Thread.Sleep(100);
                int move = 0;
                while (Console.KeyAvailable)
                {
                    move = (int)Console.ReadKey().Key;
                }
                game.MakeMove(move);
            }
        }

        static void PrintGameState(IGame game, int[][] gameState)
        {
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
    }
}
