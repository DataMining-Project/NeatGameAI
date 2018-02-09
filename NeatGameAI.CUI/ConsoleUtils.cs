using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeatGameAI.CUI
{
    public static class ConsoleUtils
    {
        public static int OptionsMenu(string menuTitle, string[] menuNames, bool clearConsole = true)
        {
            if (clearConsole)
                Console.Clear();

            int cursorLine = Console.CursorTop;

            int option = 0;
            while (option == 0)
            {
                Console.WriteLine(menuTitle);
                for (int i = 0; i < menuNames.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {menuNames[i]}");
                }
                Console.Write("Choice: ");
                string choice = Console.ReadLine();

                if (clearConsole)
                    Console.Clear();
                else
                    ClearLines(cursorLine, menuNames.Length + 3);

                if (int.TryParse(choice, out int intVal))
                {
                    if (intVal > 0 && intVal <= menuNames.Length)
                    {
                        option = intVal;
                    }
                    else Console.Error.WriteLine("No such option!");
                }
                else Console.Error.WriteLine("Invalid input!");
            }

            return option;
        }

        public static void ClearLines(int startLine, int linesCount, int newCursorLine = -1)
        {
            if (newCursorLine == -1)
                newCursorLine = startLine;

            Console.SetCursorPosition(0, startLine);

            for (int i = 0; i < linesCount; i++)
            {
                Console.Write(new string(' ', Console.WindowWidth));
            }

            Console.SetCursorPosition(0, newCursorLine);
        }

        public static void PressAnyKeyToContinue()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public static bool YesNoOptionMenu(string question)
        {
            var menuNames = new string[] { "Yes", "No" };

            int option = OptionsMenu(question, menuNames, false);

            return option == 1;
        }

        public static void FlushKeys()
        {
            while (Console.KeyAvailable)
                Console.ReadKey();
        }
    }
}
