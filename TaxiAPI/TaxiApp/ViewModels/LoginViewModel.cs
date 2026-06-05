using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxiApp.Services;
using TaxiApp.Views;

namespace TaxiApp.ViewModels;

public partial class LoginViewModel : BaseViewModel
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

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task Login()
    {
        // Сброс ошибок
        HasError = false;
        ErrorMessage = string.Empty;

        // Валидация
        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorMessage = "Введите номер телефона";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите пароль";
            HasError = true;
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(Phone, @"^\+?\d{10,15}$"))
        {
            ErrorMessage = "Введите корректный номер телефона (10-15 цифр)";
            HasError = true;
            return;
        }

        // Показываем индикатор загрузки
        IsLoading = true;
        IsNotLoading = false;

        try
        {
            var result = await _authService.LoginAsync(Phone, Password);

            if (result != null && result.Success)
            {
                // Успешный вход - открываем главное окно
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = new MainWindow(result.UserId, result.Role);
                    mainWindow.Show();

                    // Закрываем все окна входа
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
                ErrorMessage = result?.Message ?? "Ошибка входа. Проверьте телефон и пароль.";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка подключения к серверу: {ex.Message}";
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
        // Открываем окно регистрации
        var registerWindow = new RegisterWindow();
        registerWindow.Show();

        // Закрываем окно входа
        var windowsToClose = new List<Window>();
        foreach (Window window in Application.Current.Windows)
        {
            if (window is LoginWindow loginWindow)
            {
                windowsToClose.Add(loginWindow);
            }
        }

        foreach (var window in windowsToClose)
        {
            window.Close();
        }
    }
}