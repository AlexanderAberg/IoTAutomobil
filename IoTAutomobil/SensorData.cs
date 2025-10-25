using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
