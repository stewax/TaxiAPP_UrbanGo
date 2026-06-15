using System.Text.Json.Serialization;

namespace TaxiApp.Models;

public class DriverTripDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("distanceKm")]
    public decimal DistanceKm { get; set; }

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("clientName")]
    public string ClientName { get; set; } = string.Empty;

    [JsonPropertyName("pickupAddress")]
    public string PickupAddress { get; set; } = string.Empty;

    [JsonPropertyName("destinationAddress")]
    public string DestinationAddress { get; set; } = string.Empty;
}

public class DriverEarningsResponse
{
    [JsonPropertyName("trips")]
    public List<DriverTripDto> Trips { get; set; } = new();

    [JsonPropertyName("totalIncome")]
    public decimal TotalIncome { get; set; }

    [JsonPropertyName("totalDistance")]
    public decimal TotalDistance { get; set; }

    [JsonPropertyName("totalTrips")]
    public int TotalTrips { get; set; }
}