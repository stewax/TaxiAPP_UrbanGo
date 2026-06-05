using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiApp.Services
{
    public interface IApiService
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object data);
        Task<T?> PutAsync<T>(string endpoint, int id, object data);
        Task<bool> DeleteAsync(string endpoint, int id);
        Task<byte[]?> DownloadFileAsync(string endpoint);
    }
}
