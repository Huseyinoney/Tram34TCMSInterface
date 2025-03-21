using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tram34TCMSInterface.Application.Abstractions.UDP;

namespace Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS
{
    public class SendTakoMeterPulseDataToTakoReadExchangeFromTCMSHandler : IRequestHandler<SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand, bool>
    {
        private readonly IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP;

        public SendTakoMeterPulseDataToTakoReadExchangeFromTCMSHandler(IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP)
        {
            this.readDataFromTCMSWithUDP = readDataFromTCMSWithUDP;
        }

        public async Task<bool> Handle(SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand request, CancellationToken cancellationToken)
        {
            var result = await readDataFromTCMSWithUDP.SendTakoMeterPulseDataToTakoReadExchange(request.trainData);
            return result;
        }
    }
}
