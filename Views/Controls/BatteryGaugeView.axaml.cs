using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace AvionX.Views.Controls
{
    /// <summary>
    /// Battery Gauge - Horizontal Battery Icon with Bars
    ///
    /// DESIGN PATTERN: Custom Control with Computed Properties
    /// - BatteryLevel (0-100) input property
    /// - BatteryColor computed property (renk seviyeye göre değişir)
    /// - Bar1-8 Opacity properties (her bar 12.5% temsil eder)
    ///
    /// AVİONİK STANDART:
    /// - Red (<20%): CRITICAL - Immediate landing required
    /// - Orange (20-40%): WARNING - Return to base
    /// - Yellow (40-60%): CAUTION - Mission completion required
    /// - Green (>60%): NORMAL - Continue operations
    /// </summary>
    public partial class BatteryGaugeView : UserControl
    {
        /// <summary>
        /// Battery Level (0-100%)
        /// </summary>
        public static readonly StyledProperty<double> BatteryLevelProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(
                nameof(BatteryLevel),
                defaultValue: 100.0,
                coerce: CoerceBatteryLevel);

        public double BatteryLevel
        {
            get => GetValue(BatteryLevelProperty);
            set => SetValue(BatteryLevelProperty, value);
        }

        /// <summary>
        /// Battery Color (Computed Property)
        /// </summary>
        public static readonly StyledProperty<IBrush> BatteryColorProperty =
            AvaloniaProperty.Register<BatteryGaugeView, IBrush>(
                nameof(BatteryColor),
                defaultValue: Brushes.Green);

        public IBrush BatteryColor
        {
            get => GetValue(BatteryColorProperty);
            private set => SetValue(BatteryColorProperty, value);
        }

        // Bar Opacity Properties (Her bar 12.5% temsil eder - 8 bar total)
        public static readonly StyledProperty<double> Bar1OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar1Opacity), 1.0);

        public double Bar1Opacity
        {
            get => GetValue(Bar1OpacityProperty);
            private set => SetValue(Bar1OpacityProperty, value);
        }

        public static readonly StyledProperty<double> Bar2OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar2Opacity), 1.0);

        public double Bar2Opacity
        {
            get => GetValue(Bar2OpacityProperty);
            private set => SetValue(Bar2OpacityProperty, value);
        }

        public static readonly StyledProperty<double> Bar3OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar3Opacity), 1.0);

        public double Bar3Opacity
        {
            get => GetValue(Bar3OpacityProperty);
            private set => SetValue(Bar3OpacityProperty, value);
        }

        public static readonly StyledProperty<double> Bar4OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar4Opacity), 1.0);

        public double Bar4Opacity
        {
            get => GetValue(Bar4OpacityProperty);
            private set => SetValue(Bar4OpacityProperty, value);
        }

        public static readonly StyledProperty<double> Bar5OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar5Opacity), 1.0);

        public double Bar5Opacity
        {
            get => GetValue(Bar5OpacityProperty);
            private set => SetValue(Bar5OpacityProperty, value);
        }

        public static readonly StyledProperty<double> Bar6OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar6Opacity), 1.0);

        public double Bar6Opacity
        {
            get => GetValue(Bar6OpacityProperty);
            private set => SetValue(Bar6OpacityProperty, value);
        }

        public static readonly StyledProperty<double> Bar7OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar7Opacity), 1.0);

        public double Bar7Opacity
        {
            get => GetValue(Bar7OpacityProperty);
            private set => SetValue(Bar7OpacityProperty, value);
        }

        public static readonly StyledProperty<double> Bar8OpacityProperty =
            AvaloniaProperty.Register<BatteryGaugeView, double>(nameof(Bar8Opacity), 1.0);

        public double Bar8Opacity
        {
            get => GetValue(Bar8OpacityProperty);
            private set => SetValue(Bar8OpacityProperty, value);
        }

        public BatteryGaugeView()
        {
            InitializeComponent();

            // BatteryLevel değiştiğinde görselleri güncelle
            this.GetObservable(BatteryLevelProperty)
                .Subscribe(_ => UpdateBatteryVisuals());

            // İlk değerleri ayarla
            UpdateBatteryVisuals();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static double CoerceBatteryLevel(AvaloniaObject obj, double value)
        {
            return Math.Clamp(value, 0, 100);
        }

        /// <summary>
        /// Battery görsellerini günceller (renk + bar opacity)
        /// Her bar 12.5% temsil eder: 8 bar total
        /// </summary>
        private void UpdateBatteryVisuals()
        {
            double level = BatteryLevel;

            // Renk Belirleme - Açık cırt yeşil
            if (level >= 60)
            {
                BatteryColor = new SolidColorBrush(Color.FromRgb(0, 255, 0)); // #00FF00 (Cırt Yeşil)
            }
            else if (level >= 40)
            {
                BatteryColor = new SolidColorBrush(Color.FromRgb(255, 215, 0));   // #FFD700 (Sarı)
            }
            else if (level >= 20)
            {
                BatteryColor = new SolidColorBrush(Color.FromRgb(255, 165, 0));   // #FFA500 (Turuncu)
            }
            else
            {
                BatteryColor = new SolidColorBrush(Color.FromRgb(255, 68, 68));   // #FF4444 (Kırmızı)
            }

            // Bar Opacity Hesaplama
            // 8 bar var, her biri 12.5% temsil ediyor
            Bar1Opacity = level > 0 ? 1.0 : 0.2;      // 0-12.5%
            Bar2Opacity = level > 12.5 ? 1.0 : 0.2;   // 12.5-25%
            Bar3Opacity = level > 25 ? 1.0 : 0.2;     // 25-37.5%
            Bar4Opacity = level > 37.5 ? 1.0 : 0.2;   // 37.5-50%
            Bar5Opacity = level > 50 ? 1.0 : 0.2;     // 50-62.5%
            Bar6Opacity = level > 62.5 ? 1.0 : 0.2;   // 62.5-75%
            Bar7Opacity = level > 75 ? 1.0 : 0.2;     // 75-87.5%
            Bar8Opacity = level > 87.5 ? 1.0 : 0.2;   // 87.5-100%
        }
    }
}
