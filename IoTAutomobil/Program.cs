using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Program
    {
        static async Task Main()
        {
            await Menu.RunAsync(new DataAnalysisMenu());
        }
    }
}
