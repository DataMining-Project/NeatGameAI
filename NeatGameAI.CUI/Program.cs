using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeatGameAI.CUI
{
    class Program
    {
        static void Main(string[] args)
        {
            var bl = new BreakoutLearner();

            bl.Learn(100000);

            Console.ReadKey();
        }
    }
}
