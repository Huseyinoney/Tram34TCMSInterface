using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tram34TCMSInterface.Application.Abstractions.UDP;

namespace Tram34TCMSInterface.Application.Features.SendDataToLogicManagerFromTCMS
{
    
    public class SendDataToLogicManagerFromTCMSHandler : IRequestHandler<SendDataToLogicManagerFromTCMSCommand, bool>
    {
        private readonly IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP;

        public SendDataToLogicManagerFromTCMSHandler(IReadDataFromTCMSWithUDP readDataFromTCMSWithUDP)
        {
            this.readDataFromTCMSWithUDP = readDataFromTCMSWithUDP;
        }

        public Task<bool> Handle(SendDataToLogicManagerFromTCMSCommand request, CancellationToken cancellationToken)
        {
           var result = readDataFromTCMSWithUDP.SendDataToLogicManager(request.trainData);
            return Task.FromResult(result);
        }
    }
}
