using Avalonia;
using Avalonia.ReactiveUI; // <--- Bu satırı ekle
using System;

namespace AvionX
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia konfigürasyonu
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI(); // <--- KRİTİK NOKTA: Bu satır mutlaka olmalı!
    }
}