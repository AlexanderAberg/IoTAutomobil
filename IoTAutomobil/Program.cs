namespace IoTAutomobil
{
    internal class Program
    {
        static void Main(string[] args)
        {
        // https://api.thingspeak.com/update?api_key=FKN4YZGI0E2HX5SW&field1=0

            while (true)
            {
                var sensorData = new SensorData(new Random().Next(800, 6000),
                    new Random().Next(0, 120),
                    new Random().Next(0, 100),
                    new Random().Next(80, 100),
                    "P" + new Random().Next(https://club.autodoc.se/obd-codes/all).ToString("D4")
                );
                var sendDataService = new SendDataService();
                sendDataService.SendData(sensorData);

                System.Threading.Thread.Sleep(15000);
            }
        }
    }
}
