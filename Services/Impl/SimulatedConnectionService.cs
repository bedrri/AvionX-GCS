using System;
using System.Threading;
using System.Threading.Tasks;
using AvionX.Models;
using AvionX.Services.Interfaces;

namespace AvionX.Services.Impl
{
    public class SimulatedConnectionService : IConnectionService
    {
        public event Action<TelemetryData>? DataReceived;
        public event Action<bool>? ConnectionStatusChanged;

        private bool _isConnected;
        private CancellationTokenSource? _cancellationTokenSource;

        // --- GERÇEKÇİ DRONE FİZİK SİMÜLASYONU ---

        // Durum değişkenleri
        private double _currentAltitude = 50.0;      // Başlangıç: 50m yükseklik
        private double _currentSpeed = 12.0;         // Başlangıç: 12 m/s (43 km/h)
        private double _currentRoll = 0.0;
        private double _currentPitch = 0.0;
        private double _currentBattery = 100.0;
        private double _verticalSpeed = 0.0;         // Dikey hız (tırmanma/iniş)
        private double _currentHeading = 45.0;       // Başlangıç: Kuzeydoğu (45°)

        // GPS pozisyonu (heading'e göre hareket edecek)
        private double _currentLatitude = 41.0082;   // İstanbul
        private double _currentLongitude = 28.9784;

        // Hedef değerler (Autopilot gibi)
        private double _targetRoll = 0;
        private double _targetPitch = 0;
        private double _targetAltitude = 50.0;
        private double _targetSpeed = 12.0;
        // _targetHeading kaldırıldı - heading artık roll'dan otomatik hesaplanıyor

        // Uçuş senaryosu durumu
        private FlightMode _currentMode = FlightMode.Cruise;
        private double _maneuverStartTime = 0;

        private readonly Random _random = new Random();

        // Uçuş modları
        private enum FlightMode
        {
            TakeOff,        // Kalkış
            Cruise,         // Seyir
            Banking,        // Viraj
            Climbing,       // Tırmanma
            Descending,     // İniş
            Hovering,       // Asılı durma
            Landing         // Iniş
        }

        public bool IsConnected => _isConnected;

        public void Connect(string portName)
        {
            if (_isConnected) return;
            _isConnected = true;
            ConnectionStatusChanged?.Invoke(true);

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => GenerateRealisticFlightDataLoop(_cancellationTokenSource.Token));
        }

        public void Disconnect()
        {
            if (!_isConnected) return;
            _cancellationTokenSource?.Cancel();
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(false);
        }

