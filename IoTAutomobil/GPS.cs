using System;

namespace IoTAutomobil
{
    internal class GPS
    {
        // <iframe src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d4066.429275948711!2d17.969560776157596!3d59.36275660834567!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x465f9de741ccc1ff%3A0xab6669fe29e893ed!2sRosengatan%208%2C%20172%2070%20Sundbyberg!5e0!3m2!1ssv!2sse!4v1761677602119!5m2!1ssv!2sse" width="600" height="450" style="border:0;" allowfullscreen="" loading="lazy" referrerpolicy="no-referrer-when-downgrade"></iframe>
        public const double DefaultStartLatitude = 59.362893293655716;
        public const double DefaultStartLongitude = 17.972157154401952;

        private const double EarthRadiusMeters = 6371000.0;

        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public double Altitude { get; init; }
        public double SpeedKmh { get; init; }
        public double Heading { get; init; }
        public DateTime Timestamp { get; init; }

        public static GPS CreateStart(double speedKmh = 0, double heading = 0, double altitude = 0, DateTime? timestampUtc = null)
            => new GPS
            {
                Latitude = DefaultStartLatitude,
                Longitude = DefaultStartLongitude,
                Altitude = altitude,
                SpeedKmh = speedKmh,
                Heading = NormalizeHeading(heading),
                Timestamp = timestampUtc ?? DateTime.UtcNow
            };

        public GPS Moved(double distanceMeters, double headingDegrees, double? speedKmh = null, double? altitude = null, DateTime? timestampUtc = null)
        {
            if (distanceMeters < 0) throw new ArgumentOutOfRangeException(nameof(distanceMeters));
            var headingRad = DegToRad(NormalizeHeading(headingDegrees));
            var lat1 = DegToRad(Latitude);
            var lon1 = DegToRad(Longitude);
            var angularDistance = distanceMeters / EarthRadiusMeters;

            var sinLat1 = Math.Sin(lat1);
            var cosLat1 = Math.Cos(lat1);
            var sinAd = Math.Sin(angularDistance);
            var cosAd = Math.Cos(angularDistance);
            var sinHeading = Math.Sin(headingRad);
            var cosHeading = Math.Cos(headingRad);

            var sinLat2 = sinLat1 * cosAd + cosLat1 * sinAd * cosHeading;
            var lat2 = Math.Asin(sinLat2);

            var y = sinHeading * sinAd * cosLat1;
            var x = cosAd - sinLat1 * sinLat2;
            var lon2 = lon1 + Math.Atan2(y, x);

            var newLat = RadToDeg(lat2);
            var newLon = NormalizeLongitude(RadToDeg(lon2));

            var newSpeed = speedKmh ?? SpeedKmh;
            var newHeading = NormalizeHeading(headingDegrees);
            var newAltitude = altitude ?? Altitude;

            DateTime newTimestamp;
            if (timestampUtc.HasValue)
            {
                newTimestamp = timestampUtc.Value;
            }
            else if (newSpeed > 0)
            {
                var mps = newSpeed / 3.6;
                var seconds = distanceMeters / mps;
                newTimestamp = Timestamp.AddSeconds(seconds);
            }
            else
            {
                newTimestamp = DateTime.UtcNow;
            }

            return new GPS
            {
                Latitude = newLat,
                Longitude = newLon,
                Altitude = newAltitude,
                SpeedKmh = newSpeed,
                Heading = newHeading,
                Timestamp = newTimestamp
            };
        }

        public override string ToString()
            => $"Lat: {Latitude:F6}, Lon: {Longitude:F6}, Alt: {Altitude:F1} m, Speed: {SpeedKmh:F1} km/h, Heading: {Heading:F1}°, Time: {Timestamp:u}";

        private static double NormalizeLongitude(double lon)
        {
            lon = (lon + 540) % 360 - 180;
            return lon;
        }

        private static double NormalizeHeading(double heading)
        {
            heading %= 360;
            if (heading < 0) heading += 360;
            return heading;
        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;
        private static double RadToDeg(double rad) => rad * 180.0 / Math.PI;
    }
}
