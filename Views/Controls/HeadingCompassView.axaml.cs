using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Markup.Xaml;

namespace AvionX.Views.Controls
{
    /// <summary>
    /// Heading Compass - Dönen Pusula Bileşeni
    ///
    /// DESIGN PATTERN: Custom Control with Code-Behind Rotation
    /// - Heading property ile dış dünyadan veri alır
    /// - MVVM uyumlu, ViewModele bağlanır
    /// - C# RenderTransform ile rotation (Artificial Horizon ile aynı pattern)
    ///
    /// AVİONİK MANTIK:
    /// - Uçak merkezdeki yeşil simge (sabit)
    /// - Pusula kadranı döner (heading'e göre ters yönde)
    /// - Örnek: Heading=90° (Doğu) → Pusula -90° döner → E harfi sağa gelir
    /// </summary>
    public partial class HeadingCompassView : UserControl
    {
        // Canvas reference for rotating compass dial
        private Canvas? Canvas_CompassDial;
        private const double CANVAS_SIZE = 200;
        private const double CENTER = CANVAS_SIZE / 2; // 100

        /// <summary>
        /// Heading (Pusula Açısı) Property
        /// 0° = Kuzey (N), 90° = Doğu (E), 180° = Güney (S), 270° = Batı (W)
        /// </summary>
        public static readonly StyledProperty<double> HeadingProperty =
            AvaloniaProperty.Register<HeadingCompassView, double>(
                nameof(Heading),
                defaultValue: 0.0,
                coerce: CoerceHeading);

        public double Heading
        {
            get => GetValue(HeadingProperty);
            set => SetValue(HeadingProperty, value);
        }

        public HeadingCompassView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get reference to rotating canvas (using x:Name)
            Canvas_CompassDial = this.Find<Canvas>("PART_CompassDial");

            Console.WriteLine($"[Compass] InitializeComponent: Canvas_CompassDial={(Canvas_CompassDial != null ? "OK" : "NULL")}");

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Console.WriteLine($"[Compass] OnLoaded called");
            DrawCompassMarkers();
            ApplyRotation();
        }

        /// <summary>
        /// Pusula işaretlerini matematiksel olarak çizer (mükemmel simetri)
        /// </summary>
        private void DrawCompassMarkers()
        {
            if (Canvas_CompassDial == null) return;

            // Clear existing tick marks (keep only letters)
            var elementsToRemove = Canvas_CompassDial.Children
                .Where(c => c is Rectangle)
                .ToList();
            foreach (var element in elementsToRemove)
            {
                Canvas_CompassDial.Children.Remove(element);
            }

            // Draw tick marks for every 15 degrees
            for (int angle = 0; angle < 360; angle += 15)
            {
                // N, E, S, W (0°, 90°, 180°, 270°) için çizgi çizme - sadece harfler var
                bool isCardinal = angle % 90 == 0;
                if (isCardinal) continue;

                bool isIntercardinal = angle % 45 == 0; // NE, SE, SW, NW

                double length, thickness;
                IBrush color;

                if (isIntercardinal)
                {
                    length = 15;
                    thickness = 3;
                    color = new SolidColorBrush(Color.FromRgb(136, 136, 136)); // #888
                }
                else
                {
                    length = 10;
                    thickness = 2;
                    color = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // #555
                }

                // Radyan'a çevir
                double angleRad = angle * Math.PI / 180.0;

                // Çizginin dış ucunun koordinatları (center'dan 90px uzaklıkta - radius)
                double outerRadius = 90;
                double innerRadius = outerRadius - length;

                double outerX = CENTER + outerRadius * Math.Sin(angleRad);
                double outerY = CENTER - outerRadius * Math.Cos(angleRad);
                double innerX = CENTER + innerRadius * Math.Sin(angleRad);
                double innerY = CENTER - innerRadius * Math.Cos(angleRad);

                // Çizgiyi Line olarak çiz
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Point(innerX, innerY),
                    EndPoint = new Point(outerX, outerY),
                    Stroke = color,
                    StrokeThickness = thickness,
                    StrokeLineCap = PenLineCap.Round
                };

                Canvas_CompassDial.Children.Add(line);
            }
        }

        /// <summary>
        /// Heading değerini 0-360° arasında normalize eder
        /// AVİONİK STANDART: Heading her zaman 0-360 arası
        /// </summary>
        private static double CoerceHeading(AvaloniaObject obj, double value)
        {
            // Normalize: 0-360° arası
            while (value >= 360) value -= 360;
            while (value < 0) value += 360;
            return value;
        }

        /// <summary>
        /// Property changed handler - Heading değiştiğinde rotasyonu güncelle
        /// </summary>
        static HeadingCompassView()
        {
            HeadingProperty.Changed.AddClassHandler<HeadingCompassView>((x, e) => x.ApplyRotation());
        }

        /// <summary>
        /// Pusula kadranını döndür (Artificial Horizon pattern)
        /// AVIATION LOGIC: Uçak sabit, dünya döner (inverse rotation)
        /// </summary>
        private void ApplyRotation()
        {
            if (Canvas_CompassDial == null)
            {
                Console.WriteLine("[Compass] ApplyRotation: Canvas not ready yet");
                return;
            }

            // Ters yönde döndür (aircraft fixed, world rotates)
            // Merkez nokta: (0, 0) - Canvas'ın sol üst köşesi
            Canvas_CompassDial.RenderTransform = new RotateTransform(-Heading, 0, 0);

            Console.WriteLine($"[Compass] ApplyRotation: Heading={Heading:F1}° → Rotation={-Heading:F1}°");
        }
    }

    /// <summary>
    /// NegateConverter - Artık kullanılmıyor ama geriye dönük uyumluluk için bırakıldı
    /// Şimdi rotation C# tarafında yapılıyor
    /// </summary>
    public class NegateConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double d)
                return -d;
            return 0.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double d)
                return -d;
            return 0.0;
        }
    }
}
