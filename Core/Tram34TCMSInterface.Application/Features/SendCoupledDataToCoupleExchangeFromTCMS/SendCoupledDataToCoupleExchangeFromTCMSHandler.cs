using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tram34TCMSInterface.Application.Abstractions.UDP;

namespace Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS
{
    
    public class SendCoupledDataToCoupleExchangeFromTCMSHandler : IRequestHandler<SendCoupledDataToCoupleExchangeFromTCMSCommand, bool>
    {
        private readonly IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP;

        public SendCoupledDataToCoupleExchangeFromTCMSHandler(IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP)
        {
            this.readDataFromTCMSWithUDP = readDataFromTCMSWithUDP;
        }

        public async Task<bool> Handle(SendCoupledDataToCoupleExchangeFromTCMSCommand request, CancellationToken cancellationToken)
        {
           var result = await readDataFromTCMSWithUDP.SendCoupledDataToCoupleExchange(request.trainData);
            return result;
        }
    }
}
