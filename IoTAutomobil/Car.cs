using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class Car
    {
        private const double MaxFuelConsumptionPerSecond = 0.005;
        private const int IdleRpm = 800;
        private const int MaxRpm = 6000;
        private const int MinSpeed = 0;
        private const int MaxSpeed = 120;
        private const int UpdateIntervalSeconds = 15;
        private const int TripDurationMinutes = 10;

        private double _currentSpeed = 0;
        private double _currentRpm = IdleRpm;
        private double _currentFuelLevel = 100.0;
        private readonly Random _random = new Random();
        private readonly SendDataService _sendService = new SendDataService();

        public async Task StartSimulationAsync()
        {
            Console.WriteLine("Starting car telemetry simulation...");
            var startTime = DateTime.Now;

            while (true)
            {
                var elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMinutes >= TripDurationMinutes)
                {
                    Console.WriteLine("Simulation finished. Trip duration reached.");
                    break;
                }

                SimulateTrip();

                var data = new SensorData(
                    rpm: (int)Math.Round(_currentRpm),
                    speed: (int)Math.Round(_currentSpeed),
                    fuel: (int)Math.Round(_currentFuelLevel),
                    engineTemperature: (int)Math.Round(GenerateEngineTemperature()),
                    dtc: GenerateDtc()
                );

                Console.WriteLine($"Time: {elapsedTime.TotalSeconds:F0}s | Speed: {_currentSpeed:F0} km/h | RPM: {_currentRpm:F0} | Fuel: {_currentFuelLevel:F1}%");

                await _sendService.SendDataAsync(data);
                await Task.Delay(TimeSpan.FromSeconds(UpdateIntervalSeconds));
            }
        }

        private void SimulateTrip()
        {
            string tripPhase;
            if (_currentSpeed < 30)
            {
                tripPhase = "Accelerating";
            }
            else if (_currentSpeed < 90)
            {
                tripPhase = "Cruising";
            }
            else
            {
                tripPhase = "Decelerating";
            }

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
                    _currentSpeed -= _random.Next(2, 5);
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
            if (_random.NextDouble() < 0.05)
            {
                string[] codes = { "P0300", "P0420", "P0171", "P0455", "P0133" };
                return codes[_random.Next(codes.Length)];
            }
            return string.Empty;
        }
    }
}