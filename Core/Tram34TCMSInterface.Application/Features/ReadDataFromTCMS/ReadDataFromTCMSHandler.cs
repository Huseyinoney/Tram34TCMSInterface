using MediatR;
using Microsoft.Extensions.Configuration;
using Tram34TCMSInterface.Application.Abstractions.UDP;
using static Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP;

namespace Tram34TCMSInterface.Application.Features.ReadDataFromTCMS
{
    public class ReadDataFromTCMSHandler : IRequestHandler<ReadDataFromTCMSCommand, TrainData?>
    {
        private readonly IReadDataFromTCMSWithUDP _udpService;
        private readonly string _expectedIp;
        private readonly int _expectedPort;


        public ReadDataFromTCMSHandler(
            IReadDataFromTCMSWithUDP udpService,
            IConfiguration configuration
            )
        {
            _udpService = udpService;

            (_expectedIp, _expectedPort) = GetUdpConfiguration(configuration);
            if (string.IsNullOrWhiteSpace(_expectedIp) || _expectedPort == 0)
            {
                throw new ArgumentException("Invalid IP or Port configuration in appsettings.json");
            }
        }

        public async Task<TrainData?> Handle(ReadDataFromTCMSCommand request, CancellationToken cancellationToken)
        {
            var (buffer, senderEndPoint) = await _udpService.ReadDataFromTCMS();

            if (!IsExpectedSender(senderEndPoint))
            {
                return null;
            }

            return _udpService.ConvertByteArrayToJson(buffer);
        }

        private static (string, int) GetUdpConfiguration(IConfiguration configuration)
        {
            var ip = configuration["UDP:Address"];
            var port = int.TryParse(configuration["UDP:SourcePort"], out int parsedPort) ? parsedPort : 0;
            return (ip, port);
        }

        private bool IsExpectedSender(System.Net.IPEndPoint senderEndPoint)
        {
            return senderEndPoint.Address.ToString() == _expectedIp && senderEndPoint.Port == _expectedPort;
        }
    }
}
