using System.ComponentModel.DataAnnotations;

namespace TaxiAPI.DTOs
{
    public class OrderCreateDto
    {
        [Required] public int TariffId { get; set; }

        [Required, MinLength(5, ErrorMessage = "Адрес должен быть не короче 5 символов")]
        public string PickupAddress { get; set; } = string.Empty;

        [Required, MinLength(5)]
        public string DestinationAddress { get; set; } = string.Empty;
    }
}
