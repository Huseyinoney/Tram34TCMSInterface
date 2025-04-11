using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Application.Abstractions.LogService
{
    public interface ILogService
    {
        Task SendLogAsync<T>(T log) where T : BaseLogModel;
        public string TrainId { get; set; }
    }
}
