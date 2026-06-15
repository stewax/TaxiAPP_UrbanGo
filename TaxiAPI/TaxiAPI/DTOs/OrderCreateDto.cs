using System.ComponentModel.DataAnnotations;

namespace TaxiAPI.DTOs;

public class OrderCreateDto
{
    [Required(ErrorMessage = "ID клиента обязателен")]
    public int ClientId { get; set; }

    [Required(ErrorMessage = "ID тарифа обязателен")]
    public int TariffId { get; set; }

    [Required(ErrorMessage = "Адрес подачи обязателен")]
    [MaxLength(255)]
    public string PickupAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адрес назначения обязателен")]
    [MaxLength(255)]
    public string DestinationAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Стоимость обязательна")]
    [Range(0, 999999.99)]
    public decimal EstimatedCost { get; set; }
}