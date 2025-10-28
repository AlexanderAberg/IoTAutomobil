using System;

namespace IoTAutomobil
{
    internal class SensorData
    {
        public SensorData(int rpm, int speed, double fuel, int engineTemperature, string dtc)
        {
            Rpm = rpm;
            Speed = speed;
            Fuel = fuel;
            EngineTemperature = engineTemperature;
            Dtc = dtc;
        }

        public int Rpm { get; set; }
        public int Speed { get; set; }
        public double Fuel { get; set; }
        public int EngineTemperature { get; set; }
        public string Dtc { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
    }
}
