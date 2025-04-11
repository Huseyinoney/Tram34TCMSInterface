using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Application.Abstractions.MongoDB
{
    public interface IMongoDBConfigurationRepository
    {
        public Task<TrainConfiguration> GetTrainConfigurationAsync(string trainId);
        public Task<(string Ip, string Port)> GetLoggerServerConfigurationAsync(string TrainId);
    }
}
