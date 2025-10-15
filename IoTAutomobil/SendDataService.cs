using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTAutomobil
{
    internal class SendDataService
    {
        internal void SendData(SensorData sensorData)
        {
            var thingSpeak = new ThingSpeak();
            thingSpeak.SendData(sensorData);
        }
    }
}
