using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tram34TCMSInterface.Application.Abstractions.TCP;
using Tram34TCMSInterface.Application.Abstractions.UDP;

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
