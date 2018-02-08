using NeatGameAI.Games.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NeatGameAI.Games.Breakout
{
    public class BreakoutGame : IGame
    {
        private int platformWidth;
        private int blocksWidth;
        private int blocksHeigth;
        private int blockRows;
        private int blockCols;
        private int blocksSpaceTop;
        private int movesPerBallMove;
        private int movesUntilBallMove;

        private int[][] gameState;
        private Rectangle ball;
        private BallAngle ballAngle;
        private BallDirection ballDirection;
        private Rectangle platform;

        private int[][] blockIndexes;
        private List<Rectangle> blocks;

        public int WindowWidth { get; private set; }
        public int WindowHeight { get; private set; }
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public int[] GameMoves { get; private set; }

        public BreakoutGame()
        {
            platformWidth = 12;
            blockRows = 6;
            blockCols = 10;
            blocksWidth = 6;
            blocksHeigth = 2;
            blocksSpaceTop = 6;

            movesPerBallMove = 2;

            WindowWidth = blockCols * blockRows;
            WindowHeight = 40;
            Score = 0;
            IsGameOver = false;
            GameMoves = Enum.GetValues(typeof(BreakoutMove)).Cast<int>().ToArray();
            RestartGame();
        }

        public int[][] GetCurrentState(out bool gameOver)
        {
            gameOver = IsGameOver;
            return gameState;
        }

        public void MakeMove(int move)
        {
            if (IsGameOver)
                return;

            BreakoutMove gameMove = BreakoutMove.None;
            if (Enum.IsDefined(typeof(BreakoutMove), move))
            {
                gameMove = (BreakoutMove)move;
            }

            // Move the platform if needed
            switch (gameMove)
            {
                case BreakoutMove.None:
                    break;
                case BreakoutMove.Left:
                    if (platform.X > 0)
                    {
                        gameState[platform.Y][platform.X + platformWidth - 1] = 0;
                        platform.X = platform.X - 1;
                        gameState[platform.Y][platform.X] = 3;
                    }
                    if (platform.X > 0)
                    {
                        gameState[platform.Y][platform.X + platformWidth - 1] = 0;
                        platform.X = platform.X - 1;
                        gameState[platform.Y][platform.X] = 3;
                    }
                    break;
                case BreakoutMove.Right:
                    if (platform.X + platformWidth < WindowWidth)
                    {
                        gameState[platform.Y][platform.X] = 0;
                        platform.X = platform.X + 1;
                        gameState[platform.Y][platform.X + platformWidth - 1] = 3;
                    }
                    if (platform.X + platformWidth < WindowWidth)
                    {
                        gameState[platform.Y][platform.X] = 0;
                        platform.X = platform.X + 1;
                        gameState[platform.Y][platform.X + platformWidth - 1] = 3;
                    }
                    break;
            }

            movesUntilBallMove--;

            if (movesUntilBallMove == 0)
            {
                MoveBall();
                movesUntilBallMove = movesPerBallMove;
            }
        }

        private void MoveBall()
        {
            // Check if ball have block neighbours, remove them and update direction
            RemoveAdjacentBlocks();

            // Calculate new ball position
            Rectangle newBall = new Rectangle(ball.X, ball.Y, 2, 2);
            BallAngle newBallAngle = ballAngle;
            BallDirection newBallDirection = ballDirection;

            CalculateNewBallPosition(ball, ref newBall, ballDirection, ballAngle);

            // If ball is after platform line, check if ricochet or game over
            if (newBall.Y >= WindowHeight - 2) // if after platform line, check if ricochet or gameover
            {
                if (!IsPlatformRicochet(ref newBall, ref newBallDirection, ref newBallAngle))
                {
                    IsGameOver = true;
                    DrawBallOnNewPosition(ball, newBall);
                    return;
                }
            }

            bool leftBoundHit = false;
            bool rightBoundHit = false;
            bool topBoundHit = false;

            // Check if ball overlaps with window bounds and fix its position
            FixBallOutOfBounds(ref newBall, ref newBallDirection, ref newBallAngle, ref leftBoundHit, ref rightBoundHit, ref topBoundHit);

            // Check if ball overlaps with blocks and remove them and fix its position
            RemoveHitBlocks(ref newBall, ref newBallDirection, ref newBallAngle, leftBoundHit, rightBoundHit);

            // Check if ball overlaps with window bounds and fix its position
            FixBallOutOfBounds(ref newBall, ref newBallDirection, ref newBallAngle, ref leftBoundHit, ref rightBoundHit, ref topBoundHit);

            // Draw ball on new position
            DrawBallOnNewPosition(ball, newBall);
            ball = newBall;
            ballAngle = newBallAngle;
            ballDirection = newBallDirection;
        }

        private void RemoveAdjacentBlocks()
        {
            var blocksToRemove = new HashSet<int>();

            bool topAdjacent = false, bottomAdjacent = false,
                rightAdjacent = false, leftAdjacent = false,
                topLeftAdjacent = false, topRightAdjacent = false,
                bottomLeftAdjacent = false, bottomRightAdjacent = false;

            // Top check
            if (ball.Y > blocksSpaceTop && ball.Y <= blocksSpaceTop + (blockRows * blocksHeigth))
            {
                if (gameState[ball.Y - 1][ball.X] == 1)
                {
                    topAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y - 1 - blocksSpaceTop][ball.X]);
                }
                if (gameState[ball.Y - 1][ball.X + 1] == 1)
                {
                    topAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y - 1 - blocksSpaceTop][ball.X + 1]);
                }

                // Top left check
                if (ball.X > 0)
                {
                    if (gameState[ball.Y - 1][ball.X - 1] == 1)
                    {
                        topLeftAdjacent = true;
                        blocksToRemove.Add(blockIndexes[ball.Y - 1 - blocksSpaceTop][ball.X - 1]);
                    }
                }
                // Top right check
                if (ball.X + 2 < WindowWidth)
                {
                    if (gameState[ball.Y - 1][ball.X + 2] == 1)
                    {
                        topRightAdjacent = true;
                        blocksToRemove.Add(blockIndexes[ball.Y - 1 - blocksSpaceTop][ball.X + 2]);
                    }
                }
            }
            // Bottom check
            if (ball.Y >= blocksSpaceTop - 2 && ball.Y < blocksSpaceTop + (blockRows * blocksHeigth))
            {
                if (gameState[ball.Y + 2][ball.X] == 1)
                {
                    bottomAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y + 2 - blocksSpaceTop][ball.X]);
                }
                if (gameState[ball.Y + 2][ball.X + 1] == 1)
                {
                    bottomAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y + 2 - blocksSpaceTop][ball.X + 1]);
                }

                // Bottom left check
                if (ball.X > 0)
                {
                    if (gameState[ball.Y + 2][ball.X - 1] == 1)
                    {
                        bottomLeftAdjacent = true;
                        blocksToRemove.Add(blockIndexes[ball.Y + 2 - blocksSpaceTop][ball.X - 1]);
                    }
                }
                // Bottom right check
                if (ball.X + 2 < WindowWidth)
                {
                    if (gameState[ball.Y + 2][ball.X + 2] == 1)
                    {
                        bottomRightAdjacent = true;
                        blocksToRemove.Add(blockIndexes[ball.Y + 2 - blocksSpaceTop][ball.X + 2]);
                    }
                }
            }

            // Left check
            if (ball.X > 0)
            {
                if (gameState[ball.Y][ball.X - 1] == 1)
                {
                    leftAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y - blocksSpaceTop][ball.X - 1]);
                }
                if (gameState[ball.Y + 1][ball.X - 1] == 1)
                {
                    leftAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y + 1 - blocksSpaceTop][ball.X - 1]);
                }
            }
            // Right check
            if (ball.X + 2 < WindowWidth)
            {
                if (gameState[ball.Y][ball.X + 2] == 1)
                {
                    rightAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y - blocksSpaceTop][ball.X + 2]);
                }
                if (gameState[ball.Y + 1][ball.X + 2] == 1)
                {
                    rightAdjacent = true;
                    blocksToRemove.Add(blockIndexes[ball.Y + 1 - blocksSpaceTop][ball.X + 2]);
                }
            }

            // Change ball angle and direction
            bool hasSideAdjacent = false;
            if (leftAdjacent || rightAdjacent)
            {
                hasSideAdjacent = true;
                ballAngle = (BallAngle)((int)ballAngle * -1);
            }
            if (topAdjacent || bottomAdjacent)
            {
                hasSideAdjacent = true;
                ballDirection = (BallDirection)((int)ballDirection * -1);
            }

            // Handle edges
            if (!hasSideAdjacent)
            {
                if ((topLeftAdjacent && (int)ballAngle < 0 && (int)ballDirection < 0) ||
                    (topRightAdjacent && (int)ballAngle > 0 && (int)ballDirection < 0) ||
                    (bottomLeftAdjacent && (int)ballAngle < 0 && (int)ballDirection > 0) ||
                    (bottomRightAdjacent && (int)ballAngle > 0 && (int)ballDirection > 0))
                {
                    ballAngle = (BallAngle)((int)ballAngle * -1);
                    ballDirection = (BallDirection)((int)ballDirection * -1);
                }
            }

            // Remove adjacent blocks
            foreach (var blockIndex in blocksToRemove)
            {
                var block = blocks[blockIndex];

                for (int i = 0; i < blocksHeigth; i++)
                {
                    for (int j = 0; j < blocksWidth; j++)
                    {
                        gameState[block.Y + i][block.X + j] = 0;
                    }
                }
            }

            Score += blocksToRemove.Count * 100;
        }

        private void CalculateNewBallPosition(Rectangle oldBall, ref Rectangle newBall, BallDirection ballDirection, BallAngle ballAngle)
        {
            switch (ballAngle)
            {
                case BallAngle.LeftBlunt:
                    newBall.X = oldBall.X - 2;
                    newBall.Y = oldBall.Y + (1 * (int)ballDirection);
                    break;
                case BallAngle.LeftNormal:
                    newBall.X = oldBall.X - 2;
                    newBall.Y = oldBall.Y + (2 * (int)ballDirection);
                    break;
                case BallAngle.LeftSharp:
                    newBall.X = oldBall.X - 1;
                    newBall.Y = oldBall.Y + (2 * (int)ballDirection);
                    break;
                case BallAngle.RigthSharp:
                    newBall.X = oldBall.X + 1;
                    newBall.Y = oldBall.Y + (2 * (int)ballDirection);
                    break;
                case BallAngle.RightNormal:
                    newBall.X = oldBall.X + 2;
                    newBall.Y = oldBall.Y + (2 * (int)ballDirection);
                    break;
                case BallAngle.RightBlunt:
                    newBall.X = oldBall.X + 2;
                    newBall.Y = oldBall.Y + (1 * (int)ballDirection);
                    break;
            }
        }

        private bool IsPlatformRicochet(ref Rectangle newBall, ref BallDirection newBallDirection, ref BallAngle newAngle)
        {
            if (newBall.Y < WindowHeight - 2)
                return false;

            // Calculate ball position on impact
            Rectangle impactBall;
            if (newBall.Y == platform.Y)
                impactBall = ball;
            else
            {
                int impactBallX = ball.X;
                if (ballAngle == BallAngle.LeftNormal)
                    impactBallX -= 1;
                else if (ballAngle == BallAngle.RightNormal)
                    impactBallX += 1;

                impactBall = new Rectangle(impactBallX, WindowHeight - 3, 2, 2);
            }

            bool hasRicochet = false;

            // Normal ricochet
            if (platform.X <= impactBall.X + 1 && impactBall.X < platform.X + platformWidth)
            {
                hasRicochet = true;

                // Calculate new ball angle
                int platformMiddle = platform.X - 1 + platformWidth / 2;
                int distanceFromMiddle = ball.X - platformMiddle;
                bool leftSideOfPlatform = distanceFromMiddle < 0;
                int modulusDist = Math.Abs(distanceFromMiddle);

                if (modulusDist > 4)
                {
                    if (leftSideOfPlatform)
                        newAngle = BallAngle.LeftBlunt;
                    else
                        newAngle = BallAngle.RightBlunt;
                }
                else if (modulusDist > 1)
                {
                    if (leftSideOfPlatform)
                        newAngle = BallAngle.LeftNormal;
                    else
                        newAngle = BallAngle.RightNormal;
                }
                else if (modulusDist != 0)
                {
                    if (leftSideOfPlatform)
                        newAngle = BallAngle.LeftSharp;
                    else
                        newAngle = BallAngle.RigthSharp;
                }
                else // if right in the middle change direction of comming ball
                {
                    int newDirection = (int)newAngle < 0 ? 1 : -1;
                    newAngle = (BallAngle)((int)BallAngle.RigthSharp * newDirection);
                }
            }
            else // Edge cases
            {
                if ((int)ballAngle < 0 && impactBall.X == platform.X + platformWidth)
                {
                    hasRicochet = true;
                    newAngle = BallAngle.RightBlunt;
                }
                else if ((int)ballAngle > 0 && impactBall.X + 2 == platform.X)
                {
                    hasRicochet = true;
                    newAngle = BallAngle.LeftBlunt;
                }
            }

            if (hasRicochet)
            {
                // Calculate new ball position after ricochet
                newBallDirection = BallDirection.Up;
                CalculateNewBallPosition(impactBall, ref newBall, newBallDirection, newAngle);
            }

            return hasRicochet;
        }

        private void FixBallOutOfBounds(ref Rectangle newBall, ref BallDirection newBallDirection, ref BallAngle newBallAngle,
                                        ref bool leftBoundHit, ref bool rightBoundHit, ref bool topBoundHit)
        {
            if (newBall.X < 0)
            {
                newBall.X += 2 * (newBall.X * -1);
                if (!leftBoundHit)
                    newBallAngle = (BallAngle)((int)newBallAngle * -1);

                leftBoundHit = true;
            }
            else if (newBall.X >= WindowWidth - 1)
            {
                newBall.X -= 2 * (newBall.X - WindowWidth + 2);
                if (!rightBoundHit)
                    newBallAngle = (BallAngle)((int)newBallAngle * -1);

                rightBoundHit = true;
            }

            if (newBall.Y < 0)
            {
                newBall.Y += 2 * (newBall.Y * -1);
                if (!topBoundHit)
                    newBallDirection = BallDirection.Down;

                topBoundHit = true;
            }
        }

        private void RemoveHitBlocks(ref Rectangle ball, ref BallDirection ballDirection, ref BallAngle ballAngle, bool leftBoundHit, bool rightBoundHit)
        {

            var blocksToRemove = new HashSet<int>();

            bool topLeftOverlap = false, topRightOverlap = false,
                bottomLeftOverlap = false, bottomRightOverlap = false;

            // Top check
            if (ball.Y > blocksSpaceTop && ball.Y <= blocksSpaceTop + (blockRows * blocksHeigth))
            {
                if (gameState[ball.Y][ball.X] == 1)
                {
                    topLeftOverlap = true;
                    blocksToRemove.Add(blockIndexes[ball.Y - blocksSpaceTop][ball.X]);
                }
                if (gameState[ball.Y][ball.X + 1] == 1)
                {
                    topRightOverlap = true;
                    blocksToRemove.Add(blockIndexes[ball.Y - blocksSpaceTop][ball.X + 1]);
                }
            }

            // Bottom check
            if (ball.Y >= blocksSpaceTop - 2 && ball.Y < blocksSpaceTop + (blockRows * blocksHeigth))
            {
                if (gameState[ball.Y + 1][ball.X] == 1)
                {
                    bottomLeftOverlap = true;
                    blocksToRemove.Add(blockIndexes[ball.Y + 1 - blocksSpaceTop][ball.X]);
                }
                if (gameState[ball.Y + 1][ball.X + 1] == 1)
                {
                    bottomRightOverlap = true;
                    blocksToRemove.Add(blockIndexes[ball.Y + 1 - blocksSpaceTop][ball.X + 1]);
                }
            }
            // Left check
            if (gameState[ball.Y][ball.X] == 1)
            {
                topLeftOverlap = true;
                blocksToRemove.Add(blockIndexes[ball.Y - blocksSpaceTop][ball.X]);
            }
            if (gameState[ball.Y + 1][ball.X] == 1)
            {
                bottomLeftOverlap = true;
                blocksToRemove.Add(blockIndexes[ball.Y + 1 - blocksSpaceTop][ball.X]);
            }
            // Right check
            if (gameState[ball.Y][ball.X + 1] == 1)
            {
                topRightOverlap = true;
                blocksToRemove.Add(blockIndexes[ball.Y - blocksSpaceTop][ball.X + 1]);
            }
            if (gameState[ball.Y + 1][ball.X + 1] == 1)
            {
                bottomRightOverlap = true;
                blocksToRemove.Add(blockIndexes[ball.Y + 1 - blocksSpaceTop][ball.X + 1]);
            }

            // Calulate new ball position
            //if (topLeftOverlap && topRightOverlap && bottomLeftOverlap)
            //{
            //    ball.X += 2;
            //    ball.Y += 2;
            //    ballAngle = (BallAngle)((int)ballAngle * -1);
            //    ballDirection = (BallDirection)((int)ballDirection * -1);

            //}
            //if (topLeftOverlap && topRightOverlap && bottomRightOverlap)
            //{
            //    ball.X -= 2;
            //    ball.Y += 2;
            //    ballAngle = (BallAngle)((int)ballAngle * -1);
            //    ballDirection = (BallDirection)((int)ballDirection * -1);
            //}
            //if (bottomLeftOverlap && bottomRightOverlap && topLeftOverlap)
            //{
            //    ball.X += 2;
            //    ball.Y -= 2;
            //    ballAngle = (BallAngle)((int)ballAngle * -1);
            //    ballDirection = (BallDirection)((int)ballDirection * -1);
            //}
            //if (bottomLeftOverlap && bottomRightOverlap && topRightOverlap)
            //{
            //    ball.X -= 2;
            //    ball.Y -= 2;
            //    ballAngle = (BallAngle)((int)ballAngle * -1);
            //    ballDirection = (BallDirection)((int)ballDirection * -1);
            //}

            if ((topLeftOverlap && !topRightOverlap && !bottomLeftOverlap && bottomRightOverlap) ||
                (!topLeftOverlap && topRightOverlap && bottomLeftOverlap && !bottomRightOverlap))
            {
                if ((int)ballAngle < 0)
                    ball.X += 2;
                else
                    ball.X -= 2;

                if ((int)ballDirection < 0)
                    ball.Y += 2;
                else
                    ball.Y -= 2;

                ballAngle = (BallAngle)((int)ballAngle * -1);
                ballDirection = (BallDirection)((int)ballDirection * -1);
            }

            if (topLeftOverlap && bottomLeftOverlap)
            {
                ball.X += 2;
                if (!leftBoundHit)
                    ballAngle = (BallAngle)((int)ballAngle * -1);
            }
            else if (topRightOverlap && bottomRightOverlap)
            {
                ball.X -= 2;
                if (!rightBoundHit)
                    ballAngle = (BallAngle)((int)ballAngle * -1);
            }

            if (topLeftOverlap && topRightOverlap)
            {
                ball.Y += 2;
                ballDirection = (BallDirection)((int)ballDirection * -1);
            }
            else if (bottomRightOverlap && bottomLeftOverlap)
            {
                ball.Y -= 2;
                ballDirection = (BallDirection)((int)ballDirection * -1);
            }

            if ((topLeftOverlap && (int)ballAngle < 0 && (int)ballDirection < 0) ||
                (topRightOverlap && (int)ballAngle > 0 && (int)ballDirection < 0) ||
                (bottomLeftOverlap && (int)ballAngle < 0 && (int)ballDirection > 0) ||
                (bottomRightOverlap && (int)ballAngle > 0 && (int)ballDirection > 0))
            {
                ballAngle = (BallAngle)((int)ballAngle * -1);
                ballDirection = (BallDirection)((int)ballDirection * -1);
            }

            // Remove hit blocks
            foreach (var blockIndex in blocksToRemove)
            {
                var block = blocks[blockIndex];

                for (int i = 0; i < blocksHeigth; i++)
                {
                    for (int j = 0; j < blocksWidth; j++)
                    {
                        gameState[block.Y + i][block.X + j] = 0;
                    }
                }
            }

            Score += blocksToRemove.Count * 100;
        }

        public void DrawBallOnNewPosition(Rectangle oldBall, Rectangle newBall)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    gameState[oldBall.Y + i][oldBall.X + j] = 0;
                }
            }

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (newBall.Y + i >= 0 && newBall.Y + i < WindowHeight
                        && newBall.X + j >= 0 && newBall.X + j < WindowWidth)
                        gameState[newBall.Y + i][newBall.X + j] = 2;
                }
            }
        }

        public void RestartGame()
        {
            Score = 0;
            IsGameOver = false;
            movesUntilBallMove = movesPerBallMove;
            int middleX = WindowWidth / 2;

            // Create platform
            platform = new Rectangle(middleX - (platformWidth / 2), WindowHeight - 1, platformWidth, 1);

            // Create ball
            ball = new Rectangle(middleX - 1, WindowHeight - 3, 2, 2);
            ballAngle = BallAngle.RightNormal;
            ballDirection = BallDirection.Up;

            // Create blocks
            blocks = new List<Rectangle>();
            blockIndexes = new int[blockRows * blocksHeigth][];

            for (int row = 0; row < blockRows; row++)
            {
                for (int i = 0; i < blocksHeigth; i++)
                {
                    blockIndexes[(row * blocksHeigth) + i] = new int[blockCols * blocksWidth];
                }

                for (int col = 0; col < blockCols; col++)
                {
                    int blockX = col * blocksWidth;
                    int blockY = blocksSpaceTop + (row * blocksHeigth);
                    var block = new Rectangle(blockX, blockY, blocksWidth, blocksHeigth);

                    blocks.Add(block);
                    int blockIndex = blocks.Count - 1;

                    // Save block index for fast lookup
                    for (int i = 0; i < blocksHeigth; i++)
                    {
                        for (int j = 0; j < blocksWidth; j++)
                        {
                            blockIndexes[(row * blocksHeigth) + i][blockX + j] = blockIndex;
                        }
                    }
                }
            }

            // Create initial game state
            gameState = new int[WindowHeight][];
            for (int i = 0; i < WindowHeight; i++)
            {
                gameState[i] = new int[WindowWidth];
                // If block row, draw the blocks
                if (i >= blocksSpaceTop && i < blocksSpaceTop + (blockRows * blocksHeigth))
                {
                    for (int j = 0; j < WindowWidth; j++)
                    {
                        gameState[i][j] = 1;
                    }
                }
            }

            // Draw the ball
            gameState[ball.Y][ball.X] = 2;
            gameState[ball.Y][ball.X + 1] = 2;
            gameState[ball.Y + 1][ball.X] = 2;
            gameState[ball.Y + 1][ball.X + 1] = 2;

            // Draw platform
            for (int i = 0; i < platformWidth; i++)
            {
                gameState[platform.Y][platform.X + i] = 3;
            }
        }

        enum BallAngle
        {
            LeftBlunt = -3,
            LeftNormal = -2,
            LeftSharp = -1,
            RigthSharp = 1,
            RightNormal = 2,
            RightBlunt = 3
        }

        enum BallDirection { Up = -1, Down = 1 }
    }
}
