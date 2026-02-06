using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvionX.Services.Impl;      // Servislerimiz burada
using AvionX.Services.Interfaces; // Arayüzlerimiz burada
using AvionX.ViewModels;
using AvionX.Views;
using Microsoft.Extensions.DependencyInjection; // DI Kütüphanesi
using System;

namespace AvionX
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 1. ServiceCollection oluşturulur.
            // Bu bizim "Alet Çantamız"dır. Uygulamada kullanacağımız her şeyi buraya atacağız.
            var collection = new ServiceCollection();

            // 2. Servisleri Kaydet (Register Services)
            
            // AddSingleton: Uygulama boyunca SADECE BİR TANE yaratılır.
            // Donanım bağlantıları (SerialPort) genelde Singleton olmalıdır.
            // "Biri IConnectionService isterse, ona SimulatedConnectionService ver" diyoruz.
            collection.AddSingleton<IConnectionService, SimulatedConnectionService>();

            // 3. ViewModel'leri Kaydet
            // AddTransient: Her ihtiyaç duyulduğunda YENİ bir tane yaratılır.
            // Genelde ViewModel'ler Transient (veya duruma göre Singleton) olur.
            collection.AddTransient<MainViewModel>();

            // 4. Çantayı Kapat ve Servis Sağlayıcıyı (Provider) Üret
            var services = collection.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 5. Ana Pencereyi Oluştur
                // DataContext (ViewModel) artık elle "new" ile değil,
                // services kutusundan "GetRequiredService" ile çekiliyor.
                
                var mainViewModel = services.GetRequiredService<MainViewModel>();
                
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}