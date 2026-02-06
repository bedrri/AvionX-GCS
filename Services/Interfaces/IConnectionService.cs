using System;
using AvionX.Models; // TelemetryData modelini kullanabilmek için

namespace AvionX.Services.Interfaces
{
    public interface IConnectionService
    {
        // --- Observer Pattern ---
        // Servis yeni bir veri paketi oluşturduğunda bu event tetiklenecek.
        // ViewModel bu event'e abone (subscribe) olacak.
        event Action<TelemetryData>? DataReceived;

        // Bağlantı durumunu takip etmek için bir event (Bağlandı/Koptu)
        event Action<bool>? ConnectionStatusChanged;

        // --- Metotlar (Actions) ---
        // Bağlantıyı başlatır
        void Connect(string portName);

        // Bağlantıyı keser
        void Disconnect();

        // Şu an bağlı mı?
        bool IsConnected { get; }
    }
}