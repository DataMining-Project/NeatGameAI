using NeatGameAI.Games.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NeatGameAI.Games.Snake
{
    public class SnakeGame : IGame
    {
        struct Position
        {
            public int Row;
            public int Col;

            public Position(int row, int col)
            {
                Row = row;
                Col = col;
            }
        }

        struct HeadObstaclesStatus
        {
            public bool IsThereObstacleOnTheLeft;
            public bool IsThereObstacleOnTheRight;
            public bool IsThereObstacleInFront;

            public HeadObstaclesStatus(bool isThereObstacleOnTheLeft, bool isThereObstacleOnTheRight,
                bool isThereObstacleInFront)
            {
                IsThereObstacleOnTheLeft = isThereObstacleOnTheLeft;
                IsThereObstacleOnTheRight = isThereObstacleOnTheRight;
                IsThereObstacleInFront = isThereObstacleInFront;
            }
        }

        Position[] directions;
        int direction;
        double previousMoveRelativeDistanceToFood;
        int maxStatesBeforeLastEat;
        int statesBeforeLastEat;
        double lastDistance;

        static Random rand = new Random();
        static List<Position> foodSpawns;
        int foodSpawnIndex;

        private int[][] gameState;
        Queue<Position> snakeElements;
        Position snakeHead;
        Position food;

        public int WindowWidth { get; private set; }
        public int WindowHeight { get; private set; }
        public int NeuralInputsCount { get; private set; }
        public int NeuralOutputsCount { get; private set; }
        public bool HasRandomEvents { get; private set; }
        public double Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public int[] GameMoves { get; private set; }
        public char[] StateSymbols { get; private set; }

        public SnakeGame()
        {
            NeuralInputsCount = 8;
            NeuralOutputsCount = 5;
            HasRandomEvents = false;
           
            statesBeforeLastEat = 0;

            directions = new Position[]
            {
                new Position(0, 1), // right
                new Position(0, -1), // left
                new Position(1, 0), // down
                new Position(-1, 0), // up
            };

            WindowWidth = 12;
            WindowHeight = 12;
            maxStatesBeforeLastEat = WindowWidth * WindowHeight;
            Score = 0;
            lastDistance = double.MaxValue;
            IsGameOver = false;
            StateSymbols = new char[] { ' ', '▒', '▓', '█' };

            GameMoves = Enum.GetValues(typeof(SnakeMove)).Cast<int>().ToArray();

            InitializeGame();
        }

        public int[][] GetCurrentState(out bool gameOver)
        {
            gameOver = IsGameOver;
            return gameState;
        }

        public double[] GetNeuralInputs()
        {
            var obstacles = GetHeadObstaclesStatus();

            return new double[]
            {
                (snakeHead.Col / (double)WindowWidth) * 2 - 1,
                (snakeHead.Row / (double)WindowHeight) * 2 - 1,
                (food.Col / (double)WindowWidth) * 2 - 1,
                (food.Row / (double)WindowHeight) * 2 - 1,
                (direction / 4.0) * 2 - 1,
                obstacles.IsThereObstacleInFront ? -1.0 : 0.0,
                obstacles.IsThereObstacleOnTheLeft ? -1.0 : 0.0,
                obstacles.IsThereObstacleOnTheRight ? -1.0 : 0.0
            };
        }

        public void MakeMove(int move)
        {
            if (statesBeforeLastEat == maxStatesBeforeLastEat)
                IsGameOver = true;

            if (IsGameOver)
                return;

            SnakeMove gameMove = SnakeMove.None;
            if (Enum.IsDefined(typeof(SnakeMove), move))
                gameMove = (SnakeMove)move;

            switch (gameMove)
            {
                case SnakeMove.None:
                    break;
                case SnakeMove.Left:
                    if (direction != (int)Direction.Right) direction = (int)Direction.Left;
                    break;
                case SnakeMove.Right:
                    if (direction != (int)Direction.Left) direction = (int)Direction.Right;
                    break;
                case SnakeMove.Up:
                    if (direction != (int)Direction.Down) direction = (int)Direction.Up;
                    break;
                case SnakeMove.Down:
                    if (direction != (int)Direction.Up) direction = (int)Direction.Down;
                    break;
                default:
                    break;
            }

            MoveSnake();
        }

        public IGame NewGame()
        {
            return new SnakeGame();
        }

        IGame IGame.NewGame()
        {
            return NewGame();
        }

        private void InitializeGame()
        {
            foodSpawnIndex = 0;
            if (foodSpawns == null)
            {
                foodSpawns = new List<Position>();
                for (int i = 0; i < WindowHeight; i++)
                    for (int j = 0; j < WindowWidth; j++)
                        foodSpawns.Add(new Position(i, j));

                foodSpawns = foodSpawns.OrderBy(x => rand.Next()).ToList();
            }

            direction = (int)Direction.Right;
            snakeElements = new Queue<Position>();
            var initialTailPosition = new Position(5, 5);
            snakeElements.Enqueue(initialTailPosition);
            var initialHeadPosition = new Position(5, 6);
            snakeElements.Enqueue(initialHeadPosition);
            snakeHead = initialHeadPosition;

            food = GetNextFoodPosition();

            previousMoveRelativeDistanceToFood = RelativeDistanceToFood();

            // Create initial game state
            gameState = new int[WindowHeight][];
            for (int i = 0; i < WindowHeight; i++)
                gameState[i] = new int[WindowWidth];

            // Draw food
            gameState[food.Row][food.Col] = 1;

            // Draw snake
            foreach (Position position in snakeElements)
            {
                if (position.Row != snakeHead.Row && position.Col != snakeHead.Col)
                    gameState[position.Row][position.Col] = 2;
                else // draw the head
                    gameState[position.Row][position.Col] = 3;
            }
        }

        private void MoveSnake()
        {
            Position nextDirection = directions[direction];
            Position snakeNewHead = new Position(snakeHead.Row + nextDirection.Row, snakeHead.Col + nextDirection.Col);

            if (snakeElements.Contains(snakeNewHead) || snakeNewHead.Col < 0 || snakeNewHead.Row < 0
                || snakeNewHead.Row >= WindowHeight || snakeNewHead.Col >= WindowWidth)
            {
                IsGameOver = true;
                return;
            }

            // Remove snake head
            gameState[snakeHead.Row][snakeHead.Col] = 2;

            // Draw new snake head
            snakeElements.Enqueue(snakeNewHead);
            gameState[snakeNewHead.Row][snakeNewHead.Col] = 3;

            // Set new snake head
            snakeHead = snakeNewHead;

            if (snakeNewHead.Col == food.Col && snakeNewHead.Row == food.Row)
            {
                // Feeding the snake
                Score += 1000;
                if (WindowHeight * WindowWidth == snakeElements.Count)
                {
                    IsGameOver = true;
                    return;
                }

                lastDistance = double.MaxValue;
                food = GetNextFoodPosition();

                // Draw food
                gameState[food.Row][food.Col] = 1;
                statesBeforeLastEat = 0;
            }
            else
            {
                // Moving...
                statesBeforeLastEat++;
                Position last = snakeElements.Dequeue();
                // Remove last snake element
                gameState[last.Row][last.Col] = 0;
            }

            var dist = DistanceToFood();
            if (dist < lastDistance)
            {
                lastDistance = dist;
                Score += (RelativeDistanceToFood()) * 10;
            }
        }

        private Position GetNextFoodPosition()
        {
            Position food;
            do
            {
                if (foodSpawnIndex == foodSpawns.Count)
                    foodSpawnIndex = 0;

                food = foodSpawns[foodSpawnIndex++];
            } while (snakeElements.Contains(food));

            return food;
        }

        private double RelativeDistanceToFood()
        {
            double dist = DistanceToFood();
            //double maxDist = Math.Sqrt(Math.Pow(WindowHeight, 2) + Math.Pow(WindowWidth, 2));
            double maxDist = WindowWidth + WindowHeight;
            return (maxDist - dist) / maxDist;
        }

        private double DistanceToFood()
        {
            //double dist = Math.Sqrt(Math.Pow(food.Col - snakeHead.Col, 2) + Math.Pow(food.Row - snakeHead.Row, 2));
            double dist = Math.Abs(food.Col - snakeHead.Col) + Math.Abs(food.Row - snakeHead.Row);
            return dist;
        }

        private HeadObstaclesStatus GetHeadObstaclesStatus()
        {
            bool obstacleLeftCol = false;
            bool obstacleRightCol = false;
            bool obstacleUpRow = false;
            bool obstacleDownRow = false;
            bool isThereObstacleOnTheLeft = false;
            bool isThereObstacleOnTheRight = false;
            bool isThereObstacleInFront = false;

            if (snakeHead.Col == 0 || gameState[snakeHead.Row][snakeHead.Col - 1] == 2)
                obstacleLeftCol = true;

            if (snakeHead.Col == WindowWidth - 1 || gameState[snakeHead.Row][snakeHead.Col + 1] == 2)
                obstacleRightCol = true;

            if (snakeHead.Row == 0 || gameState[snakeHead.Row - 1][snakeHead.Col] == 2)
                obstacleUpRow = true;

            if (snakeHead.Row == WindowHeight - 1 || gameState[snakeHead.Row + 1][snakeHead.Col] == 2)
                obstacleDownRow = true;

            var dir = (Direction)direction;

            switch (dir)
            {
                case Direction.Right:
                    isThereObstacleOnTheLeft = obstacleUpRow;
                    isThereObstacleOnTheRight = obstacleDownRow;
                    isThereObstacleInFront = obstacleRightCol;
                    break;
                case Direction.Left:
                    isThereObstacleOnTheLeft = obstacleDownRow;
                    isThereObstacleOnTheRight = obstacleUpRow;
                    isThereObstacleInFront = obstacleLeftCol;
                    break;
                case Direction.Down:
                    isThereObstacleOnTheLeft = obstacleRightCol;
                    isThereObstacleOnTheRight = obstacleLeftCol;
                    isThereObstacleInFront = obstacleDownRow;
                    break;
                case Direction.Up:
                    isThereObstacleOnTheLeft = obstacleLeftCol;
                    isThereObstacleOnTheRight = obstacleRightCol;
                    isThereObstacleInFront = obstacleUpRow;
                    break;
                default:
                    break;
            }

            return new HeadObstaclesStatus(
                isThereObstacleOnTheLeft,
                isThereObstacleOnTheRight,
                isThereObstacleInFront
            );
        }

        enum Direction
        {
            Right = 0,
            Left = 1,
            Down = 2,
            Up = 3
        }
    }
}
