using System;
using System.IO;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Car
    {
        private const double MaxFuelConsumptionPerSecond = 0.05;
        private const int IdleRpm = 800;
        private const int MaxRpm = 6000;
        private const int MinSpeed = 0;
        private const int MaxSpeed = 120;
        private const int UpdateIntervalSeconds = 5;
        private const int TripDurationMinutes = 10;

        private const double DtcProbabilityPerTick = 0.05;
        private static readonly TimeSpan DtcDuration = TimeSpan.FromMinutes(2);

        private double _currentSpeed = 0;
        private double _currentRpm = IdleRpm;
        private double _currentFuelLevel = 100.0;
        private readonly Random _random = new Random();
        private readonly SendDataService _sendService = new SendDataService();

        private readonly IDtcProvider _dtcPrimary;
        private readonly IDtcProvider _dtcFallback;

        private readonly IDtcInfoProvider _dtcInfo;

        private string? _activeDtc = null;
        private DateTime _activeDtcClearAt = DateTime.MinValue;
        private DateTime _plannedDtcAt = DateTime.MaxValue;
        private DateTime _simulationStart;

        private static bool DtcDebug => string.Equals(Environment.GetEnvironmentVariable("DTC_INFO_DEBUG"), "1", StringComparison.OrdinalIgnoreCase);

        public Car(IDtcProvider? dtcProvider = null)
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            var csvPath = Path.Combine(dataDir, "dtc-codes.csv");

            _dtcPrimary = dtcProvider ?? new CsvDtcProvider(csvPath, _random);
            _dtcFallback = new FallbackDtcProvider(_random);

            // Consistent cache locations used by the web info provider
            var listCachePath = Path.Combine(dataDir, "dtc-list.cache.html");
            var codeCacheDir = Path.Combine(dataDir, "dtc-cache");

            _dtcInfo = new ChainDtcInfoProvider(
                new WebDtcInfoProvider(
                    listUrl: "https://club.autodoc.se/obd-codes/all",
                    listCachePath: listCachePath,
                    cacheMaxAge: TimeSpan.FromDays(7),
                    codePageTemplates: new[]
                    {
                        "https://club.autodoc.se/obd-codes/{code}",
                        "https://club.autodoc.se/obd-code/{code}",
                        "https://club.autodoc.se/koder/{code}"
                    },
                    codeCacheDir: codeCacheDir
                ),
                new CsvDtcInfoProvider(csvPath),
                new HeuristicDtcInfoProvider()
            );

            if (DtcDebug)
            {
                Console.WriteLine($"[DTC] BaseDir: {AppContext.BaseDirectory}");
                Console.WriteLine($"[DTC] List cache: {listCachePath}");
                Console.WriteLine($"[DTC] Code cache dir: {codeCacheDir}");
                // Optional probe to create caches immediately
                _dtcInfo.TryGetInfo("P0171", out _);
            }
        }

        public async Task StartSimulationAsync()
        {
            Console.WriteLine("Starting car telemetry simulation...");
            _simulationStart = DateTime.Now;

            var earliest = _simulationStart.AddMinutes(1);
            var latest = _simulationStart.AddMinutes(TripDurationMinutes - 1);
            if (latest <= earliest)
            {
                latest = _simulationStart.AddMinutes(TripDurationMinutes);
                earliest = _simulationStart.AddSeconds(30);
            }
            var secondsWindow = Math.Max(1, (int)(latest - earliest).TotalSeconds);
            _plannedDtcAt = earliest.AddSeconds(_random.Next(secondsWindow));

            while (true)
            {
                var elapsedTime = DateTime.Now - _simulationStart;
                if (elapsedTime.TotalMinutes >= TripDurationMinutes)
                {
                    Console.WriteLine("Simulation finished. Trip duration reached.");
                    break;
                }

                SimulateTrip();

                var dtc = GenerateDtc();

                var data = new SensorData(
                    rpm: (int)Math.Round(_currentRpm),
                    speed: (int)Math.Round(_currentSpeed),
                    fuel: Math.Round(_currentFuelLevel, 2),
                    engineTemperature: (int)Math.Round(GenerateEngineTemperature()),
                    dtc: dtc
                );

                var dtcPart = "";
                if (!string.IsNullOrEmpty(dtc))
                {
                    if (_dtcInfo.TryGetInfo(dtc, out var info) && !string.IsNullOrWhiteSpace(info.Title))
                        dtcPart = $" | DTC: {dtc} - {info.Title} (More: {info.Url})";
                    else
                        dtcPart = $" | DTC: {dtc} (More: https://club.autodoc.se/obd-codes/all)";
                }

                Console.WriteLine($"Time: {elapsedTime.TotalSeconds:F0}s | Speed: {_currentSpeed:F0} km/h | RPM: {_currentRpm:F0} | Fuel: {_currentFuelLevel:F1}%{dtcPart}");

                await _sendService.SendDataAsync(data);
                await Task.Delay(TimeSpan.FromSeconds(UpdateIntervalSeconds));
            }
        }

        private void SimulateTrip()
        {
            string tripPhase;
            if (_currentSpeed < 90) tripPhase = "Accelerating";
            else if (_currentSpeed < 100) tripPhase = "Cruising";
            else tripPhase = "Decelerating";

            switch (tripPhase)
            {
                case "Accelerating":
                    _currentSpeed += _random.Next(2, 5);
                    _currentRpm = CalculateRpm(_currentSpeed);
                    break;
                case "Cruising":
                    _currentSpeed += _random.NextDouble() * 2 - 1;
                    _currentRpm = CalculateRpm(_currentSpeed);
                    break;
                case "Decelerating":
                    _currentSpeed -= _random.Next(1, 4);
                    _currentRpm = CalculateRpm(_currentSpeed);
                    break;
            }

            _currentSpeed = Math.Max(MinSpeed, Math.Min(MaxSpeed, _currentSpeed));
            _currentRpm = Math.Max(IdleRpm, _currentRpm);

            double consumptionRate = (_currentSpeed / MaxSpeed) * (_currentRpm / MaxRpm) * MaxFuelConsumptionPerSecond;
            _currentFuelLevel -= consumptionRate * UpdateIntervalSeconds;
            _currentFuelLevel = Math.Max(0, _currentFuelLevel);
        }

        private double CalculateRpm(double speed)
        {
            if (speed < 1) return IdleRpm;
            return IdleRpm + (speed / MaxSpeed) * (MaxRpm - IdleRpm);
        }

        private double GenerateEngineTemperature()
        {
            double baseTemp = 85;
            double speedFactor = _currentSpeed / MaxSpeed;
            double temp = baseTemp + (speedFactor * _random.Next(0, 15));
            return Math.Max(80, Math.Min(110, temp));
        }

        private string GenerateDtc()
        {
            var now = DateTime.Now;

            if (!string.IsNullOrEmpty(_activeDtc))
            {
                if (now < _activeDtcClearAt) return _activeDtc;

                Console.WriteLine($"DTC cleared: {_activeDtc}");
                _activeDtc = null;
            }

            if (_activeDtc is null && now >= _plannedDtcAt)
            {
                _activeDtc = PickRandomDtc();
                _activeDtcClearAt = now.Add(DtcDuration);
                LogDtcGenerated("scheduled", _activeDtc);
                return _activeDtc;
            }

            if (_activeDtc is null && _random.NextDouble() < DtcProbabilityPerTick)
            {
                _activeDtc = PickRandomDtc();
                _activeDtcClearAt = now.Add(DtcDuration);
                LogDtcGenerated("random", _activeDtc);
                return _activeDtc;
            }

            return string.Empty;
        }

        private void LogDtcGenerated(string kind, string code)
        {
            if (_dtcInfo.TryGetInfo(code, out var info) && !string.IsNullOrWhiteSpace(info.Title))
                Console.WriteLine($"DTC generated ({kind}): {code} - {info.Title} (More: {info.Url})");
            else
                Console.WriteLine($"DTC generated ({kind}): {code} (More: https://club.autodoc.se/obd-codes/all)");
        }

        private string PickRandomDtc()
        {
            if (_dtcPrimary.TryGetRandom(out var code)) return code;
            _dtcFallback.TryGetRandom(out var fallback);
            return fallback;
        }
    }
}