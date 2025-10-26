using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
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

            bool hasDtc = !string.IsNullOrWhiteSpace(sensorData.Dtc);

            var sb = new StringBuilder();
            sb.Append("api_key=").Append(Uri.EscapeDataString(_apiKey));
            sb.Append("&field1=").Append(sensorData.Rpm);
            sb.Append("&field2=").Append(sensorData.Speed);
            sb.Append("&field3=").Append(sensorData.Fuel.ToString(CultureInfo.InvariantCulture));
            sb.Append("&field4=").Append(sensorData.EngineTemperature);

            if (hasDtc)
                sb.Append("&field5=").Append(Uri.EscapeDataString(sensorData.Dtc));

            sb.Append("&field6=").Append(hasDtc ? "1" : "0");

            sb.Append("&status=").Append(Uri.EscapeDataString(hasDtc ? $"DTC: {sensorData.Dtc}" : "OK"));

            var query = sb.ToString();
            var uri = new UriBuilder("https://api.thingspeak.com/update") { Query = query }.Uri;

            try
            {
                Console.WriteLine($"ThingSpeak request: {query.Replace(_apiKey, "****")}");
                var resp = await s_http.GetStringAsync(uri).ConfigureAwait(false);
                Console.WriteLine($"ThingSpeak update response: {resp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ThingSpeak send failed: {ex.Message}");
            }
        }
    }
}