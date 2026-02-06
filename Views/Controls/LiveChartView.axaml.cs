using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvionX.ViewModels;
using ScottPlot; // ScottPlot 5 Namespace
using ScottPlot.Avalonia; // Avalonia bileşenleri için
using System.Linq;
using System;

namespace AvionX.Views.Controls
{
    public partial class LiveChartView : UserControl
    {
        // ScottPlot 5'te kontrol türü 'AvaPlot' olarak geçer
        private AvaPlot? _avaPlot;

        public LiveChartView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // XAML'daki bileşeni buluyoruz
            _avaPlot = this.FindControl<AvaPlot>("AltitudePlot");

            if (_avaPlot != null)
            {
                // --- SCOTTPLOT 5 STİL AYARLARI ---
                
                // Arka planı uygulamamızın koyu temasına uyduruyoruz (#2d2d30)
                var darkThemeColor = ScottPlot.Color.FromHex("#2d2d30");
                _avaPlot.Plot.FigureBackground.Color = darkThemeColor;
                _avaPlot.Plot.DataBackground.Color = darkThemeColor;

                // Eksen ve yazı renklerini açık gri yapıyoruz
                _avaPlot.Plot.Axes.Color(ScottPlot.Colors.LightGray);
                
                // Izgara (Grid) çizgilerini hafifçe belli ediyoruz
                _avaPlot.Plot.Grid.MajorLineColor = ScottPlot.Colors.White.WithOpacity(0.1);

                // Eksen İsimleri
                _avaPlot.Plot.Axes.Bottom.Label.Text = "Zaman";
                _avaPlot.Plot.Axes.Left.Label.Text = "İrtifa (m)";
            }
            
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RequestChartUpdate += UpdateChart;
            }
        }

        private void UpdateChart()
        {
            // Eğer kontrol henüz yüklenmediyse işlem yapma
            if (_avaPlot == null) return;

            if (DataContext is MainViewModel vm)
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        lock (vm.AltitudeHistory)
                        {
                            // Veri yoksa çizme
                            if (vm.AltitudeHistory.Count < 2) return;

                            // 1. Plottable'ları (Çizgileri) temizle ama Eksenleri koru
                            _avaPlot.Plot.PlottableList.Clear();

                            // 2. Yeni veriyi ekle
                            double[] data = vm.AltitudeHistory.ToArray();
                            var signal = _avaPlot.Plot.Add.Signal(data);
                            signal.Color = ScottPlot.Colors.Cyan;
                            signal.LineWidth = 2;

                            // 3. Eksenleri otomatik ayarla (AutoScale)
                            _avaPlot.Plot.Axes.AutoScale();
                            
                            // 4. Çiz
                            _avaPlot.Refresh();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Hata olursa konsola yaz ama programı durdurma
                        System.Diagnostics.Debug.WriteLine($"Çizim Hatası: {ex.Message}");
                    }
                });
            }
        }
    }
}