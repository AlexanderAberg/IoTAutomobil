using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var menu = new Menu();
            await menu.RunAsync();
        }
    }
}
