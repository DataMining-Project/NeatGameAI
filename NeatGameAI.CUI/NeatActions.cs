using NeatGameAI.Games.Base;
using NeatGameAI.Games.Breakout;
using NeatGameAI.Games.Evolution;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NeatGameAI.CUI
{
    public static class NeatActions
    {
        public static void StartLearningSession(GameMenuOption gameOption)
        {
            GetGameParameters(gameOption, out IGame game, out string gameName);

            var neatAlgo = CreateNewNeatAlgorithm(game, gameName);
            TrainGame(neatAlgo, game, gameName);
        }

        public static void LoadLearningSession(GameMenuOption gameOption)
        {
            GetGameParameters(gameOption, out IGame game, out string gameName);

            if (TrySelectFileForPopulationLoad(out string fileName, game, gameName))
            {
                var neatAlgo = LoadExistingNeatAlgorithm(game, gameName, fileName);
                TrainGame(neatAlgo, game, gameName);
            }            
        }

        public static void LoadAIPlayer(GameMenuOption gameOption)
        {
            GetGameParameters(gameOption, out IGame game, out string gameName);

            if (TrySelectFileForGenomeLoad(out string fileName, game, gameName))
            {
                var network = LoadSavedAI(game, gameName, fileName);
                ShowAIPlay(game, network);
            }
        }

        public static void PlayGame(GameMenuOption gameOption)
        {
            GetGameParameters(gameOption, out IGame game, out string gameName);

            GamePlayLoop(game, gameName);
        }

        private static void GetGameParameters(GameMenuOption gameOption, out IGame game, out string gameName)
        {
            string optionName = Enum.GetName(typeof(GameMenuOption), gameOption);
            gameName = optionName + "Game";
            string assembly = "NeatGameAI.Games";
            string fullName = assembly + "." + optionName + "." + gameName;

            var objectHandle = Activator.CreateInstance(assembly, fullName);
            game = (IGame)objectHandle.Unwrap();
        }

        private static bool TrySelectFileForPopulationLoad(out string fullFileName, IGame game, string gameName)
        {
            fullFileName = string.Empty;

            string filePath = Path.Combine(".", "Populations", gameName);

            if (Directory.Exists(filePath))
            {
                var fileNames = Directory.GetFiles(filePath);

                if (fileNames.Length > 0)
                {
                    var menuNames = fileNames.Concat(new string[] { "Cancel" }).ToArray();

                    int option = ConsoleUtils.OptionsMenu("What population do you want to load:", menuNames, false);

                    if (option == menuNames.Length)
                        return false;

                    fullFileName = fileNames[option - 1];
                    return true;
                }
            }

            Console.Error.WriteLine("No populations have been saved!");
            ConsoleUtils.PressAnyKeyToContinue();
            return false;
        }

        private static bool TrySelectFileForGenomeLoad(out string fullFileName, IGame game, string gameName)
        {
            fullFileName = string.Empty;

            string filePath = Path.Combine(".", "Genomes", gameName);

            if (Directory.Exists(filePath))
            {
                var fileNames = Directory.GetFiles(filePath);

                if (fileNames.Length > 0)
                {
                    var menuNames = fileNames.Concat(new string[] { "Cancel" }).ToArray();

                    int option = ConsoleUtils.OptionsMenu("What genome do you want to load:", menuNames, false);

                    if (option == menuNames.Length)
                        return false;

                    fullFileName = fileNames[option - 1];
                    return true;
                }
            }

            Console.Error.WriteLine("No genomes have been saved!");
            ConsoleUtils.PressAnyKeyToContinue();
            return false;
        }

        private static NeatEvolutionAlgorithm<NeatGenome> CreateNewNeatAlgorithm(IGame game, string gameName)
        {
            GameExperiment experiment = new GameExperiment(game);
            experiment.Initialize(gameName);
            return experiment.CreateEvolutionAlgorithm();
        }

        private static NeatEvolutionAlgorithm<NeatGenome> LoadExistingNeatAlgorithm(IGame game, string gameName, string fullFileName)
        {
            GameExperiment experiment = new GameExperiment(game);
            experiment.Initialize(gameName);
            return LoadPopulation(experiment, gameName, fullFileName);
        }

        private static IBlackBox LoadSavedAI(IGame game, string gameName, string fullFileName)
        {
            GameExperiment experiment = new GameExperiment(game);
            experiment.Initialize(gameName);

            var genome = LoadGenome(experiment, gameName, fullFileName);

            return ExtractNetworkFromGenome(genome, game, gameName);
        }

        private static void TrainGame(NeatEvolutionAlgorithm<NeatGenome> neatAlgo, IGame game, string gameName)
        {
            neatAlgo.UpdateEvent += GameTraining_UpdateEvent;

            bool exit = false;
            while (!exit)
            {
                neatAlgo.StartContinue();
                Console.ReadKey();
                neatAlgo.Stop();

                Console.Clear();
                exit = HandleTrainPause(neatAlgo, game, gameName);
                Console.Clear();
            }
        }

        private static bool HandleTrainPause(NeatEvolutionAlgorithm<NeatGenome> neatAlgo, IGame game, string gameName)
        {
            var menuOptions = new string[]
                {
                    "Save learning session",
                    "Save best genome",
                    "Show AI play",
                    "Continue learning",
                    "Exit"
                };

            Console.WriteLine($"Learning paused! Last generation: {neatAlgo.CurrentGeneration}, Fitness: {neatAlgo.Statistics._maxFitness}");

            while (true)
            {
                int option = ConsoleUtils.OptionsMenu("Choose your action:", menuOptions, false);

                switch (option)
                {
                    case 1:
                        SavePopulation(neatAlgo, gameName);
                        Console.WriteLine("Population saved successfully!");
                        break;
                    case 2:
                        SaveBestGenome(neatAlgo, gameName);
                        Console.WriteLine("Genome saved successfully!");
                        break;
                    case 3:
                        var network = ExtractNetworkFromGenome(neatAlgo.CurrentChampGenome, game, gameName);
                        ShowAIPlay(game, network);
                        break;
                    case 4:
                        return false;
                    case 5:
                        return true;
                    default:
                        return true;
                }
            }
        }

        public static IBlackBox ExtractNetworkFromGenome(NeatGenome genome, IGame game, string gameName)
        {
            GameExperiment experiment = new GameExperiment(game);
            experiment.Initialize(gameName);

            var decoder = experiment.CreateGenomeDecoder();

            return decoder.Decode(genome);
        }

        private static void ShowAIPlay(IGame game, IBlackBox network)
        {
            // Save console window parameters and change them for the game
            var windowWidth = Console.WindowWidth;
            var windowHeight = Console.WindowHeight;
            Console.WindowWidth = game.WindowWidth;
            Console.WindowHeight = game.WindowHeight + 6;

            bool exit = false;
            while(!exit)
            {
                // Clear console and input
                ConsoleUtils.FlushKeys();
                Console.Clear();

                // Start showing game
                game = game.NewGame();                

                bool interupted = false;
                Console.CursorVisible = false;

                while (!game.IsGameOver && !interupted)
                {
                    // Get game state
                    var gameState = game.GetCurrentState(out _);

                    PrintGameState(game, gameState, 25);

                    // Check for key input
                    if (Console.KeyAvailable)
                    {
                        interupted = true;
                        break;
                    }

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

                ConsoleUtils.FlushKeys();
                Console.CursorVisible = true;

                if (interupted)
                    Console.WriteLine($"Gameplay interepted! Last score was: {game.Score:N2}");
                else
                    Console.WriteLine($"Game over, AI score was: {game.Score:N2}");
                
                exit = !ConsoleUtils.YesNoOptionMenu("Do you want to watch the play again?");
            }

            // Clear console and input
            ConsoleUtils.FlushKeys();
            Console.Clear();

            // Return console to previous state
            Console.WindowWidth = windowWidth;
            Console.WindowHeight = windowHeight;           
        }

        private static void SavePopulation(NeatEvolutionAlgorithm<NeatGenome> neatAlgo, string gameName)
        {
            var gen = neatAlgo.CurrentGeneration;
            var fitness = neatAlgo.Statistics._maxFitness;

            string filePath = Path.Combine(".", "Populations", gameName);
            string fileName = gameName + "-" + DateTime.Now.GetTimestamp() + $"-G{gen}-F{fitness:F2}.xml";

            Directory.CreateDirectory(filePath);
            var xw = XmlWriter.Create(Path.Combine(filePath, fileName), new XmlWriterSettings() { Indent = true });

            var doc = NeatGenomeXmlIO.SaveComplete(neatAlgo.GenomeList, false);
            doc.Save(xw);
            xw.Flush();
        }

        private static void SaveBestGenome(NeatEvolutionAlgorithm<NeatGenome> neatAlgo, string gameName)
        {
            var genome = neatAlgo.CurrentChampGenome;
            var fitness = neatAlgo.Statistics._maxFitness;
            
            string filePath = Path.Combine(".", "Genomes", gameName);
            string fileName = gameName + "-" + DateTime.Now.GetTimestamp() + $"-F{fitness:F2}.xml";

            Directory.CreateDirectory(filePath);
            var xw = XmlWriter.Create(Path.Combine(filePath, fileName), new XmlWriterSettings() { Indent = true });
            var doc = NeatGenomeXmlIO.SaveComplete(genome, false);
            doc.Save(xw);
            xw.Flush();
        }

        private static NeatEvolutionAlgorithm<NeatGenome> LoadPopulation(GameExperiment gameExperiment, string gameName, string fullFileName)
        {
            var xr = XmlReader.Create(fullFileName);
            var genomeFactory = gameExperiment.CreateGenomeFactory();
            var genomeList = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, (NeatGenomeFactory)genomeFactory);
            var neatAlgo = gameExperiment.CreateEvolutionAlgorithm(genomeFactory, genomeList);
            return neatAlgo;
        }

        private static NeatGenome LoadGenome(GameExperiment gameExperiment, string gameName, string fullFileName)
        {
            var xr = XmlReader.Create(fullFileName);
            var genomeFactory = gameExperiment.CreateGenomeFactory();
            var genomeList = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, (NeatGenomeFactory)genomeFactory);
            var genome = genomeList[0];
            return genome;
        }

        public static void GamePlayLoop(IGame game, string gameName)
        {
            // Save console window parameters and change them for the game
            var windowWidth = Console.WindowWidth;
            var windowHeight = Console.WindowHeight;
            Console.WindowWidth = game.WindowWidth;
            Console.WindowHeight = game.WindowHeight + 6;
            
            bool exit = false;
            while (!exit)
            {
                game = game.NewGame();
                Console.CursorVisible = false;
                Console.Clear();

                bool interupted = false;
                while (!game.IsGameOver && !interupted)
                {
                    PrintGameState(game, game.GetCurrentState(out _), 50,  true);
                    int move = 0;
                    while (Console.KeyAvailable)
                    {
                        var cki = Console.ReadKey().Key;
                        if (cki == ConsoleKey.Escape)
                        {
                            interupted = true;
                            break;
                        }

                        move = (int)cki;
                    }
                    game.MakeMove(move);
                }

                ConsoleUtils.FlushKeys();
                Console.CursorVisible = true;

                if (interupted)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.WriteLine($"Gameplay interepted! Your score was: {game.Score:N2}");
                }                    
                else
                    Console.WriteLine($"Game over, Your score was: {game.Score:N2}");

                exit = !ConsoleUtils.YesNoOptionMenu("Do you want to play again?");
            }

            // Clear console and input
            ConsoleUtils.FlushKeys();
            Console.Clear();

            // Return console to previous state
            Console.WindowWidth = windowWidth;
            Console.WindowHeight = windowHeight;
        }

        private static void GameTraining_UpdateEvent(object sender, EventArgs e)
        {
            var neatAlgo = sender as NeatEvolutionAlgorithm<NeatGenome>;
            if (neatAlgo == null) return;

            if (neatAlgo.RunState == SharpNeat.Core.RunState.Running)
                Console.WriteLine("Generation: {0, -6}, Best Fitness: {1, -10:N2}", neatAlgo.CurrentGeneration, neatAlgo.Statistics._maxFitness);
        }

        private static string GetTimestamp(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMddHHmmssfff");
        }        

        private static void PrintGameState(IGame game, int[][] gameState, int waitMilliseconds, bool isPlayerGame = false)
        {
            Thread.Sleep(waitMilliseconds);
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
                        default:
                            line.Append("█");
                            break;
                    }
                }
                Console.WriteLine(line.ToString());
            }

            if (isPlayerGame)
            {
                var lineTop = Console.CursorTop;
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, lineTop);
            }            
        }
    }
}
