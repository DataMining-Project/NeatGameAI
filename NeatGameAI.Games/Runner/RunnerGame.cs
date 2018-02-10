using NeatGameAI.Games.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeatGameAI.Games.Runner
{
    class RunnerGame : IGame
    {
        private int blocksWidth;
        private int blocksHeight;
        private int jumpPos;
        private int jumpApex;
        private static Random rnd = new Random();

        private int[][] gameState;
        private Rectangle player;
        private Rectangle block;
        private Rectangle platform;

        public int WindowWidth { get; private set; }
        public int WindowHeight { get; private set; }
        public int NeuralInputsCount { get; private set; }
        public int NeuralOutputsCount { get; private set; }
        public bool HasRandomEvents { get; private set; }
        public double Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public int[] GameMoves { get; private set; }

        public RunnerGame()
        {
            NeuralInputsCount = 3;
            NeuralOutputsCount = 3;
            HasRandomEvents = true;

            blocksWidth = 2;
            blocksHeight = 1;
            jumpPos = 0;
            jumpApex = 3;
            WindowWidth = 30;
            WindowHeight = 6;
            Score = 0;
            IsGameOver = false;
            GameMoves = Enum.GetValues(typeof(RunnerMove)).Cast<int>().ToArray();

            InitializeGame();
        }

        public int[][] GetCurrentState(out bool gameOver)
        {
            gameOver = IsGameOver;
            return gameState;
        }

        public double[] GetNeuralInputs()
        {
            return new double[]
            {
                (double)block.Y / WindowHeight,
                (double)block.X / WindowWidth,
                (double)player.Y / WindowHeight
            };
        }

        public void InitializeGame()
        {

            // Create platform
            platform = new Rectangle(0, WindowHeight - 1, WindowWidth, 1);

            // Create player
            player = new Rectangle(3, WindowHeight - 3, 1, 2);
            jumpPos = 0;
            // Create block
            block = GenerateBlock();

            // Create initial game state
            gameState = new int[WindowHeight][];
            for (int i = 0; i < WindowHeight; i++)
            {
                gameState[i] = new int[WindowWidth];
            }

            // Draw the player
            gameState[player.Y][player.X] = 1;
            gameState[player.Y + 1][player.X] = 1;

            // Draw block
            gameState[block.Y][block.X] = 2;
            gameState[block.Y][block.X + 1] = 2;

            // Draw platform
            for (int i = 0; i < WindowWidth; i++)
            {
                gameState[platform.Y][platform.X + i] = 3;
            }
        }

        public void MakeMove(int move)
        {
            if (IsGameOver)
                return;

            RunnerMove gameMove = RunnerMove.None;
            if (Enum.IsDefined(typeof(RunnerMove), move))
            {
                gameMove = (RunnerMove)move;
            }

            if (jumpPos == 0)
            {
                player.Y = WindowHeight - 3;
                player.Height = 2;
            }
            else if (jumpPos > 0 && jumpPos < 3)
            {
                var oldPlayer = player;
                player.Y--;
                jumpPos++;
                RedrawRectangle(oldPlayer, player, 1);
            }
            else if (jumpPos > 3)
            {
                var oldPlayer = player;
                player.Y++;
                jumpPos++;
                RedrawRectangle(oldPlayer, player, 1);
            }

            if (jumpPos == jumpApex * 2)
            {
                jumpPos = 0;
            }

            // Move the platform if needed
            switch (gameMove)
            {
                case RunnerMove.None:
                    break;
                case RunnerMove.Up:
                    //Score += 1;
                    if (jumpPos == 0)
                    {
                        gameState[player.Y][player.X] = 0;
                        player.Y = player.Y - 1;
                        gameState[player.Y][player.X] = 1;
                        jumpPos++;
                    }
                    break;
                case RunnerMove.Down:
                    //Score += 1;
                    if (jumpPos == 0)
                    {
                        gameState[player.Y][player.X] = 0;
                        player.Y = player.Y + 1;
                        player.Height = 1;
                        //gameState[player.Y][player.X] = 1;
                    }
                    break;
            }


            //Score += ((WindowWidth - ((platform.X + platformWidth / 2) - ball.X)) / (double)WindowWidth) * 10;

            var oldBlock = block;
            block.X--;
            if(block.X < 0)
            {
                block = GenerateBlock();
            }
            if (!IsPlayerHit(player,block))
            {
                RedrawRectangle(oldBlock, block, 2);
            }
            
        }

        public bool IsPlayerHit(Rectangle player, Rectangle block)
        {
            if (jumpPos == 0)
            {
                if (player.X == block.X && player.Height == 2 || block.Y == WindowHeight - 2)
                {
                    IsGameOver = true;
                    return true;
                }
            }
            else
            {
                if (player.X == block.X && (player.Y + player.Height) >= block.Y)
                {
                    IsGameOver = true;
                    return true;
                }
            }

            return false;
        }


        public void RedrawRectangle(Rectangle oldRect, Rectangle newRect, int num)
        {
            gameState[oldRect.Y][oldRect.X] = 0;
            if (oldRect.Width == 2) gameState[oldRect.Y][oldRect.X + 1] = 0;
            if (oldRect.Height == 2) gameState[oldRect.Y + 1][oldRect.X] = 0;

            gameState[newRect.Y][newRect.X] = num;
            if (newRect.Width == 2) gameState[newRect.Y][newRect.X + 1] = num;
            if (newRect.Height == 2) gameState[newRect.Y + 1][newRect.X] = num;
        }
        public Rectangle GenerateBlock()
        {
            if (rnd.NextDouble() < 0.5)
            {
                return new Rectangle(WindowWidth - 2, WindowHeight - 2, 2, 1);
            }
            else return new Rectangle(WindowWidth - 2, WindowHeight - 3, 2, 1);
        }

        public IGame NewGame()
        {
            return new RunnerGame();
        }
    }
}