        private async Task GenerateRealisticFlightDataLoop(CancellationToken token)
        {
            double timeCounter = 0;
            double nextModeChange = 5.0; // İlk mod değişimi 5 saniye sonra
            double nextAltitudeVariation = 2.0; // İlk irtifa varyasyonu 2 saniye sonra

            // Başlangıç hedeflerini ayarla
            SetTargetsForMode(_currentMode);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    double deltaTime = 0.05; // 50ms = 0.05 saniye
                    timeCounter += deltaTime;

                    // --- MOD YÖNETİMİ (Gerçekçi uçuş senaryoları) ---
                    if (timeCounter >= nextModeChange)
                    {
                        _currentMode = ChooseNextFlightMode();
                        _maneuverStartTime = timeCounter;
                        nextModeChange = timeCounter + _random.Next(8, 20); // 8-20 saniye sonraki mod değişimi

                        SetTargetsForMode(_currentMode);
                    }

                    // --- SÜREKLİ İRTİFA VARYASYONU (Cruise modunda bile hareket) ---
                    if (timeCounter >= nextAltitudeVariation && _currentMode == FlightMode.Cruise)
                    {
                        // Cruise modunda bile küçük irtifa değişimleri yap
                        _targetAltitude += (_random.NextDouble() - 0.5) * 20.0; // ±10m varyasyon
                        _targetAltitude = Math.Clamp(_targetAltitude, 30.0, 150.0);
                        nextAltitudeVariation = timeCounter + _random.Next(3, 8); // 3-8 saniye sonra tekrar
                    }

                    // --- FİZİK SİMÜLASYONU (Gerçekçi drone dinamiği) ---

                    // 1. ROLL & PITCH FİZİĞİ
                    // Drone'lar roll ile dönüş yapar, pitch ile hız/yükseklik değiştirir
                    UpdateAttitude(deltaTime);

                    // 2. İRTİFA & DİKEY HIZ
                    UpdateAltitude(deltaTime);

                    // 3. YATAY HIZ
                    UpdateSpeed(deltaTime);

                    // 4. BATARYA (Gerçekçi tüketim)
                    UpdateBattery(deltaTime);

                    // 5. HEADING (Pusula - virajda değişir)
                    UpdateHeading(deltaTime);

                    // 6. GPS POZİSYONU (Heading ve speed'e göre hareket)
                    UpdateGpsPosition(deltaTime);

                    // 7. ATMOSFER ETKİLERİ (Rüzgar, türbülans)
                    ApplyAtmosphericEffects();

                    // --- VERİYİ PAKETLE ---
                    var data = new TelemetryData
                    {
                        Altitude = _currentAltitude,
                        Speed = _currentSpeed,
                        BatteryLevel = _currentBattery,

                        // Çok hafif motor titreşimi (±0.1° - daha smooth)
                        Roll = _currentRoll + (_random.NextDouble() - 0.5) * 0.2,
                        Pitch = _currentPitch + (_random.NextDouble() - 0.5) * 0.2,
                        Heading = _currentHeading,
                        VerticalSpeed = _verticalSpeed,

                        // GPS: Gerçek fizik tabanlı hareket (heading ve speed'e göre)
                        Latitude = _currentLatitude,
                        Longitude = _currentLongitude,

                        Timestamp = DateTime.Now
                    };

                    DataReceived?.Invoke(data);
                    await Task.Delay(50, token); // 20 FPS
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Simülasyon Hatası: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        private FlightMode ChooseNextFlightMode()
        {
            // Gerçekçi mod geçişleri
            var modes = _currentMode switch
            {
                FlightMode.TakeOff => new[] { FlightMode.Cruise, FlightMode.Hovering },
                FlightMode.Cruise => new[] { FlightMode.Banking, FlightMode.Climbing, FlightMode.Descending, FlightMode.Hovering },
                FlightMode.Banking => new[] { FlightMode.Cruise, FlightMode.Banking },
                FlightMode.Climbing => new[] { FlightMode.Cruise, FlightMode.Hovering },
                FlightMode.Descending => new[] { FlightMode.Cruise, FlightMode.Landing },
                FlightMode.Hovering => new[] { FlightMode.Cruise, FlightMode.Banking, FlightMode.Landing },
                FlightMode.Landing => new[] { FlightMode.TakeOff, FlightMode.Hovering },
                _ => new[] { FlightMode.Cruise }
            };

            return modes[_random.Next(modes.Length)];
        }

        private void SetTargetsForMode(FlightMode mode)
        {
            switch (mode)
            {
                case FlightMode.TakeOff:
                    _targetAltitude = 100.0;
                    _targetSpeed = 8.0;
                    _targetPitch = 15.0; // Burun yukarı
                    _targetRoll = 0;
                    break;

                case FlightMode.Cruise:
                    _targetAltitude = 80.0 + _random.NextDouble() * 40.0; // 80-120m
                    _targetSpeed = 12.0 + _random.NextDouble() * 6.0;     // 12-18 m/s
                    _targetPitch = 0;
                    _targetRoll = 0;
                    break;

                case FlightMode.Banking:
                    // Viraj: 20-45° roll, hafif pitch
                    _targetRoll = (_random.Next(0, 2) == 0 ? 1 : -1) * (20 + _random.NextDouble() * 25);
                    _targetPitch = -5.0; // Virajda hafif burun aşağı
                    _targetSpeed = 10.0;
                    break;

                case FlightMode.Climbing:
                    _targetAltitude = _currentAltitude + 30.0;
                    _targetSpeed = 8.0; // Tırmanırken yavaşla
                    _targetPitch = 12.0;
                    _targetRoll = 0;
                    break;

                case FlightMode.Descending:
                    _targetAltitude = Math.Max(30.0, _currentAltitude - 40.0);
                    _targetSpeed = 7.0;
                    _targetPitch = -8.0; // Burun aşağı
                    _targetRoll = 0;
                    break;

                case FlightMode.Hovering:
                    _targetSpeed = 0.5; // Neredeyse durgun
                    _targetPitch = 0;
                    _targetRoll = 0;
                    break;

                case FlightMode.Landing:
                    _targetAltitude = 5.0;
                    _targetSpeed = 2.0;
                    _targetPitch = -5.0;
                    _targetRoll = 0;
                    break;
            }
        }

        private void UpdateAttitude(double deltaTime)
        {
            // PID kontrolcü gibi yumuşak geçiş - Dengeli hız
            // Simülasyon + ViewModeldeki smooth filter = çift katmanlı yumuşatma
            _currentRoll = Lerp(_currentRoll, _targetRoll, deltaTime * 1.2);   // Orta hız
            _currentPitch = Lerp(_currentPitch, _targetPitch, deltaTime * 1.5); // Orta hız

            // Fiziksel limitler
            _currentRoll = Math.Clamp(_currentRoll, -60, 60);
            _currentPitch = Math.Clamp(_currentPitch, -30, 30);
        }

        private void UpdateAltitude(double deltaTime)
        {
            // Hedef yüksekliğe göre dikey hız ayarla
            double altitudeError = _targetAltitude - _currentAltitude;

            // P kontrolcü (Proportional) - daha agresif
            double desiredVerticalSpeed = Math.Clamp(altitudeError * 0.5, -8.0, 8.0); // Max ±8 m/s dikey hız

            // Dikey hızı yumuşat - daha hızlı tepki
            _verticalSpeed = Lerp(_verticalSpeed, desiredVerticalSpeed, deltaTime * 3.0);

            // İrtifayı güncelle
            _currentAltitude += _verticalSpeed * deltaTime;
            _currentAltitude = Math.Max(5.0, _currentAltitude); // Min 5m (zemin)
        }

        private void UpdateSpeed(double deltaTime)
        {
            // Pitch açısı hızı etkiler (burun aşağı = hızlanma)
            double pitchEffect = -_currentPitch * 0.15;

            // Hedef hıza yumuşakça geç
            double speedTarget = _targetSpeed + pitchEffect;
            _currentSpeed = Lerp(_currentSpeed, speedTarget, deltaTime * 1.5);

            // Hız limitleri
            _currentSpeed = Math.Clamp(_currentSpeed, 0.5, 25.0);
        }

        private void UpdateBattery(double deltaTime)
        {
            // Gerçekçi batarya tüketimi (hareket, yükseklik, hız bağımlı)
            double baseConsumption = 0.01;  // %0.01/saniye temel tüketim
            double speedFactor = Math.Abs(_currentSpeed) * 0.0005;
            double climbFactor = Math.Abs(_verticalSpeed) * 0.002;
            double maneuverFactor = (Math.Abs(_currentRoll) + Math.Abs(_currentPitch)) * 0.0001;

            double totalConsumption = (baseConsumption + speedFactor + climbFactor + maneuverFactor) * deltaTime;
            _currentBattery -= totalConsumption;
            _currentBattery = Math.Max(0, _currentBattery);

            // Düşük bataryada otomatik iniş
            if (_currentBattery < 15.0 && _currentMode != FlightMode.Landing)
            {
                _currentMode = FlightMode.Landing;
                SetTargetsForMode(FlightMode.Landing);
            }
        }

        private void UpdateHeading(double deltaTime)
        {
            // KOORDİNELİ VİRAJ FİZİĞİ
            // Real Aviation: Roll angle → Turn rate (Rate of Turn)
            // Formül: Turn Rate (°/s) = (g * tan(roll)) / (v * π/180)
            // Basitleştirilmiş: Turn Rate ≈ roll * 0.5
            //
            // Örnekler:
            // Roll = 30° → Turn rate = 15°/s (30 saniyede 450° = 1.25 tur)
            // Roll = 45° → Turn rate = 22.5°/s (16 saniyede 360° = 1 tur)
            // Roll = 60° → Turn rate = 30°/s (12 saniyede 360° = 1 tur)

            double turnRate = _currentRoll * 0.5; // Gerçekçi turn rate

            // Banking mode'da ekstra agresif dönüş
            if (_currentMode == FlightMode.Banking)
            {
                turnRate *= 1.5; // Banking'de daha keskin viraj
            }

            // Heading sürekli güncellenir
            _currentHeading += turnRate * deltaTime;

            // 0-360° arası normalize et
            while (_currentHeading >= 360) _currentHeading -= 360;
            while (_currentHeading < 0) _currentHeading += 360;
        }

        private void UpdateGpsPosition(double deltaTime)
        {
            // GPS pozisyonunu heading ve speed'e göre güncelle
            // Gerçek drone gibi: Heading yönünde, speed hızıyla hareket et

            // Heading: 0° = Kuzey, 90° = Doğu, 180° = Güney, 270° = Batı
            double headingRad = _currentHeading * Math.PI / 180.0;

            // Hız (m/s) * deltaTime = Bu frame'de gidilen mesafe (metre)
            double distanceMeters = _currentSpeed * deltaTime;

            // Metre cinsinden kuzey ve doğu bileşenleri
            double northMeters = Math.Cos(headingRad) * distanceMeters;
            double eastMeters = Math.Sin(headingRad) * distanceMeters;

            // Metre → Derece dönüşümü
            // 1 derece latitude ≈ 111,320 metre
            // 1 derece longitude ≈ 111,320 * cos(latitude) metre
            const double metersPerDegreeLat = 111320.0;
            double metersPerDegreeLon = metersPerDegreeLat * Math.Cos(_currentLatitude * Math.PI / 180.0);

            // GPS koordinatlarını güncelle
            _currentLatitude += northMeters / metersPerDegreeLat;
            _currentLongitude += eastMeters / metersPerDegreeLon;
        }

        private void ApplyAtmosphericEffects()
        {
            // Rüzgar türbülansı (yüksekliğe bağlı)
            double turbulenceIntensity = _currentAltitude / 200.0; // Yüksekte daha çok rüzgar

            _targetRoll += (_random.NextDouble() - 0.5) * turbulenceIntensity;
            _targetPitch += (_random.NextDouble() - 0.5) * turbulenceIntensity * 0.5;

            // Rüzgar hız değişimi
            _currentSpeed += (_random.NextDouble() - 0.5) * turbulenceIntensity * 0.5;
        }

        private double Lerp(double start, double end, double amount)
        {
            return start + (end - start) * Math.Clamp(amount, 0, 1);
        }
    }
}