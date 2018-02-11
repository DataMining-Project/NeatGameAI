using NeatGameAI.Games.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeatGameAI.Games.Runner
{
    public class RunnerGame : IGame
    {
        private int blocksWidth;
        private int blocksHeight;
        private int jumpPos;
        private int jumpApex;
        private bool hasDucked;
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
        public char[] StateSymbols { get; private set; } 

        public RunnerGame()
        {
            NeuralInputsCount = 3;
            NeuralOutputsCount = 3;
            HasRandomEvents = true;

            blocksWidth = 2;
            blocksHeight = 2;
            jumpPos = 0;
            jumpApex = 3;
            hasDucked = false;
            WindowWidth = 30;
            WindowHeight = 6;
            Score = 0;
            IsGameOver = false;
            StateSymbols = new char[] {' ','▒','█', '█'};

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
            gameState[block.Y - 1][block.X] = 2;
            gameState[block.Y][block.X + 1] = 2;
            gameState[block.Y - 1][block.X + 1] = 2;

            // Draw platform
            for (int i = 0; i < WindowWidth; i++)
            {
                gameState[platform.Y][platform.X + i] = 3;
            }
        }

        public void MakeMove(int move)
        {
            if (Score == 100000)
            {
                IsGameOver = true;
                return;
            }
            if (IsGameOver)
                return;

            RunnerMove gameMove = RunnerMove.None;
            if (Enum.IsDefined(typeof(RunnerMove), move))
            {
                gameMove = (RunnerMove)move;
            }
            hasDucked = false;
            var oldPlayer = player;
            if (jumpPos == 0)
            {
                player.Y = WindowHeight - 3;
                player.Height = 2;
            }
            else if (jumpPos > 0 && jumpPos < jumpApex)
            {
                player.Y--;
                jumpPos++;
            }
            else if (jumpPos == jumpApex)
            {
                jumpPos++;
            }
            else
            {
                player.Y++;
                jumpPos++;
            }

            bool justLanded = false;
            if (jumpPos > jumpApex * 2)
            {
                jumpPos = 0;
                justLanded = true;
            }

            // Move the platform if needed
            switch (gameMove)
            {
                case RunnerMove.None:
                    break;
                case RunnerMove.Up:
                    //Score += 1;
                    if (jumpPos == 0 && !justLanded)
                    {
                        player.Y = player.Y - 1;
                        jumpPos++;
                    }
                    break;
                case RunnerMove.Down:
                    //Score += 1;
                    if (jumpPos == 0)
                    {
                        player.Y = player.Y + 1;
                        player.Height = 1;
                        hasDucked = true;
                    }
                    break;
            }
            //RedrawRectangle(oldPlayer, player, 1);

            Score += 1;

            var oldBlock = block;
            if (Score <= 1000)
            {
                block.X--;
            }
            else if (Score > 1000 && Score <= 5000)
            {
                block.X -= 2;
            }
            else
            {
                block.X -= 3;
            }

            if (block.X < 0)
            {
                block = GenerateBlock();
            }
            if (!IsPlayerHit(player, block))
            {
                RedrawPlayerAndBlock(player, block);
            }

        }

        public bool IsPlayerHit(Rectangle player, Rectangle block)
        {
            bool upperBlock = (block.Y == WindowHeight - 4);

            if (jumpPos == 0)
            {
                if (player.X == block.X || player.X == block.X + 1)
                {
                    if (!hasDucked || !upperBlock)
                    {
                        IsGameOver = true;
                        return true;
                    }

                }
            }
            else
            {
                if (player.X == block.X || player.X == block.X + 1)
                {
                    if (upperBlock || player.Y + 1 == block.Y)
                    {
                        IsGameOver = true;
                        return true;
                    }
                }
            }

            return false;
        }

        public void RedrawPlayerAndBlock(Rectangle player, Rectangle block)
        {
            var blocksBehind = block.X + 5;
            if(blocksBehind > WindowWidth)
            {
                blocksBehind = WindowWidth;
            }

            for (int i = 0; i <= WindowHeight - 2; i++)
            {
                for (int j = 0; j <= player.X; j++)
                {
                    gameState[i][j] = 0;
                }
                for (int j=block.X; j < blocksBehind; j++)
                {
                    gameState[i][j] = 0;
                }
            }
            gameState[player.Y][player.X] = 1;
            if (player.Height == 2) gameState[player.Y + 1][player.X] = 1;

            //bool upperBlock = (block.Y == WindowHeight - 3);

            gameState[block.Y][block.X] = 2;
            gameState[block.Y][block.X + 1] = 2;
            gameState[block.Y + 1][block.X] = 2;
            gameState[block.Y + 1][block.X + 1] = 2;
        }

        public Rectangle GenerateBlock()
        {
            if (rnd.NextDouble() < 0.5)
            {
                return new Rectangle(WindowWidth - 2, WindowHeight - 3, blocksWidth, blocksHeight);
            }
            else return new Rectangle(WindowWidth - 2, WindowHeight - 4, blocksWidth, blocksHeight);
        }

        public IGame NewGame()
        {
            return new RunnerGame();
        }
    }
}
