using System.Windows;
using TaxiApp.ViewModels;
using TaxiApp.Services;
using System.Windows.Input;

namespace TaxiApp.Views;

public partial class MainWindow : Window
{
    public MainWindow(int userId, string role)
    {
        InitializeComponent();
        EnterFullScreen();
        var app = (App)Application.Current;
        var authService = app.Services.GetService(typeof(IAuthService)) as IAuthService;
        var apiService = app.Services.GetService(typeof(IApiService)) as IApiService;

        if (authService != null)
        {
            var vm = new MainViewModel(authService, apiService);
            vm.SetCurrentUser(userId, role);
            DataContext = vm;
        }
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

    // 🔥 НОВОЕ: Очищаем токен при закрытии окна
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Если окно закрывается (не свернуто), очищаем токен
        var app = (App)Application.Current;
        var authService = app.Services.GetService(typeof(IAuthService)) as IAuthService;
        authService?.ClearToken();

        System.Diagnostics.Debug.WriteLine("🚪 MainWindow закрыт, токен очищен");
    }
}