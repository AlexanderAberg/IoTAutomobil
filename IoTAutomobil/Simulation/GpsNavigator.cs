using System;

namespace IoTAutomobil.Simulation
{
    internal sealed class GpsNavigator(GPS? start = null, double initialHeadingDeg = 90, Random? rand = null)
    {
        private GPS _gps = start ?? GPS.CreateStart();
        private double _headingDeg = initialHeadingDeg;
        private readonly Random _rand = rand ?? new Random();

        public GPS Current => _gps;

        public GPS Update(double speedKmh, double dtSeconds, double headingDriftRangeDeg = 10)
        {
            var distanceMeters = Math.Max(0, speedKmh) / 3.6 * dtSeconds;

            _headingDeg += (_rand.NextDouble() * headingDriftRangeDeg) - (headingDriftRangeDeg / 2.0);
            if (_headingDeg < 0) _headingDeg += 360;
            if (_headingDeg >= 360) _headingDeg -= 360;

            _gps = _gps.Moved(distanceMeters, _headingDeg, speedKmh: speedKmh, altitude: _gps.Altitude, timestampUtc: DateTime.UtcNow);
            return _gps;
        }
    }
}