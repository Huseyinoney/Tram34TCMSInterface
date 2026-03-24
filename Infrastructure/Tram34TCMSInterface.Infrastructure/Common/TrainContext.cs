using Tram34TCMSInterface.Application.Abstractions.Common;

namespace Tram34TCMSInterface.Infrastructure.Common
{
    public class TrainContext : ITrainContext
    {
        public string TrainId { get; set; } = string.Empty;
        public string TrainIP { get; set; } = string.Empty;
    }
}