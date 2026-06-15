using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxiApp.Services;
using TaxiApp.Views;

namespace TaxiApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isNotLoading = true;

    // 🔥 ВАЖНО: Получаем AuthService через DI
    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        System.Diagnostics.Debug.WriteLine($"✅ LoginViewModel создан с AuthService: {_authService.GetHashCode()}");
    }

    [RelayCommand]
    private async Task Login()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите телефон и пароль";
            HasError = true;
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(Phone, @"^\+?\d{10,15}$"))
        {
            ErrorMessage = "Введите корректный номер телефона (10-15 цифр)";
            HasError = true;
            return;
        }

        IsLoading = true;
        IsNotLoading = false;

        try
        {
            System.Diagnostics.Debug.WriteLine($"🔐 Попытка входа: {Phone}");

            var result = await _authService.LoginAsync(Phone, Password);

            if (result != null && result.Success)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Вход успешен: userId={result.UserId}, role={result.Role}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = new MainWindow(result.UserId, result.Role);
                    mainWindow.Show();

                    var windowsToClose = new List<Window>();
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is LoginWindow)
                        {
                            windowsToClose.Add(window);
                        }
                    }

                    foreach (var window in windowsToClose)
                    {
                        window.Close();
                    }
                });
            }
            else
            {
                ErrorMessage = result?.Message ?? "Ошибка входа";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка подключения: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            IsNotLoading = true;
        }
    }

    [RelayCommand]
    private void Register()
    {
        var registerWindow = new RegisterWindow();
        registerWindow.Show();

        var windowsToClose = new List<Window>();
        foreach (Window window in Application.Current.Windows)
        {
            if (window is LoginWindow)
            {
                windowsToClose.Add(window);
            }
        }

        foreach (var window in windowsToClose)
        {
            window.Close();
        }
    }
}