using System.Windows;
using System.Windows.Controls;
using TaxiApp.ViewModels;
using TaxiApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace TaxiApp.Views;

public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();
        EnterFullScreen();
        // 🔥 Получаем ViewModel из DI
        var app = (App)Application.Current;
        var viewModel = app.Services.GetRequiredService<RegisterViewModel>();
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
        if (DataContext is RegisterViewModel vm)
        {
            vm.Password = ((PasswordBox)sender).Password;
        }
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm)
        {
            vm.ConfirmPassword = ((PasswordBox)sender).Password;
        }
    }
}