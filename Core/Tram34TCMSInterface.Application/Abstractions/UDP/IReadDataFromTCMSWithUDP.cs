using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Tram34TCMSInterface.Application.Abstractions.UDP
{
    public interface IReadDataFromTCMSWithUDP
    {
        Task<(byte[] Buffer, IPEndPoint SenderEndPoint)> ReadDataFromTCMS();
        JsonDocument? ConvertByteArrayToJson(byte[] buffer);
        //UdpClient CreateSocket(int Port);
    }
}