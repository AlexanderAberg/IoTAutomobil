using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IoTAutomobil.DTC
{
    internal sealed class CsvDtcProvider : IDtcProvider
    {
        private readonly string[] _codes;
        private readonly Random _random;

        public CsvDtcProvider(string filePath, Random random)
        {
            _random = random;
            _codes = Load(filePath);
        }

        private static string[] Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return Array.Empty<string>();

                return File.ReadLines(filePath)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
                    .Select(line =>
                    {
                        var match = Regex.Match(line, @"\b([PBCU]\d{4})\b", RegexOptions.IgnoreCase);
                        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
                    })
                    .Where(code => code is not null)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public bool TryGetRandom(out string code)
        {
            if (_codes.Length == 0)
            {
                code = string.Empty;
                return false;
            }
            code = _codes[_random.Next(_codes.Length)];
            return true;
        }
    }

    internal sealed class FallbackDtcProvider : IDtcProvider
    {
        private readonly string[] _codes = { "P0300", "P0420", "P0171", "P0455", "P0133" };
        private readonly Random _random;

        public FallbackDtcProvider(Random random) => _random = random;

        public bool TryGetRandom(out string code)
        {
            code = _codes[_random.Next(_codes.Length)];
            return true;
        }
    }
}