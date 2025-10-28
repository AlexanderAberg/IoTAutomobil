using System;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Menu
    {
        public async Task RunAsync()
        {
            string? input;
            do
            {
                Console.Clear();
                Console.WriteLine("***** IoTAutomobil *****");
                Console.WriteLine("1. Data Analysis");
                Console.WriteLine("2. Start Simulation");
                Console.WriteLine("0. Exit");
                Console.Write("Choose your option: ");
                input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        var dataAnalysisMenu = new DataAnalysisMenu();
                        await dataAnalysisMenu.RunAsync();
                        break;

                    case "2":
                        var car = new Car();
                        await car.StartSimulationAsync();
                        Console.WriteLine("Simulation finished. Press any key to return to menu...");
                        Console.ReadKey();
                        break;

                    case "0":
                        Console.WriteLine("Goodbye.");
                        break;

                    default:
                        Console.WriteLine("Invalid choice.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                }

            } while (input != "0");
        }
    }

    internal class DataAnalysisMenu
    {
        public async Task RunAsync()
        {
            string? input;
            do
            {
                Console.Clear();
                Console.WriteLine("=== Data Analysis ===");
                Console.WriteLine("1. Data for last 24 hours");
                Console.WriteLine("2. Data for last 100 data points");
                Console.WriteLine("0. Back");
                Console.Write("Choose your option: ");
                input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await new DataAnalyser().AnalyzeLast24hAsync();
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;

                    case "2":
                        await new DataAnalyser().AnalyzeLastNAsync(100);
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;

                    case "0":
                        break;

                    default:
                        Console.WriteLine("Invalid choice.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                }

            } while (input != "0");
        }
    }
}
