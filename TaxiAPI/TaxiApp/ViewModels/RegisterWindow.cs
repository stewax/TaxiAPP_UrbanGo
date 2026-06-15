using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxiApp.Services;
using TaxiApp.Views;

namespace TaxiApp.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isNotLoading = true;

    // 🔥 ВАЖНО: Получаем AuthService через DI
    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
        System.Diagnostics.Debug.WriteLine($"✅ RegisterViewModel создан с AuthService: {_authService.GetHashCode()}");
    }

    [RelayCommand]
    private async Task Register()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Введите ФИО";
            HasError = true;
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(FullName, @"^[a-zA-Zа-яА-ЯёЁ\s\-]+$"))
        {
            ErrorMessage = "ФИО может содержать только буквы, пробелы и дефисы";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorMessage = "Введите номер телефона";
            HasError = true;
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(Phone, @"^\+?\d{10,15}$"))
        {
            ErrorMessage = "Введите корректный номер телефона (10-15 цифр)";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите пароль";
            HasError = true;
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Пароль должен содержать минимум 6 символов";
            HasError = true;
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Пароли не совпадают";
            HasError = true;
            return;
        }

        IsLoading = true;
        IsNotLoading = false;

        try
        {
            var result = await _authService.RegisterAsync(FullName, Phone, Password);

            if (result != null && result.Success)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "Регистрация успешна! Теперь вы можете войти.",
                        "Регистрация завершена",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    var loginWindow = new LoginWindow();
                    loginWindow.Show();

                    var windowsToClose = new List<Window>();
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is RegisterWindow)
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
                ErrorMessage = result?.Message ?? "Ошибка регистрации";
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
    private void Login()
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();

        var windowsToClose = new List<Window>();
        foreach (Window window in Application.Current.Windows)
        {
            if (window is RegisterWindow)
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