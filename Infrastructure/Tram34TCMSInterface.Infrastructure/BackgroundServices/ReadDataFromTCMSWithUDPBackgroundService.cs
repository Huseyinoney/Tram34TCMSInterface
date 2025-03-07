using MediatR;
using Microsoft.Extensions.Hosting;

namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class ReadDataFromTCMSWithUDPBackgroundService : BackgroundService
    {
       private IMediator mediator;

        public ReadDataFromTCMSWithUDPBackgroundService(IMediator mediator)
        {
            this.mediator = mediator;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
