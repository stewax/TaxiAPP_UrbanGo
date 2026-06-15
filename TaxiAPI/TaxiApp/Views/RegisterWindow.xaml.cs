using System.Windows;
using System.Windows.Controls;
using TaxiApp.ViewModels;
using TaxiApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TaxiApp.Views;

public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();

        // 🔥 Получаем ViewModel из DI
        var app = (App)Application.Current;
        var viewModel = app.Services.GetRequiredService<RegisterViewModel>();
        DataContext = viewModel;
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