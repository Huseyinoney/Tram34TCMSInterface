using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Tram34TCMSInterface.Application.Abstractions.UDP;

namespace Tram34TCMSInterface.Infrastructure.Services.UDP
{
    public class ReadDataFromTCMSWithUDP : IReadDataFromTCMSWithUDP
    {
        private UdpClient udpClient;
        private readonly IConfiguration _configuration;
        public ReadDataFromTCMSWithUDP(IConfiguration configuration)
        {
           _configuration = configuration;
            if (!int.TryParse(_configuration["UDP:Port"], out int port))
            {
                throw new ArgumentException("Invalid UDP port configuration");
            }
            this.udpClient = CreateSocket();
        }

        private UdpClient CreateSocket()
        {
            return new UdpClient();
        }

        public async Task<(byte[] Buffer, IPEndPoint SenderEndPoint)> ReadDataFromTCMS()
        {
            UdpReceiveResult result = await udpClient.ReceiveAsync();
            return (result.Buffer, result.RemoteEndPoint); // Buffer ve gönderen bilgisi döndürülüyor
        }

        public JsonDocument? ConvertByteArrayToJson(byte[] buffer)
        {
            string jsonString = Encoding.UTF8.GetString(buffer);

            try
            {
                JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
                return jsonDocument;
            }
            catch (JsonException)
            {
                Console.WriteLine("Geçersiz json Data");
                return null;
            }
        }
    }
}