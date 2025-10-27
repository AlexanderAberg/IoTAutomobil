using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Menu
    {
        private readonly ThingSpeak thingSpeak;

        public void Run()
        {
            string? input;
            do
            {
                Console.Clear();
                Console.WriteLine("1. Data Analysis");
                Console.WriteLine("0. Exit");
                Console.Write("Choose your option: ");
                input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        var dataAnalysisMenu = new DataAnalysisMenu();
                        dataAnalysisMenu.Run();
                        break;
                    case "0":
                        Console.WriteLine("Goodbye.");
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        Console.WriteLine("Click any key to continue...");
                        Console.ReadKey();
                        break;
                }

            } while (input != "0");
        }
    }

    internal class DataAnalysisMenu
    {
        public DataAnalysisMenu()
        {
            string? input;
            do
            {
                Console.Clear();
                Console.WriteLine("1. Data for last 24 hours");
                Console.WriteLine("2. Data for last 100 data points");
                Console.WriteLine("0. Exit");
                Console.Write("Choose your option: ");
                input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        var dataAnalyzer24h = new DataAnalyzer(24);
                        dataAnalyzer24h.AnalyzeData();
                        break;
                    case "2":
                        var dataAnalyzer100 = new DataAnalyzer(100);
                        dataAnalyzer100.AnalyzeData();
                        break;
                    case "0":
                        Console.WriteLine("Goodbye.");
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        Console.WriteLine("Click any key to continue...");
                        Console.ReadKey();
                        break;
                }

            } while (input != "0");

        }
    }
}
