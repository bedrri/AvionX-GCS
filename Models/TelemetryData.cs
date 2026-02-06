using System; // <--- Bu satırı eklemezsek DateTime'ı tanımaz.

namespace AvionX.Models
{
    public class TelemetryData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public double BatteryLevel { get; set; }
        public double Pitch { get; set; }
        public double Roll { get; set; }
        public double Heading { get; set; } // Pusula açısı (0-360°)
        public double VerticalSpeed { get; set; } // Dikey hız (m/s)
        public DateTime Timestamp { get; set; }

        public TelemetryData()
        {
            Timestamp = DateTime.Now;
        }
    }
}