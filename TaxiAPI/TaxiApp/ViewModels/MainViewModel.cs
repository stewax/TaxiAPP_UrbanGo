using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using TaxiApp.Services;
using TaxiApp.Views;
using TaxiApp.Views.Pages;

namespace TaxiApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;

    private UserControl _currentPage;
    private string _currentUserRole = "client";
    private int _currentUserId;

    public UserControl CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public int CurrentUserId => _currentUserId;
    public string CurrentUserRole => _currentUserRole;

    public MainViewModel(IAuthService authService)
    {
        _authService = authService;
        NavigateTo("Order"); // Главная страница по умолчанию
    }

    public void SetCurrentUser(int userId, string role)
    {
        _currentUserId = userId;
        _currentUserRole = role;
    }

    private void NavigateTo(string page)
    {
        CurrentPage = page switch
        {
            "Order" => new OrderPage(this),
            "History" => new HistoryPage(this),
            "Profile" => new ProfilePage(this),
            "Reviews" => new ReviewsPage(this),
            _ => new OrderPage(this)
        };
    }

    // Команда навигации (для XAML)
    public void Navigate(string page) => NavigateTo(page);

    public void Logout()
    {
        var result = MessageBox.Show("Выйти из системы?", "Выход",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _authService.ClearToken();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            foreach (Window w in Application.Current.Windows)
                if (w is MainWindow) { w.Close(); break; }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}