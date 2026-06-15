using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaxiApp.Services;
using TaxiApp.Views;
using TaxiApp.Views.Pages;
using TaxiApp.Views.Pages.Admin;
using TaxiApp.Views.Pages.Drivers;

namespace TaxiApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private readonly IApiService? _apiService;

    private UserControl _currentPage = null!;
    private string _currentUserRole = string.Empty;
    private int _currentUserId;

    public UserControl CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public int CurrentUserId => _currentUserId;
    public string CurrentUserRole => _currentUserRole;

    public bool IsClient => _currentUserRole == "client";
    public bool IsDriver => _currentUserRole == "driver";
    public bool IsAdmin => _currentUserRole == "admin";
    public bool HasRole => !string.IsNullOrEmpty(_currentUserRole);

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    // 🔥 ВАЖНО: Получаем AuthService через DI
    public MainViewModel(IAuthService authService, IApiService? apiService = null)
    {
        _authService = authService;
        _apiService = apiService;

        System.Diagnostics.Debug.WriteLine($"✅ MainViewModel создан с AuthService: {_authService.GetHashCode()}");

        NavigateCommand = new RelayCommand<string>(NavigateFromMenu);
        LogoutCommand = new RelayCommand(Logout);

        NavigateTo("Order");
    }

    public void SetCurrentUser(int userId, string role)
    {
        _currentUserId = userId;
        _currentUserRole = role;

        OnPropertyChanged(nameof(CurrentUserId));
        OnPropertyChanged(nameof(CurrentUserRole));
        OnPropertyChanged(nameof(IsClient));
        OnPropertyChanged(nameof(IsDriver));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(HasRole));

        System.Diagnostics.Debug.WriteLine($"👤 Пользователь установлен: userId={userId}, role={role}");
        System.Diagnostics.Debug.WriteLine($"   IsClient={IsClient}, IsDriver={IsDriver}, IsAdmin={IsAdmin}");

        if (role == "driver")
            NavigateTo("DriverOrders");
        else
            NavigateTo("Order");
    }

    private void NavigateFromMenu(string? page)
    {
        if (string.IsNullOrEmpty(page)) return;
        NavigateTo(page);
    }

    public void NavigateTo(string? page, int? orderId = null)
    {
        if (string.IsNullOrEmpty(page)) return;

        System.Diagnostics.Debug.WriteLine($"🔄 Навигация: {page}");

        CurrentPage = page switch
        {
            "Order" => new OrderPage(this),
            "History" => new HistoryPage(this),
            "Profile" => new ProfilePage(this),
            "Reviews" => new ReviewsPage(this),
            "DriverOrders" => new DriverOrdersPage(this),
            "DriverActiveOrder" => new DriverActiveOrderPage(this, orderId ?? 0),
            "DriverEarnings" => new DriverEarningsPage(this),
            // 🔥 НОВОЕ: Админ-страницы
            "AdminTariffs" => new AdminTariffsPage(this),
            "AdminUsers" => new AdminUsersPage(this),
            "AdminOrders" => new AdminOrdersPage(this),
            "AdminTrips" => new AdminTripsPage(this),
            _ => new OrderPage(this)
        };
    }

    private void Logout()
    {
        var result = MessageBox.Show("Выйти из системы?", "Выход",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            System.Diagnostics.Debug.WriteLine("🚪 Выход из системы...");
            _authService.ClearToken();

            var loginWindow = new LoginWindow();
            loginWindow.Show();

            var windowsToClose = new List<Window>();
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow)
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        System.Diagnostics.Debug.WriteLine($"🔔 OnPropertyChanged: {name}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter)
    {
        if (parameter is T typedParam)
            _execute(typedParam);
        else if (parameter is string strParam && typeof(T) == typeof(string))
            _execute((T)(object)strParam);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}