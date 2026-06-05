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
        // Сервисы
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IApiService, ApiService>();

        // HttpClient
        services.AddHttpClient();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<MainViewModel>();

        // Views
        services.AddSingleton<LoginWindow>();
        services.AddSingleton<RegisterWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Показываем окно входа
        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }
}