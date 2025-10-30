using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTAutomobil.Data
{
    internal class EngineTemperatureModel
    {
        public EngineTemperatureModel()
        {
            double speedFrac = Math.Clamp(_currentSpeed / MaxSpeed, 0.0, 1.0);
            double loadFrac = Math.Clamp((_currentRpm - IdleRpm) / (double)(MaxRpm - IdleRpm), 0.0, 1.0);

            bool highSpeed = speedFrac >= HighSpeedThresholdRatio;
            if (highSpeed)
                _highSpeedAccumSeconds = Math.Min(HighSpeedSustainSeconds, _highSpeedAccumSeconds + UpdateIntervalSeconds);
            else
                _highSpeedAccumSeconds = Math.Max(0.0, _highSpeedAccumSeconds - UpdateIntervalSeconds * 0.5);

            double highSpeedBonus = HighSpeedBonusMax * (_highSpeedAccumSeconds / HighSpeedSustainSeconds);

            double target = 82.0 + 10.0 * speedFrac + 6.0 * loadFrac + highSpeedBonus;
            target = Math.Clamp(target, EngineTempMin, EngineTempMax);

            double dt = UpdateIntervalSeconds;
            double alpha = 1.0 - Math.Exp(-dt / ThermalTauSeconds);
            _engineTemp += (target - _engineTemp) * alpha;

            _engineTemp += (_random.NextDouble() - 0.5) * 2.0 * TempJitter;

            _engineTemp = Math.Clamp(_engineTemp, EngineTempMin, EngineTempMax);

            return _engineTemp;
        }
    }
}
