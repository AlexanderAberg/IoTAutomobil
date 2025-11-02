using System;

namespace IoTAutomobil
{
    internal class SensorData(int rpm, int speed, double fuel, int engineTemperature, string dtc)
    {
        public int Rpm { get; set; } = rpm;
        public int Speed { get; set; } = speed;
        public double Fuel { get; set; } = fuel;
        public int EngineTemperature { get; set; } = engineTemperature;
        public string Dtc { get; set; } = dtc;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
    }
}
