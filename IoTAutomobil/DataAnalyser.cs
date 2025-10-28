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
        }
    }
}