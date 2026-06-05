using System.Windows;
using TaxiApp.ViewModels;
using TaxiApp.Services;

namespace TaxiApp.Views;

public partial class MainWindow : Window
{
    public MainWindow(int userId, string role)
    {
        InitializeComponent();

        // 🔥 Создаем ViewModel с зависимостями
        var app = (App)Application.Current;
        var authService = app.Services.GetService(typeof(IAuthService)) as IAuthService;

        if (authService != null)
        {
            var vm = new MainViewModel(authService);
            vm.SetCurrentUser(userId, role);
            DataContext = vm;
        }
    }
}