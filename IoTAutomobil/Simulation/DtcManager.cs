using System;

namespace IoTAutomobil.Simulation
{
    internal sealed class DtcManager
    {
        private readonly TimeSpan _duration;
        private readonly double _probPerTick;
        private readonly TimeSpan _cooldown;

        private string? _active;
        private DateTime _clearAt = DateTime.MinValue;
        private DateTime _nextAllowedAt = DateTime.MinValue;
        private DateTime _plannedAt = DateTime.MaxValue;

        public DtcManager(TimeSpan duration, double probabilityPerTick, TimeSpan cooldown)
        {
            _duration = duration;
            _probPerTick = probabilityPerTick;
            _cooldown = cooldown;
        }

        public void Plan(DateTime plannedAt) => _plannedAt = plannedAt;

        public string Tick(Func<string> pickRandomDtc, Action<string, string>? onGenerated = null)
        {
            var now = DateTime.Now;

            if (!string.IsNullOrEmpty(_active))
            {
                if (now < _clearAt) return _active;
                _active = null;
                _nextAllowedAt = now.Add(_cooldown);
                return string.Empty;
            }

            if (now < _nextAllowedAt) return string.Empty;

            if (now >= _plannedAt)
            {
                _active = pickRandomDtc();
                _clearAt = now.Add(_duration);
                onGenerated?.Invoke("scheduled", _active);
                return _active;
            }

            if (_probPerTick > 0 && new Random().NextDouble() < _probPerTick)
            {
                _active = pickRandomDtc();
                _clearAt = now.Add(_duration);
                onGenerated?.Invoke("random", _active);
                return _active;
            }

            return string.Empty;
        }
    }
}