using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Globalization;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Domain.Log;
using Tram34TCMSInterface.Application.Abstractions.CacheMemory;
using Tram34TCMSInterface.Application.Abstractions.Common;




namespace Tram34TCMSInterface.Infrastructure.Log
{
    public class LogService : ILogService
    {
        private readonly HttpClient _httpClient;
        private readonly IMongoDBTrainConfigurationCacheService _mongoDBTrainConfigurationCacheService;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ITrainContext trainContext;



        public LogService(HttpClient httpClient, IConfiguration configuration, IMongoDBTrainConfigurationCacheService mongoDBTrainConfigurationCacheService, ITrainContext trainContext)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _mongoDBTrainConfigurationCacheService = mongoDBTrainConfigurationCacheService ?? throw new ArgumentNullException(nameof(mongoDBTrainConfigurationCacheService));
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
            this.trainContext = trainContext;
        }

        public async Task SendLogAsync<T>(T log) where T : BaseLogModel
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));

            if (string.IsNullOrWhiteSpace(log.MessageType))
                throw new ArgumentException("Log tipi boş olamaz.", nameof(log));

            await _semaphore.WaitAsync();
            try
            {
                var (ip, port) = await GetLoggerServerConfigurationAsync();
                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
                {
                    Console.WriteLine("Log sunucu yapılandırması bulunamadı.");
                    return;
                }

                var url = $"http://{ip}:{port}/api/Producer/{log.MessageType.ToLower(new CultureInfo("en-EN"))}";
                var response = await _httpClient.PostAsJsonAsync(url, log);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Log gönderme başarısız: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
                else
                {
                    Console.WriteLine($"Log başarıyla gönderildi: {log.MessageType}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP isteği başarısız: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log gönderme hatası: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<(string?, string?)> GetLoggerServerConfigurationAsync()
        {
            return await _mongoDBTrainConfigurationCacheService.GetLoggerServerConfigurationAsync(trainContext.TrainId);
        }
    }
}