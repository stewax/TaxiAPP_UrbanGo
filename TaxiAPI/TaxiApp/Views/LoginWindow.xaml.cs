using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TaxiApp.ViewModels;
using TaxiApp.Services;
using System.Windows.Input;

namespace TaxiApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        EnterFullScreen();

        // 🔥 Получаем ViewModel из DI
        var app = (App)Application.Current;
        var viewModel = app.Services.GetRequiredService<LoginViewModel>();
        DataContext = viewModel;
    }
    private void EnterFullScreen()
    {
        this.WindowStyle = WindowStyle.None;
        this.WindowState = WindowState.Maximized;
        this.ResizeMode = ResizeMode.NoResize;
    }

    private void ExitFullScreen()
    {
        this.WindowStyle = WindowStyle.SingleBorderWindow;
        this.WindowState = WindowState.Normal;
        this.ResizeMode = ResizeMode.CanResize;
    }

    // Обработчик нажатия клавиши Escape для выхода
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ExitFullScreen();
        }
        base.OnKeyDown(e);
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