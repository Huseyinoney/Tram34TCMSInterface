using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Tram34TCMSInterface.Domain.Models;
using static Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP;

namespace Tram34TCMSInterface.Application.Abstractions.UDP
{
    public interface IReadDataFromTCMSWithUDP
    {
        Task<(byte[] Buffer, IPEndPoint SenderEndPoint)> ReadDataFromTCMS();
        TrainData ConvertByteArrayToJson(byte[] buffer);
        Task<bool> SendCoupledDataToCoupleExchange(TrainData data);
        Task<bool> SendTakoMeterPulseDataToTakoReadExchange(TrainData data);
    }
}