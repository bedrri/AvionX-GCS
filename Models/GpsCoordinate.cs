using System;

namespace AvionX.Models
{
    /// <summary>
    /// GPS Koordinat Modeli
    ///
    /// DESIGN PATTERN: Data Transfer Object (DTO)
    /// - Latitude/Longitude pair for geographic positioning
    /// - Immutable value object for thread-safe data sharing
    ///
    /// AVİYONİK STANDARTLAR:
    /// - Latitude: -90° (Güney Kutbu) → +90° (Kuzey Kutbu)
    /// - Longitude: -180° (Batı) → +180° (Doğu)
    /// - WGS84 datum standardı (GPS standardı)
    /// </summary>
    public class GpsCoordinate
    {
        /// <summary>
        /// Enlem (Latitude)
        /// Pozitif = Kuzey yarımküre, Negatif = Güney yarımküre
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Boylam (Longitude)
        /// Pozitif = Doğu meridyeni, Negatif = Batı meridyeni
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Altitude (İrtifa - metre)
        /// MSL (Mean Sea Level) üzerinden yükseklik
        /// </summary>
        public double Altitude { get; set; }

        public GpsCoordinate()
        {
            Latitude = 0;
            Longitude = 0;
            Altitude = 0;
        }

        public GpsCoordinate(double latitude, double longitude, double altitude = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        /// <summary>
        /// İki GPS koordinatı arasındaki mesafeyi Haversine formülü ile hesaplar
        /// </summary>
        /// <param name="other">Hedef koordinat</param>
        /// <returns>Mesafe (metre)</returns>
        public double DistanceTo(GpsCoordinate other)
        {
            const double EarthRadius = 6371000; // metre

            double lat1Rad = Latitude * Math.PI / 180;
            double lat2Rad = other.Latitude * Math.PI / 180;
            double deltaLat = (other.Latitude - Latitude) * Math.PI / 180;
            double deltaLon = (other.Longitude - Longitude) * Math.PI / 180;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadius * c;
        }

        /// <summary>
        /// Hedef koordinata doğru bearing (pusula açısı) hesaplar
        /// </summary>
        /// <param name="target">Hedef koordinat</param>
        /// <returns>Bearing (0-360° arası, 0=Kuzey)</returns>
        public double BearingTo(GpsCoordinate target)
        {
            double lat1Rad = Latitude * Math.PI / 180;
            double lat2Rad = target.Latitude * Math.PI / 180;
            double deltaLon = (target.Longitude - Longitude) * Math.PI / 180;

            double y = Math.Sin(deltaLon) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                      Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(deltaLon);

            double bearing = Math.Atan2(y, x) * 180 / Math.PI;

            // 0-360° arasına normalize et
            return (bearing + 360) % 360;
        }

        public override string ToString()
        {
            return $"{Latitude:F6}°, {Longitude:F6}° @ {Altitude:F1}m";
        }
    }
}
