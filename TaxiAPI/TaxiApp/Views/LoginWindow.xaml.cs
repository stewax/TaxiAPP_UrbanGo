using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TaxiApp.ViewModels;
using TaxiApp.Services;

namespace TaxiApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        // 🔥 Получаем ViewModel из DI
        var app = (App)Application.Current;
        var viewModel = app.Services.GetRequiredService<LoginViewModel>();
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = ((PasswordBox)sender).Password;
        }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        var app = (App)Application.Current;
        var authService = app.Services.GetService(typeof(IAuthService)) as IAuthService;
        authService?.ClearToken();

        System.Diagnostics.Debug.WriteLine("🔐 LoginWindow открыт, старый токен очищен");
    }
}