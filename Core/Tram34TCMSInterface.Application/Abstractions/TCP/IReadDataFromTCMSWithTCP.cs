using System.Net.Sockets;
using static Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP;

namespace Tram34TCMSInterface.Application.Abstractions.TCP
{
    public interface IReadDataFromTCMSWithTCP
    {
        Task<byte[]> ReadDataFromTCMS(NetworkStream stream);
        TrainData ConvertByteArrayToJson(byte[] buffer);
        Task<bool> SendCoupledDataToCoupleExchange(TrainData data);
        Task<bool> SendTakoMeterPulseDataToTakoReadExchange(TrainData data);
    }
}
