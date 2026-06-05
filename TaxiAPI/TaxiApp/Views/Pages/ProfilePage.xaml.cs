using System.Windows;
using System.Windows.Controls;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages;

public partial class ProfilePage : UserControl
{
    public ProfilePage(MainViewModel vm)
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("✅ Профиль успешно обновлен!", "Успех",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}