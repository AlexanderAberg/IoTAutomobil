using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class ThingSpeak
    {
        private static readonly HttpClient s_http = new HttpClient();
        private readonly string _apiKey;
        private readonly string? _channelId;

        public ThingSpeak(string? apiKey = null)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("THINGSPEAK_API_KEY");
            _channelId = _channelId ?? Environment.GetEnvironmentVariable("THINGSPEAK_CHANNEL_ID");
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

            var uri = new UriBuilder("https://api.thingspeak.com/update") { Query = sb.ToString() }.Uri;

            try
            {
                var resp = await s_http.GetStringAsync(uri).ConfigureAwait(false);
                Console.WriteLine($"ThingSpeak update response: {resp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ThingSpeak send failed: {ex.Message}");
            }
        }

        internal async Task<FeedResponse?> GetFeedsAsync(int? results = null, int? days = null)
        {
            if (!int.TryParse(_channelId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var channelId))
            {
                Console.WriteLine("ThingSpeak read: env THINGSPEAK_CHANNEL_ID not set or invalid. Set it to your numeric channel ID.");
                return null;
            }

            var readKey = Environment.GetEnvironmentVariable("THINGSPEAK_READ_KEY");

            var qs = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(readKey))
                qs.Append("api_key=").Append(Uri.EscapeDataString(readKey));

            if (results is int r && r > 0)
                qs.Append(qs.Length > 0 ? "&" : "").Append("results=").Append(r);
            else if (days is int d && d > 0)
                qs.Append(qs.Length > 0 ? "&" : "").Append("days=").Append(d);
            else
                qs.Append(qs.Length > 0 ? "&" : "").Append("results=100");

            var uri = new UriBuilder($"https://api.thingspeak.com/channels/{channelId}/feeds.json")
            {
                Query = qs.ToString()
            }.Uri;

            try
            {
                var masked = uri.ToString().Replace(readKey ?? string.Empty, "****");
                Console.WriteLine($"ThingSpeak read request: {masked}");

                using var resp = await s_http.GetAsync(uri).ConfigureAwait(false);
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ThingSpeak read HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {json}");
                    return null;
                }

                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                return JsonSerializer.Deserialize<FeedResponse>(json, opts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ThingSpeak read failed: {ex.Message}");
                return null;
            }
        }

        internal sealed class FeedResponse
        {
            public Channel? channel { get; set; }
            public FeedEntry[]? feeds { get; set; }
        }

        internal sealed class Channel
        {
            public string? name { get; set; }
            public string? field1 { get; set; }
            public string? field2 { get; set; }
            public string? field3 { get; set; }
            public string? field4 { get; set; }
            public string? field5 { get; set; }
            public string? field6 { get; set; }
        }

        internal sealed class FeedEntry
        {
            public DateTime created_at { get; set; }
            public int entry_id { get; set; }
            public string? field1 { get; set; }
            public string? field2 { get; set; }
            public string? field3 { get; set; }
            public string? field4 { get; set; }
            public string? field5 { get; set; }
            public string? field6 { get; set; }
        }
    }
}