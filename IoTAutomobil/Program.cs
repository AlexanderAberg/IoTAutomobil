using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // https://club.autodoc.se/obd-codes/all

            var car = new Car();
            await car.StartSimulationAsync();
        }
    }
}
