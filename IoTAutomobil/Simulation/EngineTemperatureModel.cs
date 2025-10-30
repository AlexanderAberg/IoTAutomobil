using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTAutomobil.Simulation
{
    internal sealed class EngineTemperatureModel
    {
        private readonly double _min;
        private readonly double _max;
        private readonly double _tauSeconds;
        private readonly double _highSpeedThresholdRatio;
        private readonly double _highSpeedSustainSeconds;
        private readonly double _highSpeedBonusMax;
        private readonly double _jitter;
        private readonly Random _rand;

        private double _temp;
        private double _highSpeedAccumSeconds;

        public EngineTemperatureModel(
            double min = 80, double max = 100, double tauSeconds = 30,
            double highSpeedThresholdRatio = 0.95, double highSpeedSustainSeconds = 30,
            double highSpeedBonusMax = 2, double jitter = 0.2, double initial = 80,
            Random? rand = null)
        {
            _min = min; _max = max; _tauSeconds = tauSeconds;
            _highSpeedThresholdRatio = highSpeedThresholdRatio;
            _highSpeedSustainSeconds = highSpeedSustainSeconds;
            _highSpeedBonusMax = highSpeedBonusMax;
            _jitter = jitter;
            _temp = initial;
            _rand = rand ?? new Random();
        }

        public int Update(double speedKmh, double rpm, double idleRpm, double maxRpm, double maxSpeedKmh, double dtSeconds)
        {
            var speedFrac = Math.Clamp(speedKmh / maxSpeedKmh, 0.0, 1.0);
            var loadFrac = Math.Clamp((rpm - idleRpm) / Math.Max(1.0, maxRpm - idleRpm), 0.0, 1.0);

            var highSpeed = speedFrac >= _highSpeedThresholdRatio;
            if (highSpeed)
                _highSpeedAccumSeconds = Math.Min(_highSpeedSustainSeconds, _highSpeedAccumSeconds + dtSeconds);
            else
                _highSpeedAccumSeconds = Math.Max(0.0, _highSpeedAccumSeconds - dtSeconds * 0.5);

            var highSpeedBonus = _highSpeedBonusMax * (_highSpeedAccumSeconds / _highSpeedSustainSeconds);

            var target = 82.0 + 10.0 * speedFrac + 6.0 * loadFrac + highSpeedBonus;
            target = Math.Clamp(target, _min, _max);

            var alpha = 1.0 - Math.Exp(-dtSeconds / _tauSeconds);
            _temp += (target - _temp) * alpha;

            _temp += (_rand.NextDouble() - 0.5) * 2.0 * _jitter;
            _temp = Math.Clamp(_temp, _min, _max);

            return (int)Math.Round(_temp);
        }
    }
}
