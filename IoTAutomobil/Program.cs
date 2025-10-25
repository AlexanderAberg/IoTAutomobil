using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // https://api.thingspeak.com/update?api_key=FKN4YZGI0E2HX5SW&field1=0
            // https://club.autodoc.se/obd-codes/all

            var car = new Car();
            await car.StartSimulationAsync();
        }
    }
}
