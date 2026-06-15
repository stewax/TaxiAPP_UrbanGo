using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiAPI.Models;
using TaxiAPI.DTOs;
using TaxiApp.Models;

namespace TaxiApp.Services
{
    public interface IApiService
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object data);
        Task<T?> PutAsync<T>(string endpoint, int id, object data);
        Task<bool> DeleteAsync(string endpoint, int id);
        Task<byte[]?> DownloadFileAsync(string endpoint);
        Task<List<Tariff>?> GetTariffsAsync();
        Task<Order?> CreateOrderAsync(OrderCreateDto dto);
        Task<List<Order>?> GetMyOrdersAsync(int userId);
        Task<ClientProfile?> GetMyProfileAsync();
        Task<bool> UpdateMyProfileAsync(ProfileUpdateDto dto);
    }
}
