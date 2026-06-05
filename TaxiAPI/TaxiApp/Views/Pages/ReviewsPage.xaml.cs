using System.Windows;
using System.Windows.Controls;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages;

public partial class ReviewsPage : UserControl
{
    public ReviewsPage(MainViewModel vm)
    {
        InitializeComponent();
    }

    private void SubmitReview_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OrderCodeInput.Text))
        {
            MessageBox.Show("Введите код заказа", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show("✅ Отзыв успешно отправлен!", "Спасибо",
            MessageBoxButton.OK, MessageBoxImage.Information);

        OrderCodeInput.Text = "";
        CommentInput.Text = "";
    }
}