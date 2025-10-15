namespace IoTAutomobil
{
    internal class Program
    {
        static void Main(string[] args)
        {
        https://api.thingspeak.com/update?api_key=FKN4YZGI0E2HX5SW&field1=0

            while (true)
            {
                var thingSpeak = new ThingSpeak();
                int rpm = new Random().Next(800, 6000);
                int speed = new Random().Next(0, 120);
                int fuel = new Random().Next(0, 100);
                int engineTemperature = new Random().Next(80, 100);
              //  string dtc = "P" + new Random().Next("https://club.autodoc.se/obd-codes/all).ToString"();

                System.Threading.Thread.Sleep(15000);
            }
        }
    }
}
