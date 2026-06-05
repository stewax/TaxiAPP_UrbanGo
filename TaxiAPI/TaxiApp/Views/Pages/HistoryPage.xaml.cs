using System.Windows.Controls;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages;

public partial class HistoryPage : UserControl
{
    public HistoryPage(MainViewModel vm)
    {
        InitializeComponent();
        // Загрузка данных из API (пока тестовые)
        TripsGrid.ItemsSource = new[]
        {
            new { Code = "ORD-20260601-1234", CreatedAt = "01.06.2026 14:30",
                  PickupAddress = "ул. Ленина, 10", DestinationAddress = "ул. Мира, 25",
                  EstimatedCost = "450 ₽", Status = "Завершен" },
            new { Code = "ORD-20260528-5678", CreatedAt = "28.05.2026 09:15",
                  PickupAddress = "пр. Победы, 5", DestinationAddress = "Вокзал",
                  EstimatedCost = "320 ₽", Status = "Завершен" },
            new { Code = "ORD-20260520-9012", CreatedAt = "20.05.2026 18:45",
                  PickupAddress = "Аэропорт", DestinationAddress = "ул. Садовая, 8",
                  EstimatedCost = "890 ₽", Status = "Завершен" }
        };
    }
}