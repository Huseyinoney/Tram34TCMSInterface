using MediatR;
using Tram34TCMSInterface.Application.Abstractions.TCP;

namespace Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS
{

    public class SendCoupledDataToCoupleExchangeFromTCMSHandler : IRequestHandler<SendCoupledDataToCoupleExchangeFromTCMSCommand, bool>
    {
        private readonly IReadDataFromTCMSWithTCP readDataFromTCMSWithTCP;

        public SendCoupledDataToCoupleExchangeFromTCMSHandler(IReadDataFromTCMSWithTCP readDataFromTCMSWithTCP)
        {
            this.readDataFromTCMSWithTCP = readDataFromTCMSWithTCP;
        }

        public async Task<bool> Handle(SendCoupledDataToCoupleExchangeFromTCMSCommand request, CancellationToken cancellationToken)
        {
           var result = await readDataFromTCMSWithTCP.SendCoupledDataToCoupleExchange(request.trainData);
            return result;
        }
    }
}
