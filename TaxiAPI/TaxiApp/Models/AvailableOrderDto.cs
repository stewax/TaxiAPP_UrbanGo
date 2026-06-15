using System.Text.Json.Serialization;

namespace TaxiApp.Models;

public class AvailableOrderDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("pickupAddress")]
    public string PickupAddress { get; set; } = string.Empty;

    [JsonPropertyName("destinationAddress")]
    public string DestinationAddress { get; set; } = string.Empty;

    [JsonPropertyName("estimatedCost")]
    public decimal EstimatedCost { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("clientName")]
    public string ClientName { get; set; } = string.Empty;

    [JsonPropertyName("clientPhone")]
    public string ClientPhone { get; set; } = string.Empty;

    [JsonPropertyName("tariffName")]
    public string TariffName { get; set; } = string.Empty;
}