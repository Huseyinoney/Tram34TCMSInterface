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
        bool SendDataToLogicManager(TrainData data);
        //UdpClient CreateSocket(int Port);
    }
}