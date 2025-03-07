using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;
using Tram34TCMSInterface.Application.Abstractions.UDP;

namespace Tram34TCMSInterface.Application.Features.ReadDataFromTCMS
{
    public class ReadDataFromTCMSHandler : IRequestHandler<ReadDataFromTCMSCommand, JsonDocument>
    {
        private readonly IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP;
        private readonly IConfiguration configuration;
        string? expectedIp;
        int expectedPort;

        public ReadDataFromTCMSHandler(IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP, IConfiguration configuration)
        {
            this.readDataFromTCMSWithUDP = readDataFromTCMSWithUDP;
            this.configuration = configuration;
            expectedIp = this.configuration["UDP:IPAddress"];
            expectedPort = int.TryParse(this.configuration["UDP:Port"], out int port) ? port : 0;

            if (string.IsNullOrWhiteSpace(expectedIp) || expectedPort == 0)
            {
                throw new ArgumentException("Invalid IP or Port configuration in appsettings.json");
            }
        }

        public async Task<JsonDocument?> Handle(ReadDataFromTCMSCommand request, CancellationToken cancellationToken)
        {  

            var (buffer, senderEndPoint) = await readDataFromTCMSWithUDP.ReadDataFromTCMS();

            if (senderEndPoint.Address.ToString() == expectedIp && senderEndPoint.Port == expectedPort)
            {
                //beklenen porttansa convert et
                var Result = readDataFromTCMSWithUDP.ConvertByteArrayToJson(buffer);
                if (Result is not null)
                {
                    return Result;
                }
            }

            Console.WriteLine($"Beklenmeyen IP veya Port: {senderEndPoint.Address}:{senderEndPoint.Port}");
            return null; // Beklenmeyen IP/porttan geldiyse boş döndür
        }
    }
}