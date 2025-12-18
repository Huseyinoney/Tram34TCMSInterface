using MediatR;
using Tram34TCMSInterface.Application.Abstractions.TCP;

namespace Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS
{
    public class SendTakoMeterPulseDataToTakoReadExchangeFromTCMSHandler : IRequestHandler<SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand, bool>
    {
        private readonly IReadDataFromTCMSWithTCP readDataFromTCMSWithTCP;

        public SendTakoMeterPulseDataToTakoReadExchangeFromTCMSHandler(IReadDataFromTCMSWithTCP readDataFromTCMSWithTCP)
        {
            this.readDataFromTCMSWithTCP = readDataFromTCMSWithTCP;
        }

        public async Task<bool> Handle(SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand request, CancellationToken cancellationToken)
        {
            var result = await readDataFromTCMSWithTCP.SendTakoMeterPulseDataToTakoReadExchange(request.trainData);
            return result;
        }
    }
}