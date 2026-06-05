using System.Windows;
using System.Windows.Controls;
using TaxiApp.ViewModels;
using TaxiApp.Services;

namespace TaxiApp.Views;

public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();

        // 🔥 Создаем ViewModel с зависимостями
        var authService = new AuthService(new System.Net.Http.HttpClient());
        DataContext = new RegisterViewModel(authService);
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm)
            vm.Password = ((PasswordBox)sender).Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm)
            vm.ConfirmPassword = ((PasswordBox)sender).Password;
    }
}