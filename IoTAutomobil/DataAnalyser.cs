using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal sealed class DataAnalyser
    {
        private readonly ThingSpeak _ts = new ThingSpeak();

        public async Task AnalyzeLast24hAsync()
        {
            var feed = await _ts.GetFeedsAsync(days: 1);
            PrintSummary("last 24 hours", feed);
        }

        public async Task AnalyzeLastNAsync(int results)
        {
            var feed = await _ts.GetFeedsAsync(results: results);
            PrintSummary($"last {results} entries", feed);
        }

        private static double ParseDouble(string? s)
            => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

        private static int ParseInt(string? s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

        private static void PrintSummary(string label, ThingSpeak.FeedResponse? feed)
        {
            if (feed?.feeds is null || feed.feeds.Length == 0)
            {
                Console.WriteLine($"No data available for {label}.");
                return;
            }

            var entries = feed.feeds;

            var rpms = entries.Select(f => ParseInt(f.field1)).Where(v => v > 0).ToArray();
            var speeds = entries.Select(f => ParseInt(f.field2)).Where(v => v >= 0).ToArray();
            var fuels = entries.Select(f => ParseDouble(f.field3)).Where(v => !double.IsNaN(v)).ToArray();
            var temps = entries.Select(f => ParseInt(f.field4)).Where(v => v > 0).ToArray();
            var dtcs = entries.Select(f => f.field5).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            var coords = entries
                .Select(f => (lat: ParseDouble(f.field7), lon: ParseDouble(f.field8), t: f.created_at))
                .Where(x => !double.IsNaN(x.lat) && !double.IsNaN(x.lon))
                .OrderBy(x => x.t)
                .ToArray();

            Console.WriteLine($"=== ThingSpeak summary for {label} ===");
            Console.WriteLine($"Entries: {entries.Length}");
            Console.WriteLine($"Time range: {entries.First().created_at:u} -> {entries.Last().created_at:u}");

            if (rpms.Length > 0)
                Console.WriteLine($"RPM avg: {rpms.Average():F0}, min: {rpms.Min()}, max: {rpms.Max()}");

            if (speeds.Length > 0)
                Console.WriteLine($"Speed avg: {speeds.Average():F1} km/h, min: {speeds.Min()}, max: {speeds.Max()}");

            if (fuels.Length > 0)
                Console.WriteLine($"Fuel avg: {fuels.Average():F2} %, min: {fuels.Min():F2} %, max: {fuels.Max():F2} %");

            if (temps.Length > 0)
                Console.WriteLine($"Engine temp avg: {temps.Average():F1} °C, min: {temps.Min()} °C, max: {temps.Max()} °C");

            Console.WriteLine($"DTC entries: {dtcs.Length}" + (dtcs.Length > 0 ? $", last: {dtcs.Last()}" : ""));

            if (coords.Length >= 2)
            {
                var totalMeters = 0.0;
                for (int i = 1; i < coords.Length; i++)
                    totalMeters += HaversineMeters(coords[i - 1].lat, coords[i - 1].lon, coords[i].lat, coords[i].lon);

                var first = coords.First();
                var last  = coords.Last();

                Console.WriteLine($"GPS points: {coords.Length}");
                Console.WriteLine($"Start:  ({first.lat:F6}, {first.lon:F6}) at {first.t:u}");
                Console.WriteLine($"Finish: ({last.lat:F6}, {last.lon:F6}) at {last.t:u}");
                Console.WriteLine($"Path length: {totalMeters/1000.0:F3} km");
            }
            else
            {
                Console.WriteLine("GPS: insufficient data points.");
            }
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            static double DegToRad(double d) => d * Math.PI / 180.0;
            var dLat = DegToRad(lat2 - lat1);
            var dLon = DegToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegToRad(lat1)) * Math.Cos(DegToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}