using Tram34TCMSInterface.Application.Abstractions.Common;

namespace Tram34TCMSInterface.Infrastructure.Common
{
    public class TrainContext : ITrainContext
    {
        public string? TrainId { get; set; }
        public string? TrainIP { get; set; }
    }
}