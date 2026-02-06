using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using AvionX.Models;
using AvionX.Services.Interfaces;
using Avalonia.Threading; // <--- BU EKLENDİ (UI Thread Erişimi İçin)

namespace AvionX.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IConnectionService _connectionService;

        // --- UI Properties ---
        private string _connectionStatusText = "Bağlantı Kesildi";
        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => this.RaiseAndSetIfChanged(ref _connectionStatusText, value);
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                this.RaiseAndSetIfChanged(ref _isConnected, value);
                // Update button color when connection state changes
                this.RaisePropertyChanged(nameof(ConnectButtonBackground));
            }
        }

        // Button background color based on connection state
        public string ConnectButtonBackground => IsConnected ? "#2E7D32" : "#8B0000"; // Green : Red

        private string _altitudeText = "0 m";
        public string AltitudeText
        {
            get => _altitudeText;
            set => this.RaiseAndSetIfChanged(ref _altitudeText, value);
        }

        private string _speedText = "0 m/s";
        public string SpeedText
        {
            get => _speedText;
            set => this.RaiseAndSetIfChanged(ref _speedText, value);
        }

        private string _batteryText = "100%";
        public string BatteryText
        {
            get => _batteryText;
            set => this.RaiseAndSetIfChanged(ref _batteryText, value);
        }

        private double _batteryLevel = 100.0;
        public double BatteryLevel
        {
            get => _batteryLevel;
            set => this.RaiseAndSetIfChanged(ref _batteryLevel, value);
        }

        private string _groundSpeedText = "0 m/s";
        public string GroundSpeedText
        {
            get => _groundSpeedText;
            set => this.RaiseAndSetIfChanged(ref _groundSpeedText, value);
        }

        private string _airSpeedText = "0 m/s";
        public string AirSpeedText
        {
            get => _airSpeedText;
            set => this.RaiseAndSetIfChanged(ref _airSpeedText, value);
        }

        private string _verticalSpeedText = "0 m/s";
        public string VerticalSpeedText
        {
            get => _verticalSpeedText;
            set => this.RaiseAndSetIfChanged(ref _verticalSpeedText, value);
        }

        private string _latitudeText = "0.0000";
        public string LatitudeText
        {
            get => _latitudeText;
            set => this.RaiseAndSetIfChanged(ref _latitudeText, value);
        }

        private string _longitudeText = "0.0000";
        public string LongitudeText
        {
            get => _longitudeText;
            set => this.RaiseAndSetIfChanged(ref _longitudeText, value);
        }

        private double _headingAngle = 0;
        public double HeadingAngle
        {
            get => _headingAngle;
            set => this.RaiseAndSetIfChanged(ref _headingAngle, value);
        }

        // Smoothing için heading
        private double _smoothedHeading = 0;

        // --- GPS Coordinates (for map) ---
        private double _droneLatitude = 41.0082;
        public double DroneLatitude
        {
            get => _droneLatitude;
            set => this.RaiseAndSetIfChanged(ref _droneLatitude, value);
        }

        private double _droneLongitude = 28.9784;
        public double DroneLongitude
        {
            get => _droneLongitude;
            set => this.RaiseAndSetIfChanged(ref _droneLongitude, value);
        }

        private double _droneAltitude = 0;
        public double DroneAltitude
        {
            get => _droneAltitude;
            set => this.RaiseAndSetIfChanged(ref _droneAltitude, value);
        }

        // --- Açı Verileri ---
        private double _rollAngle = 0;
        public double RollAngle
        {
            get => _rollAngle;
            set => this.RaiseAndSetIfChanged(ref _rollAngle, value);
        }

        private double _pitchAngle = 0;
        public double PitchAngle
        {
            get => _pitchAngle;
            set => this.RaiseAndSetIfChanged(ref _pitchAngle, value);
        }

        // --- Smoothing için geçmiş değerler ---
        private double _smoothedRoll = 0;
        private double _smoothedPitch = 0;
        private const double SmoothingFactor = 0.15; // Küçük = daha smooth

        // --- Grafik Verileri ---
        public List<double> AltitudeHistory { get; } = new List<double>();
        public List<double> SpeedHistory { get; } = new List<double>();
        public event Action? RequestChartUpdate;

        // --- Commands ---
        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }

        public MainViewModel(IConnectionService connectionService)
        {
            _connectionService = connectionService;

            _connectionService.DataReceived += OnDataReceived;
            _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;

            ConnectCommand = ReactiveCommand.Create(() => 
            {
                _connectionService.Connect("COM1");
            });

            DisconnectCommand = ReactiveCommand.Create(() => 
            {
                _connectionService.Disconnect();
            });
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            // UI Thread güvenliği
            Dispatcher.UIThread.Post(() =>
            {
                IsConnected = isConnected;
                ConnectionStatusText = isConnected ? "Bağlantı Kuruldu (Simülasyon)" : "Bağlantı Kesildi";
            });
        }

        private void OnDataReceived(TelemetryData data)
        {
            // KRİTİK DÜZELTME:
            // Gelen veri arka plandan geliyor.
            // UI elemanlarını güncellemek için işlemi UI Thread'e postalıyoruz.
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // 1. UI Metinlerini Güncelle
                    AltitudeText = $"{data.Altitude:F1} m";
                    SpeedText = $"{data.Speed:F1} m/s";
                    BatteryText = $"{data.BatteryLevel:F0}%";
                    BatteryLevel = data.BatteryLevel;

                    // Ground speed = yatay hız, airspeed = gerçek hız (pitch etkisiyle)
                    GroundSpeedText = $"{data.Speed:F1} m/s";
                    AirSpeedText = $"{data.Speed * 1.1:F1} m/s"; // Basitleştirilmiş

                    // Dikey hız = irtifa türevi (basitleştirilmiş - sonra simülasyondan alacağız)
                    VerticalSpeedText = $"{(data.Pitch * 0.5):F1} m/s";

                    // GPS koordinatları
                    LatitudeText = $"{data.Latitude:F5}";
                    LongitudeText = $"{data.Longitude:F5}";

                    // GPS koordinatları (for map)
                    DroneLatitude = data.Latitude;
                    DroneLongitude = data.Longitude;
                    DroneAltitude = data.Altitude;

                    // 2. Açıları Smooth Filtre ile Güncelle (Exponential Moving Average)
                    // DESIGN PATTERN: Low-pass filter (avionics'te kritik - sensör gürültüsü filtreleme)
                    _smoothedRoll = _smoothedRoll + SmoothingFactor * (data.Roll - _smoothedRoll);
                    _smoothedPitch = _smoothedPitch + SmoothingFactor * (data.Pitch - _smoothedPitch);
                    _smoothedHeading = _smoothedHeading + SmoothingFactor * (data.Heading - _smoothedHeading);

                    RollAngle = _smoothedRoll;
                    PitchAngle = _smoothedPitch;
                    HeadingAngle = _smoothedHeading;

                    // 3. Grafik Verisini İşle
                    lock (AltitudeHistory)
                    {
                        AltitudeHistory.Add(data.Altitude);
                        SpeedHistory.Add(data.Speed);

                        // 200 veriden fazlasını sil
                        if (AltitudeHistory.Count > 200)
                        {
                            AltitudeHistory.RemoveAt(0);
                            SpeedHistory.RemoveAt(0);
                        }
                    }

                    // 4. Grafiğe "Çiz" emri ver
                    RequestChartUpdate?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Veri işleme hatası: {ex.Message}");
                }
            });
        }
    }
}