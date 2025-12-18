using MediatR;
using Microsoft.Extensions.Configuration;
using Tram34TCMSInterface.Application.Abstractions.TCP;
using Tram34TCMSInterface.Domain.Models;

namespace Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP
{
    public class ReadDataFromTCMSWithTCPHandler : IRequestHandler<ReadDataFromTCMSWithTCPCommand, JsonDocumentFormatUDP.TrainData?>
    {
        private readonly IReadDataFromTCMSWithTCP readDataFromTCMSWithTCP;
        private readonly IConfiguration configuration;
        private readonly int _expectedPort;
        private readonly string _expectedIp;

        public ReadDataFromTCMSWithTCPHandler(IReadDataFromTCMSWithTCP readDataFromTCMSWithTCP, IConfiguration configuration)
        {

            this.readDataFromTCMSWithTCP = readDataFromTCMSWithTCP;
            this.configuration = configuration;
            (_expectedIp, _expectedPort) = GetTcpConfiguration(configuration);
            if (string.IsNullOrWhiteSpace(_expectedIp) || _expectedPort == 0)
            {
                throw new ArgumentException("Invalid IP or Port configuration in appsettings.json");
            }
        }
        public async Task<JsonDocumentFormatUDP.TrainData?> Handle(ReadDataFromTCMSWithTCPCommand request, CancellationToken cancellationToken)
        {

            if (!IsExpectedSender(request.RemoteEndPoint))
            {
                return null;
            }
            return readDataFromTCMSWithTCP.ConvertByteArrayToJson(request.DataBytes);

        }

        private static (string, int) GetTcpConfiguration(IConfiguration configuration)
        {
            var ip = configuration["TCP:AddressToClient"];
            var port = int.TryParse(configuration["TCP:LocalPort"], out int parsedPort) ? parsedPort : 0;
            return (ip, port);
        }
        private bool IsExpectedSender(System.Net.IPEndPoint senderEndPoint)
        {
            return senderEndPoint.Address.ToString() == _expectedIp /*&& senderEndPoint.Port == _expectedPort*/;
        }
    }
}
