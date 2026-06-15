using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TaxiApp.Services;
using TaxiApp.ViewModels;
using TaxiApp.Views;

namespace TaxiApp;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // HttpClient
        services.AddHttpClient();

        // 🔥 ВАЖНО: Singleton для AuthService и ApiService
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IApiService, ApiService>();

        // ViewModels (Transient - создаются каждый раз)
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<MainViewModel>();

        // Views (Singleton)
        services.AddSingleton<LoginWindow>();
        services.AddSingleton<RegisterWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }
}