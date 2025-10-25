using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class ThingSpeak
    {
        private static readonly HttpClient s_http = new HttpClient();
        private readonly string _apiKey;

        public ThingSpeak(string? apiKey = null)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("THINGSPEAK_API_KEY");
        }

        internal async Task SendDataAsync(SensorData sensorData)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.WriteLine("ThingSpeak API key not set (env THINGSPEAK_API_KEY). Skipping send.");
                return;
            }

            var uri = new UriBuilder("https://api.thingspeak.com/update")
            {
                Query =
                    $"api_key={Uri.EscapeDataString(_apiKey)}" +
                    $"&field1={sensorData.Rpm}" +
                    $"&field2={sensorData.Speed}" +
                    $"&field3={sensorData.Fuel}" +
                    $"&field4={sensorData.EngineTemperature}" +
                    $"&field5={Uri.EscapeDataString(sensorData.Dtc ?? string.Empty)}"
            }.Uri;

            try
            {
                var resp = await s_http.GetStringAsync(uri);
                Console.WriteLine($"ThingSpeak update response: {resp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ThingSpeak send failed: {ex.Message}");
            }
        }
    }
}