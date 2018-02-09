using NeatGameAI.Games.Base;
using NeatGameAI.Games.Breakout;
using NeatGameAI.Games.Evolution;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace NeatGameAI.CUI
{
    class Program
    {
        static void Main(string[] args)
        {
            bool exit = false;
            while (!exit)
            {
                int choosenOption = ChooseGameMenu(out exit);
                if (exit) break;

                var choosenGame = (GameMenuOption)choosenOption;
                exit = NeatMenuLoop(choosenGame);
            }
        }

        static int ChooseGameMenu(out bool exit)
        {
            exit = false;

            var gameNames = Enum.GetNames(typeof(GameMenuOption));
            var menuNames = gameNames.Select(n => n + " Game").Concat(new string[] { "Exit" }).ToArray();

            int option = ConsoleUtils.OptionsMenu("Please choose a game:", menuNames);
            if (option == menuNames.Length)
                exit = true;

            return option;
        }

        static bool NeatMenuLoop(GameMenuOption gameOption)
        {
            var menuNames = new string[]
            {
                "Start learning session",
                "Load learning session",
                "Load AI player",
                "Play game",
                "Change game",
                "Exit"
            };
            
            bool exit = false;
            bool backToPreviusMenu = false;
            while (!exit && !backToPreviusMenu)
            {
                int option = ConsoleUtils.OptionsMenu("What action to perform:", menuNames);

                if (option < menuNames.Length - 1)
                    HandleNeatOption(option, gameOption);
                else if (option == menuNames.Length)
                    exit = true;
                else
                    backToPreviusMenu = true;
            }

            return exit;
        }        

        private static void HandleNeatOption(int menuOption, GameMenuOption gameOption)
        {
            // Save console window parameters before performing action
            int windowHeight = Console.WindowHeight;
            int windowWidth = Console.WindowWidth;
            int windowLeft = Console.WindowLeft;
            int windowTop = Console.WindowTop;

            Console.Clear();

            switch (menuOption)
            {
                case 1:
                    NeatActions.StartLearningSession(gameOption);
                    break;
                case 2:
                    NeatActions.LoadLearningSession(gameOption);
                    break;
                case 3:
                    NeatActions.LoadAIPlayer(gameOption);
                    break;
                case 4:
                    NeatActions.PlayGame(gameOption);
                    break;
                default:
                    Console.Error.WriteLine("No such option!");
                    break;
            }

            // Restore console window initial parameters
            Console.SetWindowPosition(windowLeft, windowTop);
            Console.SetWindowSize(windowWidth, windowHeight);
        }
    }
}
