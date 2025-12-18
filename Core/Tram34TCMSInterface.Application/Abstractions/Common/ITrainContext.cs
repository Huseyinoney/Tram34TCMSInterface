namespace Tram34TCMSInterface.Application.Abstractions.Common
{
    public interface ITrainContext
    {
        string? TrainId { get; set; }
        string? TrainIP { get; set; }
    }
}