using Microsoft.Extensions.Caching.Memory;
using Tram34TCMSInterface.Application.Abstractions.CacheMemory;
using Tram34TCMSInterface.Domain.Log;
using Tram34TCMSInterface.Application.Abstractions.MongoDB;
using Tram34TCMSInterface.Application.Abstractions.LogService;

namespace Tram34TCMSInterface.Infrastructure.Repositories.Cache
{
    public class MongoDbTrainConfigurationCacheService : IMongoDBTrainConfigurationCacheService
    {
        
        private IMemoryCache memoryCache;
        private readonly IMongoDBRepository mongoDBRepository;
        private const string CacheKeyPrefix = "TrainConfig_";

        public MongoDbTrainConfigurationCacheService(IMemoryCache memoryCache, IMongoDBRepository mongoDBRepository)
        {
            this.memoryCache = memoryCache;
            this.mongoDBRepository = mongoDBRepository;
            
            
        }

        public async Task<(string Ip, string Port)> GetLoggerServerConfigurationAsync(string TrainId)
        {
            try
            {
                TrainConfiguration result = GetTrainConfigurationFromCache(TrainId);
                if (result is null)
                {
                    //logger.LogWarning("Train configuration bulunamadı! Train ID: {TrainId}", TrainId);
                    return (null, null);
                }
                var softwareConfig = result.Software?.FirstOrDefault(x => x.Name == "LoggerLinuxServer");
                if (softwareConfig is null)
                {
                    // logger.LogWarning("LoggerLinuxServer yazılımı için konfigürasyon bulunamadı! Train ID: {TrainId}", TrainId);
                    return (null, null);
                }

                string ip = softwareConfig.ip;
                string port = softwareConfig.Port;

                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
                {
                    // logger.LogWarning("LoggerLinuxServer IP veya Port eksik! Train ID: {TrainId}", TrainId);
                    return (null, null);
                }
                return (ip, port);
            }
            catch (Exception ex)
            {
                // logger.LogError(ex, "Logger server konfigürasyonu alınırken hata oluştu. Train ID: {TrainId}", TrainId);
                return (null, null);
            }
        }

        private TrainConfiguration? GetTrainConfigurationFromCache(string trainId)
        {
            var cacheKey = CacheKeyPrefix + trainId; // Açılışta kaydettiğin TrainId'yi kullan

            // Cache kontrolü
            if (memoryCache.TryGetValue(cacheKey, out TrainConfiguration cachedConfig))
            {
                // logger.LogInformation("Train ID {TrainId} için konfigürasyon cache'ten alındı.", trainId);
                return cachedConfig;
            }

            //logger.LogWarning("Train ID {TrainId} için cache'te konfigürasyon bulunamadı!", trainId);
            return null;
        }

        public async Task<bool> SaveTrainInformationToCache(string trainId)
        {
            try
            {
                if (string.IsNullOrEmpty(trainId))
                {
                    

                    return false;
                }

                var trainConfig = await mongoDBRepository.GetTrainConfigurationFromMongoDBAsync(trainId);

                if (trainConfig is not null)
                {
                    var cacheKey = CacheKeyPrefix + trainConfig.TrainId;
                    memoryCache.Set(cacheKey, trainConfig);

                    return true;
                }
                else
                {
          
                    return false;
                }
            }
            catch (Exception ex)
            {

                return false;
            }
        }


    }
}
