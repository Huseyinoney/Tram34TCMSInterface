using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tram34TCMSInterface.Application.Abstractions.CacheMemory
{
    public interface IMongoDBTrainConfigurationCacheService
    {
        Task<(string Ip, string Port)> GetLoggerServerConfigurationAsync(string TrainId);
        Task<bool> SaveTrainInformationToCache(string trainId);
    }
}
