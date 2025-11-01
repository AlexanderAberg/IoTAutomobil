using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IoTAutomobil.DTC
{
    internal sealed class ChainDtcInfoProvider : IDtcInfoProvider
    {
        private readonly IDtcInfoProvider[] _providers;
        private readonly bool _debug =
            string.Equals(Environment.GetEnvironmentVariable("DTC_INFO_DEBUG"), "1", StringComparison.OrdinalIgnoreCase);

        public ChainDtcInfoProvider(params IDtcInfoProvider[] providers) => _providers = providers;

        public bool TryGetInfo(string code, out DtcInfo info)
        {
            foreach (var p in _providers)
            {
                if (p.TryGetInfo(code, out info))
                {
                    if (_debug)
                        Console.WriteLine($"[DTC] Info from {p.GetType().Name} for {code}: {info.Title ?? "(no title)"}");
                    return true;
                }
            }
            info = new DtcInfo(code, null, DtcInfoUtil.BuildInfoUrl(code));
            if (_debug)
                Console.WriteLine($"[DTC] Info not found in providers for {code}. Using fallback URL only.");
            return false;
        }
    }

    internal sealed class CsvDtcInfoProvider : IDtcInfoProvider
    {
        private readonly Dictionary<string, string> _map;

        public CsvDtcInfoProvider(string filePath)
        {
            _map = LoadDescriptions(filePath);
        }

        private static Dictionary<string, string> LoadDescriptions(string filePath)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(filePath)) return dict;

                var rx = new Regex(@"^\s*(?<code>[PBCU]\d{4})\s*(?:[,;\t]\s*(?<desc>.+))?$", RegexOptions.IgnoreCase);

                foreach (var raw in File.ReadLines(filePath))
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var m = rx.Match(line);
                    if (!m.Success) continue;

                    var code = m.Groups["code"].Value.ToUpperInvariant();
                    var desc = m.Groups["desc"].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(desc))
                        dict[code] = desc;
                }
            }
            catch { }
            return dict;
        }

        public bool TryGetInfo(string code, out DtcInfo info)
        {
            var up = code.ToUpperInvariant();
            if (_map.TryGetValue(up, out var desc))
            {
                info = new DtcInfo(up, desc, DtcInfoUtil.BuildInfoUrl(up));
                return true;
            }
            info = new DtcInfo(up, null, DtcInfoUtil.BuildInfoUrl(up));
            return false;
        }
    }

    internal sealed class HeuristicDtcInfoProvider : IDtcInfoProvider
    {
        public bool TryGetInfo(string code, out DtcInfo info)
        {
            var up = code.ToUpperInvariant();
            if (!Regex.IsMatch(up, @"^[PBCU]\d{4}$"))
            {
                info = new DtcInfo(up, null, DtcInfoUtil.BuildInfoUrl(up));
                return false;
            }

            var system = up[0] switch
            {
                'P' => "Powertrain",
                'B' => "Body",
                'C' => "Chassis",
                'U' => "Network",
                _ => "Unknown"
            };

            var genericity = up[1] switch
            {
                '0' => "SAE generic",
                '1' or '2' or '3' => "manufacturer-specific",
                _ => "unspecified"
            };

            var subsystem = up[2] switch
            {
                '0' or '1' => "Fuel and Air Metering",
                '2' => "Fuel and Air Metering (Injector Circuit)",
                '3' => "Ignition System or Misfire",
                '4' => "Auxiliary Emission Controls",
                '5' => "Vehicle Speed and Idle Control",
                '6' => "Computer Output Circuit",
                '7' or '8' => "Transmission",
                _ => "Subsystem"
            };

            var title = $"{system} - {subsystem} ({genericity})";
            info = new DtcInfo(up, title, DtcInfoUtil.BuildInfoUrl(up));
            return true;
        }
    }
}