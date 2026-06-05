using System.Windows;
using System.Windows.Controls;
using TaxiApp.ViewModels;
using TaxiApp.Services;

namespace TaxiApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        // 🔥 Создаем ViewModel с зависимостями
        var authService = new AuthService(new System.Net.Http.HttpClient());
        DataContext = new LoginViewModel(authService);
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = ((PasswordBox)sender).Password;
    }
}